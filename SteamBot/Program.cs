using System;
using System.Threading;
using SteamKit2;
using System.IO;
using SteamTrade;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {

            if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/settings.json"))
            {
                Configuration config = Configuration.LoadConfiguration(AppDomain.CurrentDomain.BaseDirectory + "/settings.json");
                Log mainLog = new Log(config.MainLog, null);
                foreach (Configuration.BotInfo info in config.Bots)
                {
                    mainLog.Info("Launching Bot " + info.DisplayName + "...");
                    new Thread(() =>
                    {
                        int crashes = 0;
                        while (crashes < 1000)
                        {
                            try
                            {
                                new Bot(info, config.ApiKey, (Bot bot, SteamID sid) => 
                                {
                                    return (SteamBot.UserHandler)System.Activator.CreateInstance(Type.GetType(info.BotControlClass), new object[] { bot, sid });
                                });
                            }
                            catch (Exception e)
                            {
                                mainLog.Error("Error With Bot: " + e);
                                crashes++;
                            }
                        }
                    }).Start();
                    Thread.Sleep(5000);
                }
                MySQL.start();
            }
            else
            {
                Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
                Console.WriteLine("Configuration File Does not exist. Please rename 'settings-template.json' to 'settings.json' and modify the settings to match your environment");
            }
        }
    }
}
