using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
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

			//Log.Info("OnNewGameCreated");

			if (!(Campaign.Current.Clans is MBReadOnlyList<Clan> clans))
				return;

			if (!(Campaign.Current.Kingdoms is MBReadOnlyList<Kingdom> kingdoms))
				return;

			for (int i = 0; i < clans.Count; i++)
			{
				var isOnlyCastle = true;
				for (int z = 0; z < clans[i].Fortifications.Count; z++)
				{
					if (clans[i].Fortifications[z].IsTown)
					{
						isOnlyCastle = false;
					}
				}

				if (!isOnlyCastle && !clans[i].Leader.Equals(clans[i].Kingdom.Leader)) //
				{
					for (int y = 0; y < kingdoms.Count; y++)
					{
						var kingdomName = kingdoms[y].GetName().ToLower().ToString().Replace('_', ' ');
						var clanName = clans[i].GetName().ToLower().ToString();

						if (kingdomName.Equals(clanName))
						{
							//Log.Info(i + ": Clan " + clans[i].GetName() + " joining " +
							//         kingdoms[y]);
							ChangeKingdomAction.ApplyByJoinToKingdom(clans[i], kingdoms[y], true);
						}
					}
				}
				else
				{
					//Log.Info(clans[i] + " has only castle or is kingdom leader, skipping");
				}
			}
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
		{
			if (!(game.GameType is Campaign))
			{
				return;
			}
			//Log.Info("OnGameStart");
		}
	}
}
