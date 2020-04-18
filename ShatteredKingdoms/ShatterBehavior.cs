using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ShatteredKingdoms
{
	public class ShatterBehavior : CampaignBehaviorBase
	{
		protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		public ShatterBehavior()
		{ }

		public override void RegisterEvents()
		{
			try
			{
				CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, ShatterRandomClan);
			}
			catch
			{
				Log.Info("Exception in Register Events");
			}
		}

		public override void SyncData(IDataStore dataStore)
		{ }

		private void ShatterRandomClan()
		{
			try
			{
				Log.Info("Daily tick");

				if (!(Campaign.Current.Kingdoms is MBReadOnlyList<Kingdom> kingdoms))
					return;

				foreach (Kingdom kingdom in kingdoms)
				{
					if (kingdom == null || kingdom.Leader.IsHumanPlayerCharacter)
						continue;

					if (kingdom.Fortifications.Count() < 5)
						continue;

					if (!(kingdom.Clans is MBReadOnlyList<Clan> clans))
						continue;

					foreach (Clan clan in clans.ToList<Clan>())
					{
						if (clan?.Leader == null && clan?.Kingdom == null)
							continue;

						if (clan.Leader?.GetName().ToString() == (kingdom.Leader.GetName().ToString()))
							continue;

						if (clan.Fortifications.Count < 1)
							continue;

						Random rand = new Random();

						Settlement settlementTarget = clan
							.Fortifications[rand.Next(clan.Fortifications.Count - 1)].Settlement;

						if (settlementTarget.IsUnderSiege)
							continue;

						// Create Leader

						Hero newLeader = CreateRandomLeader(clan);

						if (newLeader == null)
							continue;

						// Create Army

						MobileParty rebel = null;
						
						try
						{
							Log.Info("New leader " + newLeader.CharacterObject.GetName() +
							         " will siege " + settlementTarget.Name + " " + settlementTarget.OwnerClan.Id.ToString());

							rebel = FillPartyWithTroopsAndInit(newLeader, settlementTarget);
						}
						catch (Exception e)
						{
							Log.Info("Exception when trying to create new Army");
							Log.Info(e);
						}

						if (rebel == null)
							continue;

						// Create Clan

						Clan newClan = InitializeClan(newLeader);

						if (newClan == null)
							continue;

						// Create Kingdom

						Kingdom newKingdom = InitializeKingdom(newLeader, 
							settlementTarget.BoundVillages[0].Name.ToString(),
							kingdom.Name.ToString());

						if (newKingdom == null)
							continue;

						// Siege settlement

						try
						{
							FactionManager.DeclareWar(newLeader.MapFaction, settlementTarget.MapFaction);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Leader, -20, false);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Kingdom.Leader, -20, false);

							rebel.Ai.SetDoNotMakeNewDecisions(true);

							SetPartyAiAction.GetActionForBesiegingSettlement(rebel, settlementTarget);
						}
						catch (Exception e)
						{
							Log.Info("Exception when trying to siege settlement");
							Log.Info(e);
						}

						return;
					}
				}
			}
			catch (Exception e)
			{
				Log.Info("Exception when in the Daily Tick");
				Log.Info(e);
			}
		}

		private static Hero CreateRandomLeader(Clan clan)
		{
			Hero specialHero = null;
			try
			{
				CharacterObject template = clan.Leader.CharacterObject;

				specialHero = HeroCreator.CreateSpecialHero(template,
					(Settlement) null, clan, (Clan) null, -1);

				specialHero.ChangeState(Hero.CharacterStates.NotSpawned);

				specialHero.IsMinorFactionHero = false;
				specialHero.IsNoble = true;

				Random rand = new Random();

				specialHero.AddSkillXp(SkillObject.GetSkill(0), rand.Next(80000, 200000)); // One Handed
				specialHero.AddSkillXp(SkillObject.GetSkill(2), rand.Next(80000, 200000)); // Pole Arm
				specialHero.AddSkillXp(SkillObject.GetSkill(6), rand.Next(80000, 200000)); // Riding
				specialHero.AddSkillXp(SkillObject.GetSkill(7), rand.Next(80000, 200000)); // Athletics
				specialHero.AddSkillXp(SkillObject.GetSkill(9), rand.Next(80000, 200000)); // Tactics
				specialHero.AddSkillXp(SkillObject.GetSkill(13), rand.Next(80000, 200000)); // Leadership
				specialHero.AddSkillXp(SkillObject.GetSkill(15), rand.Next(80000, 200000)); // Steward
				specialHero.AddSkillXp(SkillObject.GetSkill(17), rand.Next(80000, 200000)); // Engineering

				specialHero.ChangeState(Hero.CharacterStates.Active);

				//if (specialHero.MapFaction.Leader == null)
				//{
				//	if (!specialHero.MapFaction.IsKingdomFaction)
				//	{
				//		(specialHero.MapFaction as Clan).SetLeader(specialHero);
				//	}
				//	else
				//	{
				//		(specialHero.MapFaction as Kingdom).RulingClan = clan;
				//	}
				//}
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to create new Hero");
				Log.Info(e);
			}

			return specialHero;
		}

		private static MobileParty FillPartyWithTroopsAndInit(Hero leader, Settlement target)
		{
			MobileParty rebelParty = null;

			try
			{
				rebelParty = MobileParty.Create(leader.CharacterObject.GetName().ToString());

				Random rand = new Random();

				TroopRoster army = new TroopRoster();

				TroopRoster infantry = new TroopRoster();
				infantry.FillMembersOfRoster(rand.Next(80, 120), leader.Culture.MeleeMilitiaTroop);
				army.Add(infantry);

				TroopRoster skilledInfantry = new TroopRoster();
				skilledInfantry.FillMembersOfRoster(rand.Next(40, 60), leader.Culture.MeleeEliteMilitiaTroop);
				army.Add(skilledInfantry);

				TroopRoster archers = new TroopRoster();
				archers.FillMembersOfRoster(rand.Next(40, 60), leader.Culture.RangedMilitiaTroop);
				army.Add(archers);

				TroopRoster skilledArchers = new TroopRoster();
				skilledArchers.FillMembersOfRoster(rand.Next(20, 40), leader.Culture.RangedEliteMilitiaTroop);
				army.Add(skilledArchers);

				TroopRoster prisoners = new TroopRoster
				{
					IsPrisonRoster = true
				};

				ItemObject itemObject = new ItemObject(ItemObject.AllTradeGoods.First(i => i.IsFood));

				ItemRosterElement itemElement = new ItemRosterElement(itemObject, 300, new ItemModifier());

				rebelParty.ItemRoster.Add(itemElement);

				rebelParty.Party.Owner = leader;

				rebelParty.MemberRoster.AddToCounts(leader.CharacterObject, 1, false, 0, 0, true, 0);

				rebelParty.InitializeMobileParty(new TextObject(
						leader.CharacterObject.GetName().ToString(), null),
					army,
					prisoners,
					target.GatePosition,
					5.0f,
					3.0f);

			}
			catch (Exception e)
			{
				Log.Info(e);
			}

			return rebelParty;
		}

		private Clan InitializeClan(Hero leader)
		{
			Clan newClan = MBObjectManager.Instance.CreateObject<Clan>();

			try
			{
				newClan.Culture = leader.Culture;

				newClan.Name = new TextObject(
					"{=!}{CLAN_NAME}",
					(Dictionary<string, TextObject>) null);

				newClan.Name.SetTextVariable("CLAN_NAME", leader.Name);

				newClan.InformalName = newClan.Name;

				newClan.AddRenown(900, false);

				newClan.SetLeader(leader);

				leader.Clan = newClan;

				newClan.InitializeClan(newClan.Name,
					newClan.Name,
					leader.Culture,
					Banner.CreateRandomClanBanner(leader.StringId.GetDeterministicHashCode()));
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to create new Clan");
				Log.Info(e);
			}

			return newClan;
		}

		private Kingdom InitializeKingdom(Hero leader, string origin, string opponent)
		{
			Kingdom newKingdom = MBObjectManager.Instance.CreateObject<Kingdom>();

			TextObject name = new TextObject(
				"{=!}{CLAN_NAME}",
				(Dictionary<string, TextObject>)null);

			name.SetTextVariable("CLAN_NAME", leader.Name);

			newKingdom.InitializeKingdom(name,
				name,
				leader.Culture,
				Banner.CreateRandomClanBanner(leader.StringId.GetDeterministicHashCode()),
				0,
				0,
				new Vec2(0, 0));

			ChangeKingdomAction.ApplyByJoinToKingdom(leader.Clan, newKingdom, false);
			newKingdom.RulingClan = leader.Clan;

			return newKingdom;
		}
	}
}