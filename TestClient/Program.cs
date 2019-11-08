using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Wizdom.Client;

namespace TestClient
{
    class Program
    {
        private static WizdomClient wizdomclient;
        private static string token { get; set; }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Wizdom Rest Client");
            Console.WriteLine("");

            string commandString = string.Empty;

            while (commandString.ToLower() != "continue")
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(@"
************************************************
Choose Token Handler: 
************************************************

1: Device Code Flow
2: Paste token

q: Quit

Enter command (1, 2 | q) > ");
                commandString = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine("");
                Console.WriteLine("");


                WizdomClient.LoggerDelegate logger = delegate (string message, WizdomClient.LogLevel level)
                {
                    if (level == WizdomClient.LogLevel.info) return; //ignore info...

                    var newColor = ConsoleColor.Gray;
                    switch (level)
                    {
                        case WizdomClient.LogLevel.info:
                            newColor = ConsoleColor.Gray;
                            break;
                        case WizdomClient.LogLevel.warn:
                            newColor = ConsoleColor.Yellow;
                            break;
                        case WizdomClient.LogLevel.error:
                            newColor = ConsoleColor.Red;
                            break;
                        default:
                            break;
                    }
                    var old = Console.ForegroundColor;
                    Console.ForegroundColor = newColor;
                    Console.WriteLine(message);
                    Console.ForegroundColor = old;
                };

                WizdomClient.InstanceDecisionHandlerDelegate instanceDecisionHandler = async delegate (List<WizdomInstance> instances)
                {
                    if ((instances?.Count ?? 0) == 0) return 0;

                    if (instances.Count == 1) return instances[0].LicenseID;

                    while (true)
                    {
                        int i = 1;
                        foreach (var instance in instances)
                        {
                            Console.WriteLine($"{i}. {instance.LicenseName}");
                            i++;
                        }
                        Console.Write("Choose instance > ");
                        var k = Console.ReadKey().KeyChar.ToString();
                        int iKey;
                        if (int.TryParse(k, out iKey) && iKey < i && iKey > 0)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("");
                            return instances[iKey - 1].LicenseID;
                        }
                        else
                        {
                            Console.WriteLine();
                            Console.WriteLine("Please choose instance!");
                        }
                    }
                };

                switch (commandString.ToLower())
                {
                    case "1":
                        wizdomclient = new WizdomClient(new DeviceCodeTokenHandler()) { Logger = logger, InstanceDecisionHandler = instanceDecisionHandler };
                        commandString = "continue";
                        break;
                    case "2":
                        wizdomclient = new WizdomClient(new DelegateTokenHandler(async delegate (string clientId, string resourceId)
                        {
                            if (string.IsNullOrEmpty(token))
                            {
                                Console.Write("Input token > ");
                                token = "";
                                while (true)
                                {
                                    var k = Console.ReadKey(false);
                                    if (k.Key == ConsoleKey.Enter) break;
                                    //Console.Write("*");
                                    token += k.KeyChar;
                                }
                                Console.WriteLine();
                                Console.WriteLine();
                            }
                            return token;
                        }, async delegate () { token = string.Empty; })) { Logger = logger, InstanceDecisionHandler = instanceDecisionHandler };
                        commandString = "continue";
                        break;
                    case "q":
                        Console.WriteLine("Bye!");
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
            Console.WriteLine();

            // main command cycle
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(@"
************************************************
1: Connect to Wizdom instance
2: Disconnect / Sign Out
3: Get environment direct
4: Get environment using proxy
5: People search (test SharePoint accepts token)
6: List noticeboard news

q: Quit

Enter command (1, 2, 3, 4, 5, 6 | q) > ");
                commandString = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine("");
                Console.WriteLine("");

                switch (commandString.ToLower())
                {
                    case "1":
                        var environment = await wizdomclient.ConnectAsync();
                        //var environment = await ConnectToInstanceAsync();
                        if (environment != null) Console.WriteLine($"Connected to {environment.appUrl} running Wizdom v.{environment.wizdomVersion.ToString()} as {environment.currentPrincipal.loginName}");
                        else Console.WriteLine("Error connecting!");
                        break;
                    case "2":
                        await wizdomclient.DisconnectAsync();
                        Console.WriteLine("Disconnected!");
                        break;
                    case "3":
                        Console.WriteLine(await wizdomclient.GetAsync("/api/wizdom/noticeboard/environment"));
                        break;
                    case "4":
                        Console.WriteLine(await wizdomclient.GetAsync("/api/wizdom/noticeboard/environment", useProxy: true));
                        break;
                    case "5":
                        Console.WriteLine(await wizdomclient.GetAsync("/api/wizdom/searchPerson/search?index=3&numberOfUsersPerPage=100&selectProperties=PreferredName&selectProperties=AccountName&resultSource=b09a7990-05ea-4af9-81ef-edfab16c4e31&pageindex=1&queryText=John", useProxy: false));
                        break;
                    case "6":
                        await RenderNoticeboardItems();
                        break;
                    case "q":
                        Console.WriteLine("Bye!");
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command.");
                        break;
                }
            }
        }

        private static async Task RenderNoticeboardItems()
        {
            string sChannelSets = await wizdomclient.GetAsync("/api/wizdom/noticeboard/v1/channelsets");
            var channelSets = JObject.Parse(sChannelSets);
            foreach (var channelSet in channelSets["channelSets"]["results"])
            {
                Console.WriteLine("");
                Console.WriteLine("Current news in " + channelSet["name"].Value<string>());
                string sNBItems = await wizdomclient.GetAsync(string.Format(@"/api/wizdom/noticeboard/v1/channelsets/{0}/items/current", channelSet["id"].Value<int>()));
                try
                {
                    StringBuilder sbRes = new StringBuilder();
                    sbRes.AppendLine("");
                    var jsonArray = JObject.Parse(sNBItems);
                    foreach (var news in jsonArray["items"]["results"])
                    {
                        string id = news["id"].ToString();
                        string heading = news["heading"].ToString();
                        string date = news["dateStart"].Value<DateTime>().ToShortDateString();
                        string isread = news["readByCurrentUser"].Value<bool>() ? "  " : "* ";

                        sbRes.AppendLine($"{id}: {isread}{date}: {heading}");
                    }
                    Console.WriteLine(sbRes.ToString());
                }
                catch (Exception)
                {
                    //
                }
            }
        }
    }
}