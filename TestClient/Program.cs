using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WizdomClientStd;

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
********************************************************
Choose Token Handler: 
********************************************************

1: Device Code Flow
2: Paste token

q: Quit

Enter command (1, 2 | q) > ");
                commandString = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine("");
                Console.WriteLine("");

                switch (commandString.ToLower())
                {
                    case "1":
                        wizdomclient = new WizdomClient(new DeviceCodeTokenHandler())
                        {
                            log = delegate (string message, WizdomClient.LogLevel level)
                            {
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
                            }
                        };
                        commandString = "continue";
                        break;
                    case "2":
                        wizdomclient = new WizdomClient(new DelegateTokenHandler(async delegate (string clientId, string resourceId)
                        {
                            //"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkJCOENlRlZxeWFHckdOdWVoSklpTDRkZmp6dyIsImtpZCI6IkJCOENlRlZxeWFHckdOdWVoSklpTDRkZmp6dyJ9.eyJhdWQiOiJodHRwczovL3dlYnRvcDEuc2hhcmVwb2ludC5jb20vIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvNzUyMmU0NjItODdjNS00Y2I4LWJhZmMtZGZkZTBkNGJiOWQ3LyIsImlhdCI6MTU3MzEzODcyNSwibmJmIjoxNTczMTM4NzI1LCJleHAiOjE1NzMxNDI2MjUsImFjciI6IjEiLCJhaW8iOiJBVFFBeS84TkFBQUF3TUloS21kY2FqSmtWZHNMdFgwZWxxQ2NOMWFuWi9ZNVV2c1pkc3Y0MDhvNUUzcWVBNDh5akNZV3BjUlBBR2RrIiwiYW1yIjpbInB3ZCJdLCJhcHBfZGlzcGxheW5hbWUiOiJXaXpkb20gVW5pZmllZCBHYXRld2F5IiwiYXBwaWQiOiI0MDJjYmFlYi1jNTJhLTQzYjYtYjg4Ni00YWQxYzQ0Y2FiNmEiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IkhhbnNlbiIsImdpdmVuX25hbWUiOiJLaW0iLCJpcGFkZHIiOiIxNTIuMTE1LjE0My4zNCIsIm5hbWUiOiJLaW0gSGFuc2VuIiwib2lkIjoiZDMzNmE2MDUtNWNmMS00NjYwLWJhZmMtNTAwNjg2N2I2MDkxIiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTEyMjkyNzI4MjEtMTY3NzEyODQ4My04NDI5MjUyNDYtMjIwMSIsInB1aWQiOiIxMDAzMDAwMDgwRDc5QjQwIiwic2NwIjoiU2l0ZXMuRnVsbENvbnRyb2wuQWxsIFNpdGVzLk1hbmFnZS5BbGwgU2l0ZXMuUmVhZC5BbGwgU2l0ZXMuUmVhZFdyaXRlLkFsbCBVc2VyLkV4cG9ydC5BbGwgVXNlci5JbnZpdGUuQWxsIFVzZXIuUmVhZCBVc2VyLlJlYWQuQWxsIFVzZXIuUmVhZEJhc2ljLkFsbCBVc2VyLlJlYWRXcml0ZSBVc2VyLlJlYWRXcml0ZS5BbGwiLCJzaWQiOiI2MTVkMGNjYy1jOGQ2LTQ4NDgtODM2NC0xOTQxMzZmNzQzNGYiLCJzaWduaW5fc3RhdGUiOlsiaW5rbm93bm50d2siLCJrbXNpIl0sInN1YiI6IjhxWGQ1NF9qa1ItYWxtSDE2TmJiZktybV9FUnp2ZDdMNHAxSUx1aWVfeW8iLCJ0aWQiOiI3NTIyZTQ2Mi04N2M1LTRjYjgtYmFmYy1kZmRlMGQ0YmI5ZDciLCJ1bmlxdWVfbmFtZSI6ImtoQHdlYnRvcC5kayIsInVwbiI6ImtoQHdlYnRvcC5kayIsInV0aSI6Im43bGMwMk5mYUVhSHFnWnZWaHpNQUEiLCJ2ZXIiOiIxLjAiLCJ3aWRzIjpbImYwMjNmZDgxLWE2MzctNGI1Ni05NWZkLTc5MWFjMDIyNjAzMyJdfQ.FbDXTaE6fLVkQAVUlg0bX4SaWKMI_df97vO0-dJ4YwFB6qOgo02sXLKl1NfG6VJydHWCFtB9khfxwQ_zP2qtv5xT7MZDAcJ-_Qeif11iDxfr-wS9dbswTx4-LR-9uBTl8ocBmfjzm69ifPRGo7JZdCJUfeT-B3y8iagb3Iq0hUYqJ5NCQV1bxcsJJMbUe7XNY9uMmk-4YPnL8jreSdMSEcDbkX002MpL3wJLtWhqrrR82AhbQ84WkyGs-iIHiAP65XUZOq6iUpY1E6rqpVoJnWUkJ0sDfLv_DzD5kMw6Jl3KHja_lB0hbR2vkdEbMJKGC7d_24W6w4ggPUFRCx0Apw";

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
                        }));
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
********************************************************
a: Connect to Wizdom instance
0: Get environment using proxy root
1: Get environment direct
2: Get environment using proxy and proxy endpoint
3: Get environment using proxy and specific license id 1
4: Get licenses from disco
5: Get specific license 1 from disco
6: People search
7: List all noticeboard news from all channelsets

q: Quit

Enter command (a, 0, 1, 2, 3, 4, 5, 6, 7 | q) > ");
                commandString = Console.ReadKey().KeyChar.ToString();
                Console.WriteLine("");
                Console.WriteLine("");

                switch (commandString.ToLower())
                {
                    case "a":
                        var environment = await ConnectToInstanceAsync();
                        if (environment != null) Console.WriteLine($"Connected to {environment.appUrl} running Wizdom v.{environment.wizdomVersion.ToString()} as {environment.currentPrincipal.loginName}");
                        else Console.WriteLine("Error connecting!");
                        break;
                    case "0":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/api/wizdom/noticeboard/environment", useProxy: true));
                        break;
                    case "1":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/api/wizdom/noticeboard/environment"));
                        break;
                    case "2":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/proxy" + "/api/wizdom/noticeboard/environment", useProxy: true));
                        break;
                    case "3":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/proxy/licenseid/1" + "/api/wizdom/noticeboard/environment", useProxy: true));
                        break;
                    case "4":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/disco", useProxy: true));
                        break;
                    case "5":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/disco/1", useProxy: true));
                        break;
                    case "6":
                        Console.WriteLine(await wizdomclient.GetJsonFromAPIAsync("/proxy/api/wizdom/searchPerson/search?index=3&numberOfUsersPerPage=100&selectProperties=PreferredName&selectProperties=AccountName&selectProperties=Path&selectProperties=WorkPhone&selectProperties=MobilePhone&selectProperties=WorkEmail&selectProperties=Department&selectProperties=w365Birthday&selectProperties=Interests&selectProperties=w365HireDate&selectProperties=Responsibility&selectProperties=BaseOfficeLocation&selectProperties=Location&selectProperties=JobTitle&selectProperties=SipAddress&resultSource=b09a7990-05ea-4af9-81ef-edfab16c4e31&pageindex=1&queryText=Kim", useProxy: true));
                        break;
                    case "7":
                        string sChannelSets = await wizdomclient.GetJsonFromAPIAsync("/api/wizdom/noticeboard/v1/channelsets");
                        var channelSets = JObject.Parse(sChannelSets);
                        foreach (var channelSet in channelSets["channelSets"]["results"])
                        {
                            //string sChannelSet = GetWizdomRESTData(string.Format("/api/wizdom/noticeboard/1/channelsets/{0}", channelSet["id"].Value<int>()));
                            Console.WriteLine("");
                            Console.WriteLine("Current/archived news in " + channelSet["name"].Value<string>());
                            //string sNBItems = GetWizdomRESTData(string.Format(@"/api/wizdom/noticeboard/1/channelsets/{0}/items/archived/current?$take=5&$skip=0", channelSet["id"].Value<int>()));

                            string sNBItems = await wizdomclient.GetJsonFromAPIAsync(string.Format(@"/api/wizdom/noticeboard/v1/channelsets/{0}/items/current", channelSet["id"].Value<int>())); //?$take=0&$skip=0
                            Console.WriteLine(RenderNoticeboardItems(sNBItems));

                            //sNBItems = GetWizdomRESTData(string.Format(@"/api/wizdom/noticeboard/1.0/channelsets/{0}/items/current/archived?$take=5&$skip=0", channelSet["id"].Value<int>()));
                            //Console.WriteLine(RenderNoticeboardItems(sNBItems));
                            //sNBItems = GetWizdomRESTData(string.Format(@"/api/wizdom/noticeboard/1.1/channelsets/{0}/items/current/archived?$take=5&$skip=0", channelSet["id"].Value<int>()));
                            //Console.WriteLine(RenderNoticeboardItems(sNBItems));
                        }
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

        private static async Task<WizdomClientStd.Environment> ConnectToInstanceAsync()
        {
            var instances = await wizdomclient.GetInstancesAsync();
            if ((instances?.Count ?? 0) == 0) return null;

            if (instances.Count == 1) return await wizdomclient.ConnectAsync(instances[0].LicenseID);

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
                    return await wizdomclient.ConnectAsync(instances[iKey - 1].LicenseID);
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Please choose instance!");
                }
            }
        }

        private static string RenderNoticeboardItems(string json)
        {
            try
            {
                StringBuilder sbRes = new StringBuilder();
                sbRes.AppendLine("");
                var jsonArray = JObject.Parse(json);
                foreach (var news in jsonArray["items"]["results"])
                {
                    string id = news["id"].ToString();
                    string heading = news["heading"].ToString();
                    string principal = GetPrincipalFromId(jsonArray, news["authorPrincipalId"])["displayName"].Value<string>();
                    string date = news["dateStart"].Value<DateTime>().ToShortDateString();
                    string isread = news["readByCurrentUser"].Value<bool>() ? "  " : "* ";

                    sbRes.AppendLine(string.Format("{4}: {3}{2}: {0} by {1}", heading, principal, date, isread, id));
                    foreach (var commentId in news["commentIds"])
                    {
                        var comment = GetCommentFromId(jsonArray, commentId);
                        //sbRes.AppendLine(string.Format("  {0}: {1}", comment["id"].Value<int>(), comment["content"].Value<string>()));
                        sbRes.AppendLine(string.Format("  {0}: {2} - {1}", comment["id"].Value<int>(), GetPrincipalFromId(jsonArray, comment["authorPrincipalId"])["displayName"].Value<string>(), comment["datePosted"].Value<DateTime>().ToShortDateString()));
                    }
                }
                return sbRes.ToString();
            }
            catch (Exception)
            {
                return json;
            }
        }

        private static JToken GetCommentFromId(JObject jsonArray, JToken commentId)
        {
            foreach (var comment in jsonArray["comments"]["results"])
            {
                if (comment["id"].Value<int>() == commentId.Value<int>())
                    return comment;
            }
            return null;
        }

        private static JToken GetPrincipalFromId(JObject jsonArray, JToken principalId)
        {
            foreach (var principal in jsonArray["principals"]["results"])
            {
                if (principal["id"].Value<int>() == principalId.Value<int>())
                    return principal;
            }
            return null;
        }

        private static bool HasProp(JToken news, string v)
        {
            bool res = false;
            try
            {
                var s = news[v];
                res = s != null;
            }
            catch (Exception)
            {

            }

            return res;
        }

    }
}