using System;
using ShatteredKingdoms.Behaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ShatteredKingdoms
{
	public class ShatteredKingdomsSubModule : MBSubModuleBase
	{
		protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		protected override void OnSubModuleLoad()
		{
			NLog.Config.LoggingConfiguration logConfig = new NLog.Config.LoggingConfiguration();
			NLog.Targets.FileTarget logFile = new NLog.Targets.FileTarget(LogFileTarget()) { FileName = LogFilePath() };

			logConfig.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logFile);
			NLog.LogManager.Configuration = logConfig;
		}

		protected virtual string LogFileTarget()
		{
			return "ShatteredKingdomsLogFile";
		}

		protected virtual string LogFilePath()
		{
			// The default, relative path will place the log in $(GameFolder)\bin\Win64_Shipping_Client\
			return "ShatteredKingdomsLog.txt";
		}

		public override void OnNewGameCreated(Game game, object initializerObject)
		{
			base.OnNewGameCreated(game, initializerObject);

			ShatterKingdoms();
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			if (!(game.GameType is Campaign))
			{
				return;
			}

			//Log.Info("OnGameStart");

			CampaignGameStarter initializer = (CampaignGameStarter)gameStarterObject;

			initializer.AddBehavior(new ShatterBehavior());
		}

		private void ShatterKingdoms()
		{
			try
			{
				foreach (Clan clan in Campaign.Current.Clans)
				{
					var isOnlyCastle = true;
					foreach (Town town in clan.Fortifications)
					{
						if (town.IsTown)
						{
							isOnlyCastle = false;
						}
					}

					if (!isOnlyCastle && !clan.Leader.Equals(clan.Kingdom.Leader))
					{
						foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
						{
							var kingdomName = kingdom
								.GetName().ToLower().ToString().Replace('_', ' ');

							var clanName = clan.GetName().ToLower().ToString();

							if (kingdomName.Equals(clanName))
							{
								ChangeKingdomAction
									.ApplyByJoinToKingdom(clan, kingdom, true);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Info(e);
			}
		}
	}
}
