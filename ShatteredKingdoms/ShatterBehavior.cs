using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using Newtonsoft.Json;
using TaleWorlds.SaveSystem;
using System.Xml;

namespace ShatteredKingdoms
{
	public class ShatterBehavior : CampaignBehaviorBase
	{
		protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		//[SaveableField(1)] 
		//private Dictionary<string, Clan> _targetClans;
		//[SaveableField(2)]
		//private Dictionary<string, PartyBase> _parties;
		//[SaveableField(3)] 
		//private Dictionary<string, Settlement> _settlements;

		//_targetClans = new Dictionary<string, Clan>();
		//_parties = new Dictionary<string, PartyBase>();
		//_settlements = new Dictionary<string, Settlement>();

		public ShatterBehavior()
		{

		}

		public override void RegisterEvents()
		{
			try
			{
				CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, ShatterRandomClan);
				//CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnSiegeEnded);
			}
			catch
			{
				Log.Info("Exception in Register Events");
			}
		}

		//dataStore.SyncData("_targetClans", ref _targetClans);
		//dataStore.SyncData("_parties", ref _parties);
		//dataStore.SyncData("_settlements", ref _settlements);

		public override void SyncData(IDataStore dataStore)
		{

		}

		private void ShatterRandomClan()
		{
			try
			{
				Log.Info("Daily tick");

				if (!(Campaign.Current.Kingdoms is MBReadOnlyList<Kingdom> kingdoms))
					return;

				//if(!_targetClans.IsEmpty()) 
				//	return;



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

						//if (_targetClans.ContainsKey(clan.Id.ToString()))
						//	continue;

						

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


							//_targetClans[clan.Id.ToString()] = clan;
							//_parties[rebel.ToString()] = rebel.Party;
							//_settlements[rebel.ToString()] = settlementTarget;
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
							// Is there a garrison to siege?
							//MobileParty garrison = settlementTarget.Parties.FirstOrDefault(possibleGarrison =>
							//{
							//	if (possibleGarrison.IsGarrison)
							//	{
							//		return true;
							//	}

							//	if (possibleGarrison.MapFaction.StringId ==
							//	    settlementTarget.OwnerClan.MapFaction.StringId)
							//	{
							//		return true;
							//	}

							//	return false;
							//});


							FactionManager.DeclareWar(newLeader.MapFaction, settlementTarget.MapFaction);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Leader, -20, false);
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(newLeader, clan.Kingdom.Leader, -20, false);

							// If yes, start siege
							//if (garrison != null)
							//{
								//Campaign.Current.MapEventManager.StartSiegeOutsideMapEvent(rebel.Party,
								//	garrison.Party);

							rebel.Ai.SetDoNotMakeNewDecisions(true);

							SetPartyAiAction.GetActionForBesiegingSettlement(rebel, settlementTarget);
							//}
							//else
							//{
								//CustomChangeSettlementOwner(settlementTarget, newLeader);
							//}
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

				XmlDocument document = new XmlDocument();

				document.LoadXml(
					$"<Faction id=\"{newClan.StringId}\"" +
					//$" owner=\"{leader.StringId}\"" +
					//$" name=\"{{=!}}{leader.CharacterObject.GetName()}\"" +
					//"  tier=\"3\"" +
					"  renown=\"750\"" +
					//$" culture=\"Culture.{leader.Culture.StringId}\"" +
					//"  is_minor_faction=\"true\"" +
					//$" super_faction=\"Kingdom.{leader.StringId}_kingdom\"
					//$"  banner_key=\"{Banner.CreateRandomClanBanner(leader.StringId.GetDeterministicHashCode())}\"" + 
					"/>");

				XmlNode node = document.SelectSingleNode("/Faction");

				newClan.Deserialize(MBObjectManager.Instance, node);



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

			XmlDocument document = new XmlDocument();

			document.LoadXml(
				$"  <Kingdom" +
				$"    id = \"{newKingdom.StringId}\"" +
				$"    owner = \"{leader.StringId}\"" +
				$"    culture = \"Culture.{leader.Culture.StringId}\"" +
				$"    name = \"{{=!}}{leader.Name} of {origin}\"" +
				$"    short_name = \"{{=!}}{leader.Name} of {origin}\"" +
				$"    title = \"{{=!}}{leader.Name} of {origin}\"" +
				$"    ruler_title=\"Usurper King\"" +
				$"	  primary_banner_color = \"0xff793191\"" +
				$"    secondary_banner_color = \"0xffFCDE90\"" +
				$"    label_color = \"FF850C6D\"" +
				$"    color = \"FF4E3A55\"" +
				$"    color2 = \"FFDE9953\"" +
				$"    alternative_color = \"FFffffff\"" +
				$"    alternative_color2 = \"FF660653\"" +
				$"    text = \"The {opponent} have spread their borders too thin, this ambitious upstart is taking advantage of it\"" +
				$">" +
				$"</Kingdom>");

			XmlNode node = document.SelectSingleNode("/Kingdom");
			
			newKingdom.Deserialize(MBObjectManager.Instance, node);



			//Kingdom newKingdom = MBObjectManager.Instance.CreateObject<Kingdom>();

			//newKingdom.Culture = leader.Culture;
			//newKingdom.Name = new TextObject(
			//	"{=!}{CLAN_NAME}",
			//	(Dictionary<string, TextObject>)null);
			//newKingdom.Name.SetTextVariable("CLAN_NAME", leader.Name);
			//newKingdom.InformalName = newKingdom.Name;

			var name = new TextObject(
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

		//private void OnSiegeEnded(MapEvent mapEvent)
		//{
		//	PartyBase rebel = null;
		//	Settlement settlement = null;

		//	if (mapEvent?.InvolvedParties == null) 
		//		return;

		//	try
		//	{
		//		foreach (PartyBase rebelParty in mapEvent.InvolvedParties)
		//		{
		//			if (rebelParty?.Leader == null)
		//				continue;

		//			if (!_parties.ContainsKey(rebelParty.MobileParty.ToString()) &&
		//			    !_settlements.ContainsKey(rebelParty.MobileParty.ToString()))
		//				continue;

		//			rebel = _parties[rebelParty.MobileParty.ToString()];
		//			settlement = _settlements[rebelParty.MobileParty.ToString()];
		//		}

		//		if (rebel == null || settlement == null)
		//			return;

		//		if (mapEvent.BattleState == BattleState.AttackerVictory)
		//		{
		//			Log.Info("Rebel " + rebel.Owner.Name + " won siege " + settlement.OwnerClan.Id.ToString());

		//			_targetClans.Remove(settlement.OwnerClan.Id.ToString());

		//			CustomChangeSettlementOwner(settlement, rebel.Owner);

		//			settlement.AddGarrisonParty(true);

		//		}
		//		else
		//		{
		//			_targetClans.Remove(settlement.OwnerClan.Id.ToString());

		//			Log.Info("Siege of " + settlement.Name + " failed");

		//			rebel.MobileParty.RemoveParty();
		//			//DestroyClanAction.Apply(rebel.Owner.Clan);
		//		}


		//		_parties.Remove(rebel.MobileParty.ToString());
		//		_settlements.Remove(rebel.MobileParty.ToString());
		//	}
		//	catch (Exception e)
		//	{
		//		Log.Info("Exception caught when trying to end siege");
		//		Log.Info(e);
		//	}
		//}

		//private void CustomChangeSettlementOwner(Settlement settlement, Hero newOwner)
		//{
		//	//Hero leader = settlement.OwnerClan?.Leader;

		//	if (settlement.IsFortification)
		//	{
		//		if (newOwner != null)
		//			settlement.OwnerClan = newOwner.Clan;
		//		if (settlement.Town.GarrisonParty == null)
		//			settlement.AddGarrisonParty(false);
		//		settlement.Town.GarrisonParty.Party.Owner = newOwner.Clan.Leader;
		//		settlement.Town.Governor = (Hero) null;
		//	}

		//settlement.Party.Visuals.SetMapIconAsDirty();

		//foreach (Village boundVillage in settlement.BoundVillages)
		//{
		//	boundVillage.Settlement.Party.Visuals.SetMapIconAsDirty();
		//}

		//if (newOwner != null)
		//{
		//	if (settlement.Party.MapEvent != null &&
		//		!settlement.Party.MapEvent.AttackerSide.LeaderParty.MapFaction.IsAtWarWith(newOwner.MapFaction))
		//	{
		//		settlement.Party.MapEvent.DiplomaticallyFinished = true;

		//		List<MobileParty> mobilePartyList = new List<MobileParty>();

		//		foreach (PartyBase party in (IEnumerable<PartyBase>)settlement.Party.MapEvent.AttackerSide._parties)
		//		{
		//			if (party.MobileParty != null)
		//				mobilePartyList.Add(party.MobileParty);
		//		}

		//		foreach (MobileParty party in settlement.MapFaction._parties)
		//		{
		//			if (party.DefaultBehavior == AiBehavior.DefendSettlement &&
		//				party.TargetSettlement == settlement && party.CurrentSettlement == null)
		//				party.SetMoveModeHold();
		//		}

		//		//settlement.Party.MapEvent.Update();

		//		foreach (MobileParty mobileParty in mobilePartyList)
		//		{
		//			mobileParty.SetMoveModeHold();
		//			//if (mobileParty == MobileParty.MainParty)

		//			//	GameMenu.ActivateGameMenu("hostile_action_end_by_peace");
		//		}
		//	}

		//	foreach (Clan clan in Clan.All)
		//	{
		//		if (newOwner.MapFaction == null || clan.Kingdom == null && !clan.IsAtWarWith(newOwner.MapFaction) ||
		//			clan.Kingdom != null && !clan.Kingdom.IsAtWarWith(newOwner.MapFaction))
		//		{
		//			foreach (MobileParty party in clan._parties)
		//			{
		//				if (party.BesiegedSettlement != settlement &&
		//					(party.DefaultBehavior == AiBehavior.RaidSettlement ||
		//					 party.DefaultBehavior == AiBehavior.BesiegeSettlement ||
		//					 party.DefaultBehavior == AiBehavior.AssaultSettlement) &&
		//					party.TargetSettlement == settlement)
		//				{
		//					if (party.Army != null)
		//						party.Army.FinishArmyObjective();
		//					party.SetMoveModeHold();
		//				}
		//			}
		//		}
		//	}
		//}
		//}
	}
}