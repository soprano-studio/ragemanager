using GrandManager.engine;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ScottPlot.Control;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrandManager
{
    internal static class UserLongPoll
    {

        private static string LP_server = "";
        private static string key = "";
        private static long ts;
        private static long pts;
        public static async void startHandlerLP()
        {
            Action onCompleted = () =>
            {
                vk.addError(99999, "Error UserLP, restart server");
                startHandlerLP();
            };

            var thread = new Thread(
              async () =>
              {
                  try
                  {
                      await HandlerLongPoll();
                  }
                  finally
                  {
                      onCompleted();
                  }
              });
            thread.Start();
            //handler.Start();
        }

        public static async Task HandlerLongPoll()
        {
            Cache cach = new();
            vk Vk = new(cach);
            await GetLongPollServer();
            while (true)
            {
                try
                {
                    var result = await Vk.Get("https://" + LP_server + "?act=a_check&key=" + key + "&ts=" + ts + "&wait=25&version=3&mode=32");

                    if (result == "" || result == null)
                    {
                        break;
                    }
                    AdminInfo.addRequest(false);

                    var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, MaxDepth = 256 };
                    var _jsonSerializer = JsonSerializer.Create(settings);
                    var parames = (JObject)JsonConvert.DeserializeObject<JObject>(result, settings).ToObject(typeof(object), _jsonSerializer);


                    //var parames = JObject.Parse(result);
                    if (parames["failed"] != null)
                    {
                        //HandlerLongPoll();
                        Console.WriteLine("Update handle");
                        break;
                    }
                    if (parames["ts"] == null)
                    {
                        Console.WriteLine("[ULP] Error ts");
                        Console.WriteLine(parames);
                        break;
                    }

                    if (parames["pts"] == null)
                    {
                        Console.WriteLine("[ULP] Error pts");
                        Console.WriteLine(parames);
                        break;
                    }

                    ts = (int)parames["ts"];
                    //pts = (long)parames["pts"];

                    if (parames["updates"].Count() <= 0)
                    {
                        //Console.WriteLine("ошибка обновлений");
                        AdminInfo.clear_updates++;
                        continue;
                    }

                    var updates = parames.SelectToken("updates").ToList();
                    //long start = DateTime.Now.Ticks;
                    //long start = settings.GetMicroTime();
                    //var micro_start_command = settings.GetMicroTime();//https://vk.com/fblll

                    bool needed_pts = false;
                    foreach (var item in updates)
                    {
                        if (item[0].ToString() != "4")
                        {
                            continue;
                        }
                        needed_pts = true;
                        //break;
                    }

                    if (needed_pts)
                    {
                        var dataMessages = await Vk.VkAPI("messages.getLongPollHistory", new Dictionary<string, string>() {
                        { "ts", ts.ToString() }, { "pts", pts.ToString() }, { "access_token", GrandManager.engine.settings.USER_VK_KATE_MOBILE_TOKEN } });
                        var response = dataMessages["response"];
                        if (response["new_pts"] != null)
                        {
                            pts = (long)response["new_pts"];
                        }

                        if (response["messages"] == null)
                        {
                            continue;
                        }

                        var updatesItems = response["messages"];
                        if (updatesItems["items"] == null)
                        {
                            continue;
                        }
                        var updates2 = updatesItems["items"];
                        // WORK UPDATES 

#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                        Task.Run(async () =>
                        {
                            try
                            {
                                await Parallel.ForEachAsync(updates2, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (upd, token) =>
                                {
                                    if (upd != null)
                                    {
                                        WorkCommand(upd);
                                        //Console.WriteLine("2342");
                                        //Console.WriteLine($"[ULP] Message from {from_id} ({peer_id}): {text}");
                                    }
                                });
                            }
                            catch
                            {
                                // Log the exception here if needed
                            }
                        });
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен

                        //Console.WriteLine("1");
                    }
                    else
                    {
                        continue;
                    }
                }
                catch
                {
                    Console.WriteLine("[UPL] ERROR UNHANDLER");
                }
            }
        }

        static async Task GetLongPollServer()
        {
            Cache cach = new();
            vk Vk = new(cach);
            while (true)
            {
                var jObject = await Vk.VkAPI("messages.getLongPollServer", new Dictionary<string, string>() { { "need_pts", "1" }, { "access_token", settings.USER_VK_KATE_MOBILE_TOKEN } });
                //var jObject = JObject.Parse(result);
                if (jObject == null || jObject["response"] == null)
                {
                    Thread.Sleep(10000);
                    continue;
                }
                JToken Response = jObject["response"];
                JToken server = Response["server"];
                JToken keyd = Response["key"];
                JToken tsd = Response["ts"];
                JToken ptsd = Response["pts"];
                LP_server = server.ToString();
                key = keyd.ToString();
                ts = (int)tsd;
                pts = (long)ptsd;
                Console.WriteLine($"[ULP] Server {server}, key {key}, ts {ts}, pts {pts}");
                return;
            }
            //Console.WriteLine(result);
        }
private static async Task WorkCommand(JToken objectInfo)
        {
            if (objectInfo == null)
            {
                return;
            }
            Cache Cache = new();
            vk Vk = new(Cache);
            Vk.LoadParams(objectInfo);
            //Console.WriteLine($"[ULP] Message from {Vk.m_from_id} ({Vk.m_peer_id}): {Vk.m_text}");
        }
    }
}
