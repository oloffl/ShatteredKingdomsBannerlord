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
				CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnSiegeEnded);
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

				foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
				{
					if (kingdom == null || kingdom.Leader.IsHumanPlayerCharacter)
						continue;

					if (kingdom.Fortifications.Count() < 5) // This will be adjusted once everything works
						continue;

					foreach (Clan clan in kingdom.Clans)
					{
						if (clan?.Leader == null && clan?.Kingdom == null)
							continue;

						if (clan.Leader?.GetName().ToString() == (kingdom.Leader.GetName().ToString()))
							continue;

						if (clan.Fortifications.Count < 1)
							continue;

						Random rand = new Random();

						Settlement settlementTarget = clan
							.Fortifications[rand.Next(clan.Fortifications.Count() - 1)].Settlement;

						if (settlementTarget.IsUnderSiege || settlementTarget.IsTown || !settlementTarget.IsFortification)
							continue;

						// Create Leader

						Hero newLeader = CreateRandomLeader(clan);

						// Create Army

						MobileParty rebelParty = FillPartyWithTroopsAndInit(newLeader, settlementTarget);

						// Create Clan

						Clan newClan = InitializeClan(newLeader, settlementTarget.Name.ToString());

						// Create Kingdom

						Kingdom newKingdom = InitializeKingdom(newLeader);

						// Siege settlement

						try
						{
							FactionManager.DeclareWar(newLeader.MapFaction, settlementTarget.MapFaction);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Leader, -20, false);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Kingdom.Leader, -20, false);

							rebelParty.Ai.SetDoNotMakeNewDecisions(true);

							SetPartyAiAction.GetActionForBesiegingSettlement(rebelParty, settlementTarget);
						}
						catch (Exception e)
						{
							Log.Info("Exception when trying to siege settlement");
							Log.Info(e);
						}

						Log.Info("New leader " + newLeader.CharacterObject.GetName() +
								 " will siege " + settlementTarget.Name + " " + settlementTarget.OwnerClan.Id.ToString());

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
					null, clan, null, -1);

				specialHero.ChangeState(Hero.CharacterStates.NotSpawned);

				specialHero.IsMinorFactionHero = false;

				specialHero.IsNoble = true;

				Random rand = new Random();

				specialHero.AddSkillXp(SkillObject.GetSkill(0), rand.Next(80000, 500000)); // One Handed
				specialHero.AddSkillXp(SkillObject.GetSkill(2), rand.Next(80000, 500000)); // Pole Arm
				specialHero.AddSkillXp(SkillObject.GetSkill(6), rand.Next(80000, 500000)); // Riding
				specialHero.AddSkillXp(SkillObject.GetSkill(7), rand.Next(80000, 500000)); // Athletics
				specialHero.AddSkillXp(SkillObject.GetSkill(9), rand.Next(80000, 500000)); // Tactics
				specialHero.AddSkillXp(SkillObject.GetSkill(13), rand.Next(80000, 500000)); // Leadership
				specialHero.AddSkillXp(SkillObject.GetSkill(15), rand.Next(80000, 500000)); // Steward
				specialHero.AddSkillXp(SkillObject.GetSkill(17), rand.Next(0000, 500000)); // Engineering

				specialHero.ChangeState(Hero.CharacterStates.Active);

				if (specialHero.MapFaction.Leader == null)
				{
					if (!specialHero.MapFaction.IsKingdomFaction)
					{
						(specialHero.MapFaction as Clan).SetLeader(specialHero);
					}
					else
					{
						(specialHero.MapFaction as Kingdom).RulingClan = clan;
					}
				}
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
			MobileParty rebelParty = MBObjectManager.Instance.CreateObject<MobileParty>(leader.CharacterObject.Name.ToString() + "_" + leader.Id);

			try
			{
				rebelParty.Initialize();

				//rebelParty = MobileParty.Create(leader.CharacterObject.Name.ToString());

				Random rand = new Random();

				TroopRoster roster = new TroopRoster();

				TroopRoster infantry = new TroopRoster();
				infantry.FillMembersOfRoster(rand.Next(200, 250), leader.Culture.MeleeMilitiaTroop);
				roster.Add(infantry);

				TroopRoster skilledInfantry = new TroopRoster();
				skilledInfantry.FillMembersOfRoster(rand.Next(120, 150), leader.Culture.MeleeEliteMilitiaTroop);
				roster.Add(skilledInfantry);

				TroopRoster archers = new TroopRoster();
				archers.FillMembersOfRoster(rand.Next(160, 200), leader.Culture.RangedMilitiaTroop);
				roster.Add(archers);

				TroopRoster skilledArchers = new TroopRoster();
				skilledArchers.FillMembersOfRoster(rand.Next(100, 140), leader.Culture.RangedEliteMilitiaTroop);
				roster.Add(skilledArchers);

				TroopRoster prisoners = new TroopRoster
				{
					IsPrisonRoster = true
				};

				rebelParty.Party.Owner = leader;

				rebelParty.MemberRoster.AddToCounts(leader.CharacterObject, 1, false, 0, 0, true, 0);

				rebelParty.InitializeMobileParty(new TextObject(
						leader.CharacterObject.GetName().ToString(), null),
					roster,
					prisoners,
					target.GatePosition,
					0.0f,
					0.0f);

				// Adding food makes game unsavable

				//ItemObject itemObject = new ItemObject(ItemObject.All.First(i => i.IsFood));

				//rebelParty.ItemRoster.AddToCounts(itemObject, 50);

				rebelParty.HomeSettlement = target;

			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to create new Army");
				Log.Info(e);
			}

			return rebelParty;
		}

		private Clan InitializeClan(Hero leader, string origin)
		{
			Clan newClan = MBObjectManager.Instance.CreateObject<Clan>();

			try
			{
				newClan.Culture = leader.Culture;

				TextObject name = new TextObject(
					"{=!}{CLAN_NAME}",
					(Dictionary<string, TextObject>) null);

				name.SetTextVariable("CLAN_NAME", leader.Name + " of " + origin);

				newClan.AddRenown(900, false);

				newClan.SetLeader(leader);

				leader.Clan = newClan;

				newClan.InitializeClan(name,
					name,
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

		private Kingdom InitializeKingdom(Hero leader)
		{
			Kingdom newKingdom = MBObjectManager.Instance.CreateObject<Kingdom>();

			TextObject name = new TextObject(
				"{=!}{CLAN_NAME}",
				null);

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

		private void OnSiegeEnded(MapEvent mapEvent)
		{
			if (mapEvent?.InvolvedParties == null)
				return;

			try
			{
				foreach (PartyBase party in mapEvent.InvolvedParties)
				{
					if (party?.MobileParty?.Ai == null)
						continue;

					if(party.MobileParty.Ai.DoNotMakeNewDecisions)
						party.MobileParty.Ai.SetDoNotMakeNewDecisions(false);
				}
			}
			catch (Exception e)
			{
				Log.Info("Exception caught when trying to end siege");
				Log.Info(e);
			}
		}
	}
}