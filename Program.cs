using Google.Protobuf.Collections;
using Google.Protobuf;
using GrandManager.engine;
using GrandManager.engine.DataBaseClass;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Utilities;
using QuickChart;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Security.Policy;
using System.Text;
using Ubiety.Dns.Core;
using static System.Net.Mime.MediaTypeNames;
using ThreadState = System.Threading.ThreadState;
using GrandManager.engine.properties;
using System.Numerics;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.SKCharts;
using GrandManager.engine.cmds;
using GrandManager.engine.API;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
//using Excel = Microsoft.Office.Interop.Excel;

namespace GrandManager
{
    internal class Program
    {
        private static settings settings = new settings();

        private static string LP_server = "";
        private static string key = "";
        private static long ts;

        public static bool is_beta = false;
        public static bool is_gamma = false;
        public static bool is_pot = true;

        public static TimeZoneInfo moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
        public static DateTimeOffset moscowTime = DateTimeOffset.UtcNow + moscowTimeZone.BaseUtcOffset;

        //private static long micro_start_command = 0;
        //private static long micro_finish_command = 0;
        static async Task Main(string[] args)
        {
            settings.SetATokens();
            
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            Thread myThread = new Thread(AdminInfo.ConsoleDeb);
                                                               // запускаем дебаг
            myThread.Start();

            LoaderAPI.preloaderAllCmds();
            LoaderAPI.loadAllCmds();



            


            var testEncode = SysUtils.EncryptString("[INFO] Test Encoded Message");
            Console.WriteLine("[INFO] Test encoded: " + testEncode);
            //Z11hDKgEs23iLMVWwnvbomNJieannCZ4fIRSHyjccQn+nJn3qJd43aN1briB8So=
            //FpDD7wu6s9mYOdbcrBHMlay2bBXUIdCkSSyV-EnwRuYYe60aMkyAjGcLBrjGjLw
            Console.WriteLine("[INFO] Test decoded: " + SysUtils.DecryptString(testEncode));

            //var Imgaes = new Charts();
            //Imgaes.GetImgae();


            // ЗАПУСКАЕМ ПОЛУЧЕНИЕ БАЗЫ ДАННЫХ С ДАННЫМИ
            Cache Cache = new();
            Database dbh = new Database(Cache);

            LoadProperties(dbh);

            // LONGPOLL
            //if (is_beta)
            //{
                Thread handler = new Thread(startHandlerLP);
                handler.Start();
            //}

            //CreatePointGraph();

            
            // ----------------------------------------


            

            // ХЕНДЛЕР ОБНОВЛЕНИЯ СТАТИСТИКИ В БД
            Thread handlere = new Thread(statsHandler);
            handlere.Start();

            Thread del_messages = new Thread(HandlerDelMessages);
            del_messages.Start();

            // ----------- handler api
            //Thread handlerApi = new Thread(HandlerVkApi);
            //handlerApi.Start();

            // ----------- CHECK GAMES
            Thread handlerGames = new Thread(HandlerGames);
            handlerGames.Start();

            // ----------- CHECK USERS
            Thread handlerVkOnline = new Thread(HandlerVkOnline);
            handlerVkOnline.Start();

            System.Net.IPAddress ipaddress = System.Net.IPAddress.Parse("0.0.0.0");//System.Net.IPAddress.Parse("192.168.1.56"); //System.Net.IPAddress.Parse("185.228.233.130");
            //server listen = new server();
            //listen.start(ipaddress, 86, 150, "");

            //ThreadPool.SetMinThreads(2, 2);
            //ThreadPool.SetMinThreads(4, 4);

            Cache cach = new();
            //if (is_beta)
            //{
                cach.Set("hash_syst", new Dictionary<string, string>()
                                            { { "access_token", "system" }, { "user_id", "251372816" }, { "hash", "syst" } }, 150);
            //}



            newserver server = new newserver("0.0.0.0", 100);
            server.Start();

            //getExel();

            ///////////////////////////////////////////////////////////// USER LONG POLL

            Thread handlerUserLP = new Thread(UserLongPoll.startHandlerLP);
            //handlerUserLP.Start();

            //////////////////////////////////////////////////////////////




            while (true)
            {
                Thread.Sleep(10000);
                var cmd = Console.ReadLine();
                if (cmd == "sync")
                {
                 Commands.syncAllPeers(dbh);
                }
                else if (cmd == "test")
                {
                    long userId = 251372816; // ID Павла Дурова
                    long? dateCreated = await VKFetcher.GetDateCreatedAsync(userId);

                    if (dateCreated.HasValue)
                    {
                        // Конвертируем Unix time в обычную дату для вывода
                        DateTime registrationDate = DateTimeOffset.FromUnixTimeSeconds(dateCreated.Value).UtcDateTime;
                        Console.WriteLine($"Дата регистрации пользователя {userId}: {registrationDate:yyyy-MM-dd HH:mm:ss} UTC");
                    }
                    else
                    {
                        Console.WriteLine($"Не удалось получить дату регистрации для пользователя {userId}.");
                    }
                }
                Process proc = Process.GetCurrentProcess();
                long memoryUsage = proc.PrivateMemorySize64 / (1024 * 1024);
                if (memoryUsage >= 1000)
                {
                    long totalMemory = GC.GetTotalMemory(true) / (1024 * 1024);
                    Console.WriteLine($"WORK OUT OF MEMORY 3GB - memoryUsage = {memoryUsage} MB | totalMemory = {totalMemory} MB");

                    void CreateGcdump(int processId, string outputPath)
                    {
                        // Формирование строки аргументов
                        string arguments = $"collect -p {processId} -o \"{outputPath}\"";

                        // Запуск dotnet-gcdump
                        Process.Start("dotnet-gcdump", arguments);
                    }

                    // Пример использования:

                    int currentProcessId = Process.GetCurrentProcess().Id;
                    Console.WriteLine($"Stopping threads.. Main Process ID = " + currentProcessId);
                    foreach (ProcessThread pThread in Process.GetCurrentProcess().Threads)
                    {
                        if (pThread.Id != Thread.CurrentThread.ManagedThreadId)
                        {
                            try
                            {
                                // Находим управляемый поток по ID (через Reflection)
                                int threadId = pThread.Id;
                                Thread thread = null;

                                foreach (Thread t in AppDomain.CurrentDomain.GetAssemblies()
                                                                         .SelectMany(a => a.GetTypes())
                                                                         .Where(t => t.IsSubclassOf(typeof(Thread)) && !t.IsAbstract)
                                                                         .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                                                                         .Where(f => f.FieldType == typeof(Thread))
                                                                         .Select(f => f.GetValue(null) as Thread))
                                {
                                    if (t?.ManagedThreadId == threadId)
                                    {
                                        thread = t;
                                        break;
                                    }
                                }


                                if (thread != null)
                                {
                                    thread.Abort(); // **ОПАСНО!**
                                    Console.WriteLine($"Поток с ID {threadId} завершен.");
                                }
                                else
                                {
                                    Console.WriteLine($"Поток с ID {threadId} не найден.");
                                }

                            }
                            catch (ThreadStateException ex)
                            {
                                Console.WriteLine($"Ошибка при завершении потока {pThread.Id}: {ex.Message}");
                            }
                            catch (ArgumentException ex)
                            {
                                Console.WriteLine($"Ошибка при получении потока (ArgumentException): {ex.Message}");
                            }
                            catch (Exception ex) // Ловим другие потенциальные исключения
                            {
                                Console.WriteLine($"Ошибка при завершении потока {pThread.Id}: {ex.Message}");
                            }
                        }
                    }
                    CreateGcdump(currentProcessId, "/root/dump/memory.gcdump");

                    Environment.Exit(1);
                }

            }


            //Console.WriteLine(settings.ACCESS_TOKEN);

        }
       
        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "qwertyuioplkjhgfdsazxcvbnmQWERTYUIOPLKJHGFDSAZXCVBNM0123456789__";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static void LoadProperties(Database dbh)
        {
            var successful_houses = houses.load(dbh);
            if (successful_houses)
            {
                AdminInfo.last_string = $"[DB] Loaded {houses.list.Count()} houses";
                Console.WriteLine($"[DB] Loaded {houses.list.Count()} houses");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load houses";
                Console.WriteLine("[DB] Error load houses");
            }

            var successful_cars = cars.load(dbh);
            if (successful_cars)
            {
                AdminInfo.last_string = $"[DB] Loaded {cars.list.Count()} cars";
                Console.WriteLine($"[DB] Loaded {cars.list.Count()} cars");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load cars";
                Console.WriteLine("[DB] Error load cars");
            }

            var successful_boats = boats.load(dbh);
            if (successful_boats)
            {
                AdminInfo.last_string = $"[DB] Loaded {boats.list.Count()} boats";
                Console.WriteLine($"[DB] Loaded {boats.list.Count()} boats");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load boats";
                Console.WriteLine("[DB] Error load boats");
            }

            var successful_airplanes = airplanes.load(dbh);
            if (successful_airplanes)
            {
                AdminInfo.last_string = $"[DB] Loaded {airplanes.list.Count()} airplanes";
                Console.WriteLine($"[DB] Loaded {airplanes.list.Count()} airplanes");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load airplanes";
                Console.WriteLine("[DB] Error load airplanes");
            }

            var successful_helicopters = helicopters.load(dbh);
            if (successful_helicopters)
            {
                AdminInfo.last_string = $"[DB] Loaded {helicopters.list.Count()} helicopters";
                Console.WriteLine($"[DB] Loaded {helicopters.list.Count()} helicopters");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load helicopters";
                Console.WriteLine("[DB] Error load helicopters");
            }

            var successful_smartphones = smartphones.load(dbh);
            if (successful_smartphones)
            {
                AdminInfo.last_string = $"[DB] Loaded {smartphones.list.Count()} smartphones";
                Console.WriteLine($"[DB] Loaded {smartphones.list.Count()} smartphones");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load smartphones";
                Console.WriteLine("[DB] Error load smartphones");
            }

            var successful_computers = computers.load(dbh);
            if (successful_computers)
            {
                AdminInfo.last_string = $"[DB] Loaded {computers.list.Count()} computers";
                Console.WriteLine($"[DB] Loaded {computers.list.Count()} computers");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load computers";
                Console.WriteLine("[DB] Error load computers");
            }

            var successful_admins = admins.load(dbh);
            if (successful_admins)
            {
                AdminInfo.last_string = $"[DB] Loaded {settings.OWNER_IDS.Count()} admins";
                Console.WriteLine($"[DB] Loaded {settings.OWNER_IDS.Count()} admins");
            }
            else
            {
                AdminInfo.last_string = "[DB] Error load admins";
                Console.WriteLine("[DB] Error load admins");
            }
            return;
        }

        private static void CreatePointGraph()
        {

            /*Chart qc = new Chart();

            qc.Width = 500;
            qc.Height = 500;/*
            qc.Config = @"{
    type: 'bar',
    data: {
        labels: ['Q1', 'Q2', 'Q3', 'Q4'],
        datasets: [{
            label: 'Users',
            data: [50, 60, 70, 180]
        },
        {
            label: 'trash',
            data: [533, 100, 3, 76]
        }]
    }
}";*//*
            qc.Config = @"{
    type:'doughnut',
    data: {
            labels:[{text:'550',font:{size:72}},{text:'всего',font:{size:72}}],
            datasets: [{
                data: [50,60]
            }] },
    options: {
        plugins: {
            doughnutlabel: {
                labels: [{text:'550',font:{size:72}},{text:'всего',font:{size:72}}]
            }
        }
    }
}";
            qc.ToFile("dsds2.png");*/

            // Получим панель для рисования
            //GraphPane pane = new();

            // Очистим список кривых на тот случай, если до этого сигналы уже были нарисованы
            //pane.CurveList.Clear();

            // Создадим список точек
            //PointPairList list = new PointPairList();

            //double xmin = -50;
            //double xmax = 50;

            // Заполняем список точек
            //for (double x = xmin; x <= xmax; x += 0.01)
            //{
            // добавим в список точку
            //list.Add(x, x+2);
            //}

            // Создадим кривую с названием "Sinc",
            // которая будет рисоваться голубым цветом (Color.Blue),
            // Опорные точки выделяться не будут (SymbolType.None)
            //LineItem myCurve = pane.AddCurve("Sinc", list, Color.Blue, SymbolType.None);

            // Вызываем метод AxisChange (), чтобы обновить данные об осях.
            // В противном случае на рисунке будет показана только часть графика,
            // которая умещается в интервалы по осям, установленные по умолчанию

            //ZedGraph.AxisChange();

            // Обновляем график
            //ZedGraph.Invalidate();
        }

        public static void HandlerDelMessages()
        {
            Cache Cach = new();
            vk Vk = new(Cach);
            while (true)
            {
                if(vk.messagesForDelete.Count >= 1)
                {
                    try
                    {
                        string VkCodes = "";

                        long iter = 0;
                        foreach (KeyValuePair<string, string> entry in vk.messagesForDelete)
                        {
                            if (iter >= 25)
                            {
                                break;
                            }
                            else
                            {
                                Dictionary<string, string> paramesvk = new() { { "peer_id", entry.Key }, { "cmids", entry.Value }, { "delete_for_all", "1" } };
                                VkCodes = VkCodes + "var a = API.messages.delete(" + JsonConvert.SerializeObject(paramesvk) + "); return a;";
                                //vk.messagesForDelete.Remove(entry.Key);
                                vk.messagesForDelete.TryRemove(entry);
                            }
                            iter++;
                        }

                        _ = Vk.VkAPIs("execute", new Dictionary<string, string>()
                            {
                        { "code", VkCodes }
                    });
                    }
                    catch
                    {

                    }
                }
                Thread.Sleep(1000);
                /*Dictionary<string, string> parames = new() { { "peer_id", peerid.ToString() }, { "cmids", messid }, { "delete_for_all", "1" } };
            string vkCode = "var a = API.messages.delete(" + JsonConvert.SerializeObject(parames) + "); return a;";
            await Vk.VkAPI("execute", new Dictionary<string, string>()
            {
                { "code", vkCode }
            });*/
            }
        }

        public static async void HandlerVkApi()
        {
            while (true)
            {
                if (vk.waitMethods.Count >= 1)
                {
                    try
                    {
                        string VkCodes = "";

                        long iter = 0;
                        string str_return = " return {";
                        foreach (KeyValuePair<string, Dictionary<string, string>> entry in vk.waitMethods)
                        {
                            if (iter >= 25)
                            {
                                break;
                            }
                            else
                            {
                                if (entry.Value == null)
                                {
                                    vk.waitMethods.TryRemove(entry);
                                    break;
                                }
                                //Dictionary<string, string> paramesvk = new() { { "peer_id", entry.Key }, { "cmids", entry.Value }, { "delete_for_all", "1" } };
                                var method = entry.Value["method"];

                                entry.Value.Remove("method");
                                VkCodes = VkCodes + "var " + entry.Key + " = API." + method + "(" + JsonConvert.SerializeObject(entry.Value) + ");";
                                //vk.messagesForDelete.Remove(entry.Key);

                                str_return = str_return + "\"" + entry.Key + "\": " + entry.Key + ", ";
                                vk.waitMethods[entry.Key] = null;
                            }
                            iter++;
                        }

                        if (iter >= 25 || iter == 0)
                        {
                            Thread.Sleep(1);
                            continue;
                        }

#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                        Task.Run(async () =>
                        {
                            if (str_return != "")
                            {
                                str_return = str_return.Remove((str_return.Length - 2), 2);
                            }

                            str_return = str_return + " };";
                            VkCodes = VkCodes + str_return;



                            try
                            {
                                Cache cach = new();
                                vk Vk = new(cach);
                                var JObject = await Vk.VkAPIs("execute", new Dictionary<string, string>()
                                {
                                    { "code", VkCodes }
                                });

                                if (JObject == null)
                                {
                                    //Console.WriteLine(JObject);
                                    return;
                                }
                                if (!JObject.ContainsKey("response"))
                                {
                                    //Console.WriteLine(JObject);
                                    return;
                                }

                                var response = JObject["response"];

                                if (response != null)
                                {
                                    foreach (JProperty attributeProperty in response)
                                    {
                                        var element = response[attributeProperty.Name];


                                        var jsonObject = new JObject();
                                        jsonObject.Add("response", element);

                                        vk.waitResult.TryAdd(attributeProperty.Name, jsonObject);

                                    }
                                }
                            }
                            catch
                            {

                            }
                        });
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                    }
                    catch
                    {

                    }
                }
                Thread.Sleep(1);
                /*Dictionary<string, string> parames = new() { { "peer_id", peerid.ToString() }, { "cmids", messid }, { "delete_for_all", "1" } };
            string vkCode = "var a = API.messages.delete(" + JsonConvert.SerializeObject(parames) + "); return a;";
            await Vk.VkAPI("execute", new Dictionary<string, string>()
            {
                { "code", vkCode }
            });*/
            }
        }

        public static async void HandlerVkOnline()
        {
            Cache Cache = new();
            vk Vk = new(Cache);
            while (true)
            {
                try
                {
                    
                    var target_ids = Cache.Get< Dictionary<string, Dictionary <string, string> > >("users_online_check");
                    if (target_ids != null && target_ids.Count >= 1)
                    {
                        
                        foreach (var item in target_ids)
                        {
                            var data = await Vk.UsersGetVkInfo(new long[] { long.Parse(item.Value["id"]) }, true, "nom", true);
                            var back_status = item.Value["status"];
                            var item_id = item.Value["id"];
                            var peer_id = item.Value["peer_id"];
                            if (data.ContainsKey("id" + item.Key))
                            {
                                if (back_status != data["id" + item.Key].online)
                                {
                                    if (data["id" + item.Key].online == "1")
                                    {
                                        Vk.MessagesSend("[" + DateTimeOffset.Now.ToString("g") + "] " +
                                            "[id" + item.Value["id"] + "|" + data["id" + item.Key].first_name + " " + data["id" + item.Key].last_name + "] зашёл в сеть.", 0,
                                            long.Parse(peer_id));
                                    }
                                    else
                                    {
                                        Vk.MessagesSend("[" + DateTimeOffset.Now.ToString("g") + "] " +
                                            "[id" + item.Value["id"] + "|" + data["id" + item.Key].first_name + " " + data["id" + item.Key].last_name + "] вышел из сети.", 0,
                                            long.Parse(peer_id));
                                    }
                                    item.Value["status"] = data["id" + item.Key].online;
                                    target_ids[item.Key] = item.Value;
                                    Cache.Set("users_online_check", target_ids, 600);
                                    Thread.Sleep(15000);
                                    continue;
                                }
                            }
                            Thread.Sleep(15000);
                        }
                        Thread.Sleep(15000);
                    }
                }
                catch (Exception ex)
                {
                    Vk.MessagesSend("Ошибка - " + ex.Message, 0, 1251372816);
                }

                Thread.Sleep(15000);
            }
        }

        public static async void HandlerGames()
        {
            while (true)
            {
                if (vk.mafios.Count >= 1)
                {
                    try
                    {
                        Cache Cache = new();
                        vk Vk = new(Cache);

                        Database dbh = new Database(Cache);

                        /*
                        peer_id
                        status (start - набор, night - ночь мафия выбирает жертву, day - голосование комиссара и выбор жителей)
                        date_create
                        date_start
                         */

                        foreach (KeyValuePair<string, Dictionary<string, string>> entry in vk.mafios)
                        {
                            long peer_id = long.Parse(entry.Key);
                            var data = entry.Value;
                            DateTimeOffset date_created = dbh.timeGetForMysql(data["date_create"]);
                            DateTimeOffset date_start = dbh.timeGetForMysql(data["date_start"]);
                            string status = data["status"];
                            if (status == "start" && date_start.ToUnixTimeSeconds() <= DateTimeOffset.Now.ToUnixTimeSeconds())
                            {
                                // запуск
                                var targetUsers = Cache.Get<Dictionary<long, Dictionary<string, string>>>("mafiaplayers_" + peer_id);
                                
                                if (targetUsers.Count <= 2)
                                {
                                    Vk.MessagesSend("🔪 Количество участников в мафию должно быть более трёх человек, " +
                                        "за нужное время игроки не были набраны, игра остановлена." +
                                        "<br>Игроков набрано: " + targetUsers.Count, 0, peer_id);
                                    Games.DeleteMafia(Cache, peer_id, targetUsers);
                                    
                                    vk.mafios.TryRemove(entry);
                                    continue;
                                }
                                else
                                {
                                    // Роли: Дон, комиссар, доктор  1 мафия, мирные жители
                                    long done = 0; // Дон
                                    long komissar = 0; // Комиссар
                                    List<long> people = new();
                                    long doctor = 0; // Доктор
                                    List<long> mafiose = new();

                                    List<long> players = new();
                                    foreach (KeyValuePair<long, Dictionary<string, string>> item in targetUsers)
                                    {
                                        players.Add(item.Key);
                                    }

                                    Random rnd = new();
                                    int add_id = rnd.Next(0, players.Count());

                                    done = players[add_id];
                                    players.RemoveAt(add_id);

                                    add_id = rnd.Next(0, players.Count());

                                    komissar = players[add_id];
                                    players.RemoveAt(add_id);

                                    if (targetUsers.Count <= 4)
                                    {
                                        people = players;
                                    }
                                    else
                                    {
                                        if (targetUsers.Count >= 5 && targetUsers.Count < 7)
                                        {
                                            add_id = rnd.Next(0, players.Count());
                                            doctor = players[add_id];
                                            players.RemoveAt(add_id);
                                            people = players;
                                        }
                                        else if (targetUsers.Count >= 7 && targetUsers.Count < 13)
                                        {
                                            add_id = rnd.Next(0, players.Count());
                                            doctor = players[add_id];
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(0, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            people = players;
                                        }
                                        else if (targetUsers.Count >= 13 && targetUsers.Count < 20)
                                        {
                                            add_id = rnd.Next(0, players.Count());
                                            doctor = players[add_id];
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(0, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(1, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            people = players;
                                        }
                                        else
                                        {
                                            add_id = rnd.Next(0, players.Count());
                                            doctor = players[add_id];
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(0, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(0, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            add_id = rnd.Next(0, players.Count());
                                            mafiose.Add(players[add_id]);
                                            players.RemoveAt(add_id);

                                            people = players;
                                        }
                                    }

                                    if (targetUsers.ContainsKey(done))
                                    {
                                        targetUsers[done]["role"] = "done";
                                    }

                                    if (targetUsers.ContainsKey(komissar))
                                    {
                                        targetUsers[komissar]["role"] = "komissar";
                                    }

                                    if (targetUsers.ContainsKey(doctor))
                                    {
                                        targetUsers[doctor]["role"] = "doctor";
                                    }

                                    foreach (var item in mafiose)
                                    {
                                        if (targetUsers.ContainsKey(item))
                                        {
                                            targetUsers[item]["role"] = "mafia";
                                        }
                                    }

                                    foreach (var item in people)
                                    {
                                        if (targetUsers.ContainsKey(item))
                                        {
                                            targetUsers[item]["role"] = "people";
                                        }
                                    }

                                    Cache.Set("mafiaplayers_" + peer_id, targetUsers, 10);

                                    long[] nicks_get = new long[targetUsers.Count];
                                    long i = 0;
                                    foreach (KeyValuePair<long, Dictionary<string, string>> item in targetUsers)
                                    {
                                        nicks_get[i] = item.Key;
                                        i++;
                                    }

                                    var nicks = await Vk.UsersGetVkInfo(nicks_get);

                                    string buttons_don = "";
                                    string buttons_komissar = "";
                                    string buttons_doctor = "";


                                    var Gams = new Games(Cache, Vk, dbh);
                                    var status_nicks = await Gams.MafiaGetNotDeadPlayers(targetUsers);
                                    buttons_don = Gams.buttons_don;
                                    buttons_komissar = Gams.buttons_komissar;
                                    buttons_doctor = Gams.buttons_doctor;

                                   

                                    Vk.MessagesSend("🔪 Игра в мафию началась." +
                                        "<br>Игроков: " + targetUsers.Count + "<br><br>🧔 В живых:<br><br>" + status_nicks, 0, peer_id);

                                    Vk.MessagesSendMassive("🔪 Вы обычный мирный житель. Ваша главная цель — остаться в живых.",
                                        people);


                                    if (mafiose.Count >= 1)
                                    {
                                        Vk.MessagesSendMassive("🔪 Вы член преступной группировки.<br><br>" +
                                            "Началась ночь. Выберите, кого вы хотели-бы убить. Окончательно решение принимает Дон.",
                                        mafiose);
                                    }

                                    Vk.MessagesSend("🔪 Вы — глава преступной группировки (Дон)." +
                                        " Ваша задача - принимать окончательное решение об убийстве.<br><br>" +
                                        "Началась ночь. Выберите, кого вы хотите убить. Окончательное решение остается за вами.", 0, done, buttons_don);

                                    Vk.MessagesSend("🔪 Вы — комиссар полиции." +
                                        " Ваша задача - выяснить, кто у̶б̶и̶л̶ ̶М̶а̶р̶к̶а̶ является мафией.<br><br>" +
                                        "Началась ночь. Выберите, кого вы хотите ликвидиролвать.", 0, komissar, buttons_komissar);

                                    if (doctor != 0)
                                    {
                                        Vk.MessagesSend("🔪 Вы — доктор." +
                                        " Ваша задача - защищать мирных жителей от мафии.<br><br>" +
                                        "Началась ночь. Выберите, за кем вы будете наблюдать.", 0, doctor, buttons_doctor);
                                    }
                                    

                                    /*🌃 Наступает ночь. До утра: 1 мин.

👥 В живых:
🔹 К. Пушин
🔹 В. Копиков
🔹 Н. Обыкновенный
🔹 А. Ковальчук

📰 Роли: Дон, комиссар*/
                                    Cache.Set("mafia_" + peer_id, "night", 10);
                                    vk.mafios[entry.Key]["status"] = "night";
                                    vk.mafios[entry.Key]["date_start"] = dbh.timeGetForString(DateTimeOffset.Now.AddMinutes(1));
                                    //Cache.Delete("mafiaplayers_" + peer_id);
                                    //Cache.Delete("mafia_" + peer_id);
                                    //vk.mafios.TryRemove(entry);
                                    continue;
                                }
                            }
                            else if (status == "night" && date_start.ToUnixTimeSeconds() <= DateTimeOffset.Now.ToUnixTimeSeconds())
                            {
                                var targetUsers = Cache.Get<Dictionary<long, Dictionary<string, string>>>("mafiaplayers_" + peer_id);

                                Cache.Set("mafia_" + peer_id, "day", 10);
                                vk.mafios[entry.Key]["status"] = "day";
                                vk.mafios[entry.Key]["date_start"] = dbh.timeGetForString(DateTimeOffset.Now.AddMinutes(1));

                                var Gams = new Games(Cache, Vk, dbh);

                                //var nicks_live = await Gams.MafiaGetNotDeadPlayers(targetUsers);
                                //var nicks_dead = await Gams.MafiaGetDeadPlayers(targetUsers);

                                List<long> target_Died = new();

                                var don_select = Cache.Get<string>("mafia_don_selected_" + peer_id);
                                var komissar_select = Cache.Get<string>("komissar_maf_selected_" + peer_id);
                                var doctor_select = Cache.Get<string>("doctor_don_selected_" + peer_id);

                                long target_don_select = 0;
                                long target_komissar_select = 0;
                                bool target_komissar_select_ischeck = false;
                                long target_doctor_select = 0;

                                if (doctor_select != null && doctor_select != "" && doctor_select != "0")
                                {
                                    target_doctor_select = long.Parse(doctor_select);
                                }

                                if (don_select != null && don_select != "" && don_select != "0")
                                {
                                    target_don_select = long.Parse(don_select);
                                }

                                if (komissar_select != null && komissar_select != "" && komissar_select != "0")
                                {
                                    target_komissar_select = long.Parse(komissar_select);
                                }
                                else
                                {
                                    var komissar_check = Cache.Get<string>("komissarCheck_maf_selected_" + peer_id);
                                    if (komissar_check != null && komissar_check != "" && komissar_check != "0")
                                    {
                                        target_komissar_select = long.Parse(komissar_check);
                                        target_komissar_select_ischeck = true;
                                    }
                                }

                                long idKomissar = 0;
                                long idMafia = 0;
                                long idDoctor = 0;

                                foreach (var item in targetUsers)
                                {
                                    if (item.Value["role"] == "komissar" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "komissar")
                                        {
                                            idKomissar = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "komissar")
                                            {
                                                idKomissar = item.Key;
                                                continue;
                                            }
                                        }
                                    }

                                    if (item.Value["role"] == "done" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "done")
                                        {
                                            idMafia = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "done")
                                            {
                                                idMafia = item.Key;
                                                continue;
                                            }
                                        }
                                    }

                                    if (item.Value["role"] == "doctor" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "doctor")
                                        {
                                            idDoctor = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "doctor")
                                            {
                                                idDoctor = item.Key;
                                                continue;
                                            }
                                        }
                                    }
                                }

                                if (target_don_select == 0)
                                {
                                    var res_hods = Cache.Get<string>("skipped_hods_done_" + peer_id);
                                    if (res_hods != null && res_hods != "")
                                    {
                                        var target_hods = long.Parse(res_hods) + 1;
                                        Cache.Set("skipped_hods_done_" + peer_id, target_hods.ToString(), 5);
                                        if (target_hods >= 2)
                                        {
                                            target_don_select = idMafia;
                                        }
                                    }
                                    else
                                    {
                                        Cache.Set("skipped_hods_done_" + peer_id, "1", 5);
                                    }
                                }

                                if (target_don_select != target_doctor_select && target_don_select != 0)
                                {
                                    if (!target_Died.Contains(target_don_select))
                                    {
                                        if (target_don_select != 0)
                                        {
                                            target_Died.Add(target_don_select);
                                            targetUsers[target_don_select]["last_role"] = targetUsers[target_don_select]["role"];
                                            targetUsers[target_don_select]["role"] = "dead";
                                        }
                                    }
                                }

                                if (target_komissar_select == 0)
                                {
                                    var res_hods = Cache.Get<string>("skipped_hods_komissar_" + peer_id);
                                    if (res_hods != null && res_hods != "")
                                    {
                                        var target_hods = long.Parse(res_hods) + 1;
                                        Cache.Set("skipped_hods_komissar_" + peer_id, target_hods.ToString(), 5);
                                        if (target_hods >= 2)
                                        {
                                            target_komissar_select = idKomissar;
                                        }
                                    }
                                    else
                                    {
                                        Cache.Set("skipped_hods_komissar_" + peer_id, "1", 5);
                                    }
                                }

                                if (target_komissar_select != target_doctor_select && target_komissar_select != 0)
                                {
                                    if (!target_Died.Contains(target_komissar_select))
                                    {

                                        if (target_komissar_select != 0 && target_don_select != idKomissar && !target_komissar_select_ischeck)
                                        {
                                            target_Died.Add(target_komissar_select);
                                            targetUsers[target_komissar_select]["last_role"] = targetUsers[target_komissar_select]["role"];
                                            targetUsers[target_komissar_select]["role"] = "dead";
                                        }
                                    }
                                }


                                Cache.Set("mafiaplayers_" + peer_id, targetUsers, 10);

                                string text = "🌅 Наступает утро.<br><br>";
                                
                                //var nicks_dead = await Gams.MafiaGetDeadPlayers(targetUsers);

                                if (target_Died.Count >= 1)
                                {
                                    text = text + "💀 Не выжили:<br>";

                                    var nicks = await Vk.UsersGetVkInfo(target_Died.ToArray());
                                    foreach (var item in target_Died)
                                    {
                                        if (nicks.ContainsKey("id" + item))
                                        {
                                            text = text + "— " + nicks["id" + item.ToString()].first_name + " " +
                                                nicks["id" + item.ToString()].last_name + "<br>";
                                        }
                                        else
                                        {
                                            text = text + $"— [id{item}|пользователь]<br>";
                                        }
                                    }
                                }

                                target_Died.Clear();

                                if (await Gams.MafiaGetWinners(targetUsers, peer_id, entry))
                                {
                                    continue;
                                }
                                else
                                {
                                    text = text + "<br>💭 Время обсудить то, что произошло ночью";
                                    Vk.MessagesSend(text, 0, peer_id);
                                }
                            }
                            else if (status == "day" && date_start.ToUnixTimeSeconds() <= DateTimeOffset.Now.ToUnixTimeSeconds())
                            {
                                var targetUsers = Cache.Get<Dictionary<long, Dictionary<string, string>>>("mafiaplayers_" + peer_id);

                                Cache.Set("mafia_" + peer_id, "day2", 10);
                                vk.mafios[entry.Key]["status"] = "day2";
                                vk.mafios[entry.Key]["date_start"] = dbh.timeGetForString(DateTimeOffset.Now.AddMinutes(1));

                                string buttons_peoples = "";


                                var Gams = new Games(Cache, Vk, dbh);

                                var status_nicks = await Gams.MafiaGetNotDeadPlayers(targetUsers);
                                buttons_peoples = Gams.buttons_peoples;
                                List<long> peoplesList = new();
                                foreach (var item in targetUsers)
                                {
                                    if (item.Value["role"] != "dead")
                                    {
                                        peoplesList.Add(item.Key);
                                    }
                                }


                                Vk.MessagesSend("💀 Время линчевать кого-то на дневном суде.", 0, peer_id);
                                Vk.MessagesSendMassive("🔪 Выберите, за кого хотите проголосовать на дневном суде.",
                                        peoplesList, buttons_peoples);

                            }
                            else if (status == "day2" && date_start.ToUnixTimeSeconds() <= DateTimeOffset.Now.ToUnixTimeSeconds())
                            {
                                var targetUsers = Cache.Get<Dictionary<long, Dictionary<string, string>>>("mafiaplayers_" + peer_id);

                                Cache.Set("mafia_" + peer_id, "night", 10);
                                vk.mafios[entry.Key]["status"] = "night";
                                vk.mafios[entry.Key]["date_start"] = dbh.timeGetForString(DateTimeOffset.Now.AddMinutes(1));


                                string buttons_don = "";
                                string buttons_komissar = "";
                                string buttons_doctor = "";


                                var Gams = new Games(Cache, Vk, dbh);

                                List<long> target_Died = new();

                                var target_selected_mafia = Cache.Get<Dictionary<long, List<long>>>("peoples_mafia_selected_" + peer_id);
                                if (target_selected_mafia != null && target_selected_mafia.Count >= 1)
                                {
                                    long max_voices = 0;
                                    long max_id = 0;
                                    List<long> doubleVoices = new();
                                    foreach (var item in target_selected_mafia)
                                    {
                                        long target_voices = item.Value.Count;
                                        if (target_voices > max_voices)
                                        {
                                            max_voices = item.Value.Count;
                                            max_id = item.Key;
                                        }
                                        else if ((target_voices == max_voices) && item.Key != max_id)
                                        {
                                            max_id = item.Key;
                                            doubleVoices.Add(item.Key);
                                        }
                                    }
                                    bool isDouble = false;
                                    if (doubleVoices.Count > 0)
                                    {
                                        foreach (var item in doubleVoices)
                                        {
                                            if (item == max_id)
                                            {
                                                isDouble = true;
                                                break;
                                            }
                                        }
                                    }
                                    if (isDouble)
                                    {
                                        Vk.MessagesSend("Голоса разошлись. Никто не казнен.", 0, peer_id);
                                    }
                                    else
                                    {
                                        target_Died.Add(max_id);
                                    }
                                }
                                else
                                {
                                    Vk.MessagesSend("Никто не проголосовал. Никто не казнен.", 0, peer_id);
                                }

                                string text = "🌃 Наступает ночь. До утра: 1 мин.<br><br>";
                                if (target_Died.Count >= 1)
                                {
                                    foreach (var item in target_Died)
                                    {
                                        if (targetUsers.ContainsKey(item))
                                        {
                                            targetUsers[item]["last_role"] = targetUsers[item]["role"];
                                            targetUsers[item]["role"] = "dead";
                                        }
                                        else
                                        {
                                            targetUsers.Add(item, new Dictionary<string, string>()
                                            {
                                                { "role", "dead" },
                                                { "last_role", "people" },
                                            });
                                        }
                                    }
                                    Cache.Set("mafiaplayers_" + peer_id, targetUsers, 10);

                                    text = text + "💀 Не выжили:<br>";

                                    var nicks = await Vk.UsersGetVkInfo(target_Died.ToArray());
                                    foreach (var item in target_Died)
                                    {
                                        if (nicks.ContainsKey("id" + item))
                                        {
                                            text = text + "— " + nicks["id" + item.ToString()].first_name + " " +
                                                nicks["id" + item.ToString()].last_name + "<br>";
                                        }
                                        else
                                        {
                                            text = text + $"— [id{item}|пользователь]<br>";
                                        }
                                    }
                                    text += "<br>";
                                }
                                if (await Gams.MafiaGetWinners(targetUsers, peer_id, entry))
                                {
                                    Games.DeleteMafia(Cache, peer_id, targetUsers);
                                    continue;
                                }
                                else
                                {
                                    text = text + "<br>";
                                }
                                Games.ClearSelectedMafia(Cache, peer_id);
                                var deadPl = Gams.MafiaGetNotDeadPlayers(targetUsers);
                                buttons_komissar = Gams.buttons_komissar;
                                buttons_don = Gams.buttons_don;
                                buttons_doctor = Gams.buttons_doctor;

                                long idKomissar = 0;
                                long idMafia = 0;
                                long idDoctor = 0;

                                foreach (var item in targetUsers)
                                {
                                    if (item.Value["role"] == "komissar" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "komissar")
                                        {
                                            idKomissar = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "komissar")
                                            {
                                                idKomissar = item.Key;
                                                continue;
                                            }
                                        }
                                    }

                                    if (item.Value["role"] == "done" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "done")
                                        {
                                            idMafia = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "done")
                                            {
                                                idMafia = item.Key;
                                                continue;
                                            }
                                        }
                                    }

                                    if (item.Value["role"] == "doctor" || item.Value.ContainsKey("last_role"))
                                    {
                                        if (item.Value["role"] == "doctor")
                                        {
                                            idDoctor = item.Key;
                                            continue;
                                        }
                                        else if (item.Value.ContainsKey("last_role"))
                                        {
                                            if (item.Value["last_role"] == "doctor")
                                            {
                                                idDoctor = item.Key;
                                                continue;
                                            }
                                        }
                                    }
                                }

                                var status_nicks = await Gams.MafiaGetNotDeadPlayers(targetUsers);
                                target_Died.Clear();
                                Vk.MessagesSend(text +
                                   "<br><br>🧔 В живых:<br>" + status_nicks, 0, peer_id);



                                //if (mafiose.Count >= 1)
                                //{
                                    //Vk.MessagesSendMassive("🔪 Вы член преступной группировки.<br><br>" +
                                        //"Началась ночь. Выберите, кого вы хотели-бы убить. Окончательно решение принимает Дон.",
                                    //mafiose);
                                //}

                                Vk.MessagesSend("Началась ночь. Выберите, кого вы хотите убить. Окончательное решение остается за вами.", 0, idMafia, buttons_don);

                                Vk.MessagesSend("Началась ночь. Выберите, кого вы хотите ликвидиролвать, или проверить.", 0, idKomissar, buttons_komissar);

                                if (idDoctor != 0)
                                {
                                    Vk.MessagesSend("Началась ночь. Выберите, за кем вы будете наблюдать.", 0, idDoctor, buttons_doctor);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AdminInfo.ErrorAddCommand(ex, new vk(new Cache()));
                        AdminInfo.last_string = ex.Message;
                        Console.WriteLine("Mafia error: " + ex.Message + " " + ex.StackTrace + " | " + ex.HelpLink);
                    }
                }
                Thread.Sleep(1000);
                /*Dictionary<string, string> parames = new() { { "peer_id", peerid.ToString() }, { "cmids", messid }, { "delete_for_all", "1" } };
            string vkCode = "var a = API.messages.delete(" + JsonConvert.SerializeObject(parames) + "); return a;";
            await Vk.VkAPI("execute", new Dictionary<string, string>()
            {
                { "code", vkCode }
            });*/
            }
        }

        public static async void startHandlerLP()
        {
            Action onCompleted = () =>
            {
                vk.addError(99999, "Error LP, restart server");
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
                var result = await Vk.Get(LP_server + "?act=a_check&key=" + key + "&ts=" + ts + "&wait=25");
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
                    Console.WriteLine("Error ts");
                    Console.WriteLine(parames);
                    break;
                }
                ts = (int)parames["ts"];

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

#pragma warning disable CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен
                Task.Run(async () =>
                {
                    try
                    {
                        await Parallel.ForEachAsync(updates, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (upd, token) =>
                        {
                            if (upd != null)
                            {
                                if (upd["type"].ToString() != "message_new" && upd["type"].ToString() != "message_event"
                                    && upd["type"].ToString() != "like_add" && upd["type"].ToString() != "like_remove"
                                    && upd["type"].ToString() != "wall_reply_new" && upd["type"].ToString() != "wall_reply_delete"
                                    && upd["type"].ToString() != "wall_repost" && upd["type"].ToString() != "message_edit" && upd["type"].ToString() != "donut_subscription_create")
                                {
                                    return;
                                }

                                if (upd["type"].ToString() == "message_new" || upd["type"].ToString() == "message_event" || upd["type"].ToString() == "message_edit")
                                {
                                    //Console.WriteLine("Событие: " + upd.ToString());
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerCommand(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "like_add")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerLike(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "like_remove")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerDisLike(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "wall_reply_new")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerComment(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "wall_reply_delete")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerDeleteComment(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "wall_repost")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    workerRepost(upd["object"]);
                                    return;
                                }

                                if (upd["type"].ToString() == "donut_subscription_create")
                                {
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    DONUT_WORKER(upd["object"]);
                                    return;
                                }
                                //
                            }
                        });
                    }
                    catch
                    {
                        // Log the exception here if needed
                    }
                });
#pragma warning restore CS4014 // Так как этот вызов не ожидается, выполнение существующего метода продолжается до тех пор, пока вызов не будет завершен

                /*Task.Run(async () =>
                {
                    try
                    {
                        foreach (var upd in updates)
                        {
                            if (upd != null)
                            {
                                if (upd["type"].ToString() != "message_new" && upd["type"].ToString() != "message_event"
                                    && upd["type"].ToString() != "like_add" && upd["type"].ToString() != "like_remove"
                                    && upd["type"].ToString() != "wall_reply_new" && upd["type"].ToString() != "wall_reply_delete"
                                    && upd["type"].ToString() != "wall_repost" && upd["type"].ToString() != "message_edit")
                                {
                                    continue;
                                }

                                if (upd["type"].ToString() == "message_new" || upd["type"].ToString() == "message_event" || upd["type"].ToString() == "message_edit")
                                {
                                   
                                    Task.Run(async () => workerCommand(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }

                                if (upd["type"].ToString() == "like_add")
                                {
                                    Task.Run(async () => workerLike(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }

                                if (upd["type"].ToString() == "like_remove")
                                {
                                    Task.Run(async () => workerDisLike(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }

                                if (upd["type"].ToString() == "wall_reply_new")
                                {
                                    Task.Run(async () => workerComment(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }

                                if (upd["type"].ToString() == "wall_reply_delete")
                                {
                                    Task.Run(async () => workerDeleteComment(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }

                                if (upd["type"].ToString() == "wall_repost")
                                {
                                    Task.Run(async () => workerRepost(upd["object"]));
                                    Interlocked.Increment(ref AdminInfo.threads);
                                    continue;
                                }
                                //
                            }

                        }
                    }
                    catch
                    {

                    }
                });*/



                //long finish = settings.GetMicroTime();
                //long finish = DateTime.Now.Ticks;
                //TimeSpan interval = settings.TimeSpanFromMs(finish - micro_start_command);
                //decimal sd = settings.getSecondsFromMicros(finish - micro_start_command);
                //Console.WriteLine("datetime = " + sd + " с.");
            }
        }

        public static async void workerCommand(JToken? object_data)
        {
            //Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            try
            {
                try
                {
                    var seconds = DateTimeOffset.Now.ToUnixTimeSeconds();
                    if (AdminInfo.request_per_seconds.ContainsKey(seconds))
                    {
                        var req = AdminInfo.request_per_seconds[seconds];

                        AdminInfo.request_per_seconds.TryUpdate(seconds, req + 1, req);
                    }
                    else
                    {
                        AdminInfo.request_per_seconds.TryAdd(seconds, 1);
                    }
                }
                catch
                {

                }

                long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();

                long start_cmd = settings.GetMicroTime();
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }
                Cache Cache = new();
                vk Vk = new(Cache);
                Vk.LoadParams(object_data);

                if (Vk.is_callbackButton)
                {
                    string event_data = Vk.GenerateEventData("Команда выполнена.");
                    Vk.MessagesSendEventAnswer(Vk.event_id, Vk.m_from_id, Vk.m_peer_id, event_data);
                }

                bool is_cmd = false;
                

                Database dbh = new Database(Cache);
                Commands cmd = new(Vk, dbh, settings, Cache);


                //long finish_cmd = 0;
                if (settings.isPeer(Vk.m_peer_id))
                {
                    // КОНФЕРЕНЦИЯ
                    
                    Dictionary<string, string>? peer_info = dbh.peersGetPeer(Vk.m_peer_id);
                    if (peer_info == null)
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                }


                var user_info = dbh.peersGetMember(Vk.m_peer_id, Vk.m_from_id);
                
                if (Vk.m_action != null)
                {
                    if (Vk.m_action.type == Vk.m_action.TYPE_CHAT_INVITE_USER)
                    {
                        await cmd.inviteConversation();
                    }
                    else if(Vk.m_action.type == Vk.m_action.TYPE_CHAT_KICK_USER)
                    {
                        await cmd.kickConversation();
                    }
                }
                else
                {

                    //if (await cmd.checkBotBan())
                    //{
                    // Interlocked.Decrement(ref AdminInfo.threads);
                    // return;
                    //}
                    
                    if (cmd.systemMute(user_info))
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    // Проверяем, есть ли PAYLOAD у сообщения


                    if (Vk.m_payload != null)
                    {
                        //var payload = JObject.Parse(Vk.m_payload);
                        //if (Vk.m_payload["execute"])

                        if (Vk.m_payload["execute"] != null)
                        {
                            string ka = "";
                            try
                            {
                                ka = Vk.m_payload["execute"].ToString();
                            }
                            catch
                            {

                            }
                            Vk.m_text = "!" + ka;//payload.ToString();
                            is_cmd = await cmd.handler_command();
                        }
                        
                        //Vk.MessagesSend(Vk.m_payload.ToString(), 1);
                    }
                    else
                    {
                        if (Vk.m_text != "" && Vk.m_from_id != Vk.m_peer_id && settings.ENABLE_MESSAGES_LOG)
                        {
                            dbh.messageInsert(Vk.m_peer_id, Vk.m_from_id, Vk.m_text);
                        }
                    }
                    // -----
                    
                    // ------------------------- ОБРАБОТКА КОМАНД И МЕНЮ
                    MenuList MenuList = new(Cache);
                    long menu_select = MenuList.getMenuSelect(Vk.m_from_id);
                    if (menu_select == 0 && Vk.m_payload == null)
                    {
                        try
                        {
                            is_cmd = await cmd.handler_command();
                            //finish_cmd = settings.GetMicroTime();
                        }
                        catch
                        {
                            is_cmd = false;
                        }
                    }
                    else
                    {
                        if (Vk.m_payload == null)
                        {
                            is_cmd = await cmd.handler_menu(menu_select);
                        }
                    }

                    // ------------------------- ОБРАБОТКА КОМАНД
                }

                dbh.Dispose();

                long finish_cmd = settings.GetMicroTime();
                //Console.WriteLine("Last time cmd  - " + settings.getSecondsFromMicros(finish_cmd - start_cmd));

                if (is_cmd)
                {
                    AdminInfo.addAvergCommand(settings.getSecondsFromMicros(finish_cmd - start_cmd));
                }
                else
                {
                    AdminInfo.addAvergRequest(settings.getSecondsFromMicros(finish_cmd - start_cmd));
                }

                
                //long timeout = currentTime - Vk.m_date;
                
                //AdminInfo.addRequestPing(Vk.m_date, timeout*-1);

                statsAddLog(Cache, Vk, is_cmd);
                //Thread.Sleep(500);
                Interlocked.Decrement(ref AdminInfo.threads);
                    //Console.WriteLine("ВОРКЕР КОМАНДЫ ПРОШЕЛ");
            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки команды", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void workerLike(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);
                //Vk.LoadParams(object_data);
                long liker_id = 0;
                long object_owner_id = 0;
                long object_id = 0;
                string object_type = "";
                if (object_data["liker_id"] != null)
                {
                    liker_id = long.Parse(object_data["liker_id"].ToString());
                }
                if (object_data["object_owner_id"] != null)
                {
                    object_owner_id = long.Parse(object_data["object_owner_id"].ToString());
                }
                if (object_data["object_id"] != null)
                {
                    object_id = long.Parse(object_data["object_id"].ToString());
                }
                if (object_data["object_type"] != null)
                {
                    object_type = object_data["object_type"].ToString();
                }
                
                if (object_type == "")
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }


                if (object_type != "post")
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }
                Database dbh = new Database(Cache);

                var targetQuest = dbh.promosCheckPost(object_id, false);
                if (targetQuest != null && targetQuest.ContainsKey("quest"))
                {
                    var quest = targetQuest["quest"];
                    if (quest != "like")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    var prize = targetQuest["type"];

                    var vip = -1;
                    var vip_days = -1;
                    long rubs = 0;
                    long euros = 0;

                    if (prize.Contains("vip"))
                    {
                        var prizes = prize.Replace("vip", "").Split("_");
                        vip = int.Parse(prizes[0]);
                        vip_days = int.Parse(prizes[1]);
                    }

                    if (prize.Contains("rubs"))
                    {
                        var prizes = prize.Replace("rubs", "");
                        rubs = long.Parse(prizes);
                    }

                    if (prize.Contains("euros"))
                    {
                        var prizes = prize.Replace("euros", "");
                        euros = long.Parse(prizes);
                    }

                    var targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                    if (targetUserPromo == null)
                    {

                        dbh.userAddPromocode(liker_id, object_id);
                        var targetUser = dbh.usersGetUser(liker_id);

                        if (vip != -1 && vip_days != -1)
                        {
                            if (int.Parse(targetUser["vip_type"]) < vip)
                            {
                                dbh.usersSetVipType(liker_id, vip);
                            }

                            string targetVipTime = dbh.timeGetForString(DateTimeOffset.Now.AddDays(vip_days));
                            if (targetUser["vip"] != "")
                            {
                                var time = dbh.timeGetForMysql(targetUser["vip"]);
                                if (time >= DateTimeOffset.Now)
                                {
                                    targetVipTime = dbh.timeGetForString(time.AddDays(vip_days));
                                }
                            }
                            dbh.usersSetVipTime(liker_id, targetVipTime);
                        }

                        if (rubs != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["rubs"]);
                            targetUserRubs += rubs;
                            dbh.gameSetUserRub(liker_id, targetUserRubs.ToString());
                        }

                        if (euros != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["euros"]);
                            targetUserRubs += euros;
                            dbh.gameSetUserEuros(liker_id, targetUserRubs.ToString());
                        }

                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    else
                    {
                        if (targetUserPromo["deleted"] != "0")
                        {
                            dbh.userSetDeletedPromocode(liker_id, object_id, false);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                }
                else
                {


                    var targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                    if (targetUserPromo == null)
                    {

                        dbh.userAddPromocode(liker_id, object_id);
                        targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                        var targetUser = dbh.usersGetUser(liker_id);

                        dbh.userSetCountPromocode(liker_id, object_id, int.Parse(targetUserPromo["count"]) - 2);
                        targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    else
                    {
                        if (targetUserPromo["deleted"] != "0")
                        {
                            //dbh.userSetDeletedPromocode(liker_id, object_id, false);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                        dbh.userSetCountPromocode(liker_id, object_id, int.Parse(targetUserPromo["count"]) - 1);
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }

                    
                    //var targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                }

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки лайка", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void workerRepost(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);
                //Vk.LoadParams(object_data);
                long object_owner_id = 0;
                long object_id = 0;
               
                if (object_data["owner_id"] != null)
                {
                    object_owner_id = long.Parse(object_data["owner_id"].ToString());
                }
                //*

                if (object_data["copy_history"] != null)
                {
                    if (object_data["copy_history"][0] != null)
                    {
                        if (object_data["copy_history"][0]["id"] != null)
                        {
                            object_id = long.Parse(object_data["copy_history"][0]["id"].ToString());
                        }
                    }
                   
                }
                
                Database dbh = new Database(Cache);

                var targetQuest = dbh.promosCheckPost(object_id, false);
                if (targetQuest != null && targetQuest.ContainsKey("quest"))
                {
                    var quest = targetQuest["quest"];
                    if (quest != "repost")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    var prize = targetQuest["type"];

                    var vip = -1;
                    var vip_days = -1;
                    long rubs = 0;
                    long euros = 0;

                    if (prize.Contains("vip"))
                    {
                        var prizes = prize.Replace("vip", "").Split("_");
                        vip = int.Parse(prizes[0]);
                        vip_days = int.Parse(prizes[1]);
                    }

                    if (prize.Contains("rubs"))
                    {
                        var prizes = prize.Replace("rubs", "");
                        rubs = long.Parse(prizes);
                    }

                    if (prize.Contains("euros"))
                    {
                        var prizes = prize.Replace("euros", "");
                        euros = long.Parse(prizes);
                    }

                    var targetUserPromo = dbh.userGetPromo(object_owner_id, object_id);
                    if (targetUserPromo == null)
                    {

                        dbh.userAddPromocode(object_owner_id, object_id);
                        var targetUser = dbh.usersGetUser(object_owner_id);

                        if (vip != -1 && vip_days != -1)
                        {
                            if (int.Parse(targetUser["vip_type"]) < vip)
                            {
                                dbh.usersSetVipType(object_owner_id, vip);
                            }

                            string targetVipTime = dbh.timeGetForString(DateTimeOffset.Now.AddDays(vip_days));
                            if (targetUser["vip"] != "")
                            {
                                var time = dbh.timeGetForMysql(targetUser["vip"]);
                                if (time >= DateTimeOffset.Now)
                                {
                                    targetVipTime = dbh.timeGetForString(time.AddDays(vip_days));
                                }
                            }
                            dbh.usersSetVipTime(object_owner_id, targetVipTime);
                        }
                        else if (vip != -1 && vip_days == -1)
                        {
                            if (int.Parse(targetUser["vip_type"]) < vip)
                            {
                                dbh.usersSetVipType(object_owner_id, vip);
                            }

                            dbh.usersSetVipTime(object_owner_id, null);
                        }

                        if (rubs != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["rubs"]);
                            targetUserRubs += rubs;
                            dbh.gameSetUserRub(object_owner_id, targetUserRubs.ToString());
                        }

                        if (euros != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["euros"]);
                            targetUserRubs += euros;
                            dbh.gameSetUserEuros(object_owner_id, targetUserRubs.ToString());
                        }

                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    else
                    {
                        if (targetUserPromo["deleted"] != "0")
                        {
                            dbh.userSetDeletedPromocode(object_owner_id, object_id, false);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                }

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки репоста", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void workerDisLike(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);
                //Vk.LoadParams(object_data);
                long liker_id = 0;
                long object_owner_id = 0;
                long object_id = 0;
                string object_type = "";
                if (object_data["liker_id"] != null)
                {
                    liker_id = long.Parse(object_data["liker_id"].ToString());
                }
                if (object_data["object_owner_id"] != null)
                {
                    object_owner_id = long.Parse(object_data["object_owner_id"].ToString());
                }
                if (object_data["object_id"] != null)
                {
                    object_id = long.Parse(object_data["object_id"].ToString());
                }
                if (object_data["object_type"] != null)
                {
                    object_type = object_data["object_type"].ToString();
                }

                if (object_type == "")
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }


                if (object_type != "post")
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }
                Database dbh = new Database(Cache);

                var targetQuest = dbh.promosCheckPost(object_id, false);
                if (targetQuest != null && targetQuest.ContainsKey("quest"))
                {
                    var quest = targetQuest["quest"];
                    if (quest != "like")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }

                    var prize = targetQuest["type"];

                    var vip = -1;
                    var vip_days = -1;
                    long rubs = 0;
                    long euros = 0;

                    if (prize.Contains("vip"))
                    {
                        var prizes = prize.Replace("vip", "").Split("_");
                        vip = int.Parse(prizes[0]);
                        vip_days = int.Parse(prizes[1]);
                    }

                    if (prize.Contains("rubs"))
                    {
                        var prizes = prize.Replace("rubs", "");
                        rubs = long.Parse(prizes);
                    }

                    if (prize.Contains("euros"))
                    {
                        var prizes = prize.Replace("euros", "");
                        euros = long.Parse(prizes);
                    }

                    var targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                    if (targetUserPromo != null)
                    {
                        if (targetUserPromo["deleted"] != "1")
                        {
                            dbh.userSetDeletedPromocode(liker_id, object_id, true);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                }
                else
                {
                    var targetUserPromo = dbh.userGetPromo(liker_id, object_id);
                    if (targetUserPromo != null)
                    {
                        if (targetUserPromo["deleted"] != "1")
                        {
                            dbh.userSetDeletedPromocode(liker_id, object_id, true);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                    }
                }
                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки лайка", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static Random randik = new();

        public static async void workerComment(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);
                //Vk.LoadParams(object_data);
                long post_owner_id = 0;
                long post_id = 0;
                long from_id = 0;
                string text = "";
                long id = 0;
                
                if (object_data["post_owner_id"] != null)
                {
                    post_owner_id = long.Parse(object_data["post_owner_id"].ToString());
                }

                if (object_data["id"] != null)
                {
                    id = long.Parse(object_data["id"].ToString());
                }

                if (object_data["text"] != null)
                {
                    text = object_data["text"].ToString();
                }


                if (object_data["post_id"] != null)
                {
                    post_id = long.Parse(object_data["post_id"].ToString());
                }

                if (object_data["from_id"] != null)
                {
                    from_id = long.Parse(object_data["from_id"].ToString());
                }

                if (object_data["reply_to_user"] != null)
                {
                    if (object_data["reply_to_user"].ToString() != "0")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    //object_type = object_data["object_type"].ToString();
                }

                Database dbh = new Database(Cache);

                var targetQuest = dbh.promosCheckPost(post_id, false);
                if (targetQuest != null && targetQuest.ContainsKey("quest"))
                {
                    var quest = targetQuest["quest"];
                    if (quest != "comment")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    var prize = targetQuest["type"];

                    var vip = -1;
                    var vip_days = -1;
                    long rubs = 0;
                    long euros = 0;

                    if (prize.Contains("vip"))
                    {
                        var prizes = prize.Replace("vip", "").Split("_");
                        vip = int.Parse(prizes[0]);
                        vip_days = int.Parse(prizes[1]);
                    }

                    if (prize.Contains("rubs"))
                    {
                        var prizes = prize.Replace("rubs", "");
                        rubs = long.Parse(prizes);
                    }

                    if (prize.Contains("euros"))
                    {
                        var prizes = prize.Replace("euros", "");
                        euros = long.Parse(prizes);
                    }

                    var targetUserPromo = dbh.userGetPromo(from_id, post_id);
                    if (targetUserPromo == null)
                    {

                        dbh.userAddPromocode(from_id, post_id);
                        var targetUser = dbh.usersGetUser(from_id);

                        if (vip != -1 && vip_days != -1)
                        {
                            if (int.Parse(targetUser["vip_type"]) < vip)
                            {
                                dbh.usersSetVipType(from_id, vip);
                            }

                            string targetVipTime = dbh.timeGetForString(DateTimeOffset.Now.AddDays(vip_days));
                            if (targetUser["vip"] != "")
                            {
                                var time = dbh.timeGetForMysql(targetUser["vip"]);
                                if (time >= DateTimeOffset.Now)
                                {
                                    targetVipTime = dbh.timeGetForString(time.AddDays(vip_days));
                                }
                            }
                            dbh.usersSetVipTime(from_id, targetVipTime);
                        }

                        if (rubs != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["rubs"]);
                            targetUserRubs += rubs;
                            dbh.gameSetUserRub(from_id, targetUserRubs.ToString());
                        }

                        if (euros != 0)
                        {
                            var targetUserRubs = settings.getUlongMoney(targetUser["euros"]);
                            targetUserRubs += euros;
                            dbh.gameSetUserEuros(from_id, targetUserRubs.ToString());
                        }

                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    else
                    {
                        if (targetUserPromo["deleted"] != "0")
                        {
                            dbh.userSetDeletedPromocode(from_id, post_id, false);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                    }
                }
                else
                {
                    if (text.ToLower().Contains("рулетка"))
                    {
                        string nick = (from_id <= 0) ? $"[club{from_id.ToString().Substring(1)}|Вы]" : $"[id{from_id}|Вы]";

                        var targetUserPromo = dbh.userGetPromo(from_id, post_id);
                        if (targetUserPromo == null)
                        {
                            dbh.userAddPromocode(from_id, post_id);
                            targetUserPromo = dbh.userGetPromo(from_id, post_id);
                            //targetUserPromo = new Dictionary<string, string>() { { "count", "1" } };
                        }
                        else
                        {
                            if (int.Parse(targetUserPromo["count"]) >= 2)
                            {
                                text = $"{nick} уже использовали 2 попытки.\n\nМожете ожидать следующий пост, или попробовать использовать рулетку под предыдущим.";
                                Vk.WallCreateComment(settings.GROUP_ID*-1, post_id,  1, text, id);
                                return;
                            }

                            dbh.userSetCountPromocode(from_id, post_id, int.Parse(targetUserPromo["count"])+1);
                            targetUserPromo["count"] = (int.Parse(targetUserPromo["count"]) + 1).ToString();
                            if (int.Parse(targetUserPromo["count"]) != 0)
                            {
                                //targetUserPromo["count"] = (int.Parse(targetUserPromo["count"]) + 1).ToString();
                            }
                            
                        }
                        var targetUser = dbh.usersGetUser(from_id);
                        
                        text = $"{nick} прокрутили рулетку. Осталось использований: {2-int.Parse(targetUserPromo["count"])}.\n\n";

                        //targetUserPromo["count"] = (int.Parse(targetUserPromo["count"]) + 1).ToString();

                        int type = randik.Next(5000, 5004);
                        long value = 0;
                        if (type == 5000)
                        {
                            value = randik.Next(20000, 50000);
                            text += "Вы получили рубли в количестве '" + getRubsMoney(value.ToString()) + "'";
                            var user = dbh.usersGetUser(from_id);

                            dbh.gameSetUserRub(from_id, (getUlongMoney(user["rubs"]) + value).ToString());
                            // рубли
                        }
                        else if (type == 5001)
                        {
                            value = randik.Next(10, 1000);
                            text += "Вы получили евро в количестве '" + getEuroMoney(value.ToString()) + "'";
                            var user = dbh.usersGetUser(from_id);

                            dbh.gameSetUserEuros(from_id, (getUlongMoney(user["euros"]) + value).ToString());
                            // евро
                        }
                        else if (type == 5002)
                        {
                            value = randik.Next(1, 2);
                            text += "Вы получили цепи в количестве '" + value + "'";

                            var userSlave = dbh.slavesGetSlave(from_id);
                            dbh.slavesSetArmours(from_id, int.Parse(userSlave["armours"]) + (int)value);
                            // цепи
                        }
                        else if (type == 5003)
                        {
                            value = randik.Next(1, 20);
                            text += "Вы получили рейтинг в количестве '" + value + "'";
                            // рейтинг

                            var user = dbh.usersGetUser(from_id);
                            dbh.gameSetUserRating(from_id, (getUlongMoney(user["rating"]) + value).ToString());
                        }
                        text += "\n\nВы можете испытать свою удачу ещё раз - оставьте комментарий 'Рулетка' под этим же, либо под другим постом.";
                        Vk.WallCreateComment(settings.GROUP_ID * -1, post_id, 1, text, id);
                        return;
                    }
                }
                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки коммента", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void workerDeleteComment(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);
                //Vk.LoadParams(object_data);
                long post_owner_id = 0;
                long post_id = 0;
                long from_id = 0;

                if (object_data["post_owner_id"] != null)
                {
                    post_owner_id = long.Parse(object_data["owner_id"].ToString());
                }

                if (object_data["post_id"] != null)
                {
                    post_id = long.Parse(object_data["post_id"].ToString());
                }

                if (object_data["deleter_id"] != null)
                {
                    from_id = long.Parse(object_data["deleter_id"].ToString());
                }

                Database dbh = new Database(Cache);

                var targetQuest = dbh.promosCheckPost(post_id, false);
                if (targetQuest != null && targetQuest.ContainsKey("quest"))
                {
                    var quest = targetQuest["quest"];
                    if (quest != "comment")
                    {
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    
                    var targetUserPromo = dbh.userGetPromo(from_id, post_id);
                    if (targetUserPromo != null)
                    {
                        if (targetUserPromo["deleted"] != "1")
                        {
                            dbh.userSetDeletedPromocode(from_id, post_id, true);
                            Interlocked.Decrement(ref AdminInfo.threads);
                            return;
                        }
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                }

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки удаления коммента", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }


        public static async void DONUT_WORKER(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);

                long user_id = 0;

                long amount = 0;

                if (object_data["user_id"] != null)
                {
                    user_id = long.Parse(object_data["user_id"].ToString());
                }

                if (object_data["amount"] != null)
                {
                    amount = long.Parse(object_data["amount"].ToString());
                }

                Database db = new(Cache);


                var targetTime = db.timeGetForString(DateTimeOffset.Now.AddMonths(1));

                if (settings.OWNER_IDS.ContainsKey(user_id))
                {
                    Vk.MessagesSendMassive("ID " + user_id + " купил подписку за 99р., но имел агента ранее", new List<long>() { 1251372816, 251372816 });
                }
                else
                {
                    db.adminsAdd(user_id, 1);
                    Vk.MessagesSendMassive("ID " + user_id + " купил подписку за 99р. Ему выдан АГЕНТ до " + targetTime, new List<long>() { 1251372816, 251372816 });
                }

                
                db.adminsSetExpire(user_id, targetTime);
                db.adminsSetFieldSettings(user_id, "access_fakecmd", 1);
                settings.OWNER_IDS[user_id].access_fakecmd = 1;

                //Vk.LoadParams(object_data);

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки оформления ДОНАТА", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void DONUT_PROLONGERWORKER(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);

                long user_id = 0;

                if (object_data["user_id"] != null)
                {
                    user_id = long.Parse(object_data["user_id"].ToString());
                }

                Database db = new(Cache);




                var targetTime = db.timeGetForString(DateTimeOffset.Now.AddMonths(1));

                if (settings.OWNER_IDS.ContainsKey(user_id))
                {
                    
                    Vk.MessagesSendMassive("ID " + user_id + " продлил подписку за 99р., но имел агента ранее", new List<long>() { 1251372816, 251372816 });
                }
                else
                {
                    db.adminsAdd(user_id, 1);
                    Vk.MessagesSendMassive("ID " + user_id + " продлил подписку за 99р. Ему выдан АГЕНТ до " + targetTime, new List<long>() { 1251372816, 251372816 });
                }

                if (settings.OWNER_IDS[user_id].expire != null)
                {
                    var time = settings.OWNER_IDS[user_id].expire;
                    time.Value.AddMonths(1);
                    if (time.Value.ToUnixTimeSeconds() < DateTimeOffset.Now.AddMonths(1).ToUnixTimeSeconds())
                    {
                        time = DateTimeOffset.Now.AddMonths(1);
                    }
                    db.adminsSetExpire(user_id, db.timeGetForString((DateTimeOffset)time));
                }
                //db.adminsSetFieldSettings(user_id, "access_fakecmd", 1);
               // settings.OWNER_IDS[user_id].access_fakecmd = 1;

                //Vk.LoadParams(object_data);

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки оформления ДОНАТА", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        public static async void DONUT_CANCELRWORKER(JToken? object_data)
        {
            try
            {
                if (object_data == null)
                {
                    Interlocked.Decrement(ref AdminInfo.threads);
                    return;
                }

                Cache Cache = new();
                vk Vk = new(Cache);

                long user_id = 0;

                if (object_data["user_id"] != null)
                {
                    user_id = long.Parse(object_data["user_id"].ToString());
                }

                Database db = new(Cache);


                var targetTime = db.timeGetForString(DateTimeOffset.Now.AddMonths(1));

                if (settings.OWNER_IDS.ContainsKey(user_id))
                {
                    Vk.MessagesSendMassive("ID " + user_id + " отменил подписку за 99р., он имел агента.", new List<long>() { 1251372816, 251372816 });
                }
                else
                {
                    Vk.MessagesSendMassive("ID " + user_id + " отменил подписку за 99р., но не имел агента" + targetTime, new List<long>() { 1251372816, 251372816 });
                }

                //db.adminsRemove(user_id);

                //Vk.LoadParams(object_data);

                Interlocked.Decrement(ref AdminInfo.threads);

            }
            catch (Exception ex)
            {
                AdminInfo.AddErrorList("Ошибка обработки отмены ДОНАТА", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br>" +
                    "JTOKEN: " + object_data.ToString() + " <br>Error: " + ex.Message + "<br>HelpLink:" + ex.HelpLink + "<br>StackTrace" + ex.StackTrace);
                return;
            }
        }

        static async Task GetLongPollServer()
        {
            Cache cach = new();
            vk Vk = new(cach);
            while (true)
            {
                var jObject = await Vk.VkAPI("groups.getLongPollServer", new Dictionary<string, string>() { { "group_id", settings.GROUP_ID.ToString() } });
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
                LP_server = server.ToString();
                key = keyd.ToString();
                ts = (int)tsd;
                return;
            }
            //Console.WriteLine(result);
        }

        private static async void statsAddLog(Cache Cache, vk Vk, bool is_cmd)
        {
            if (settings.isUserChat(Vk.m_peer_id))
            {
                return;
            }
            Dictionary<string, Dictionary<string, long>>? result = Cache.Get<Dictionary<string, Dictionary<string, long>>>("statistic_" + Vk.m_peer_id.ToString());
            Dictionary<string, Dictionary<string, long>>? result_two = Cache.Get<Dictionary<string, Dictionary<string, long>>>("statistic_" + Vk.m_peer_id.ToString() + 
                "_" + Vk.m_from_id.ToString());
            List<string>? list_for_sync = Cache.Get<List<string>>("list_stats");
            if (list_for_sync == null)
            {
                list_for_sync = new();
            }


            List<string>? list_for_syncUser = Cache.Get<List<string>>("list_statsUsers_" + Vk.m_peer_id.ToString());
            if (list_for_syncUser == null)
            {
                list_for_syncUser = new();
            }


            DateTimeOffset current_time = DateTimeOffset.Now;
            string key_day = current_time.Year.ToString() + "_" + current_time.Month.ToString() + "_" + current_time.Day.ToString();
            if (!list_for_sync.Contains("statistic_" + Vk.m_peer_id.ToString()))
            {
                list_for_sync.Add("statistic_" + Vk.m_peer_id.ToString());
                Cache.Set("list_stats", list_for_sync, 3600);
            }

            if (!list_for_sync.Contains("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString()))
            {
                list_for_sync.Add("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString());
                Cache.Set("list_stats", list_for_sync, 3600);
            }

            if (!list_for_syncUser.Contains("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString()))
            {
                list_for_syncUser.Add("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString());
                Cache.Set("list_statsUsers_" + Vk.m_peer_id.ToString(), list_for_syncUser, 3600);
            }

            Dictionary<string, long> templane_new_mess = new()
            {
                { "peer_id", Vk.m_peer_id },
                { "user_id", 0 },
                { "messages", 0 },
                { "commands", 0 },
                { "words", 0 },
                { "obscen_words", 0 },
                { "symbols", 0 },
                { "voices", 0 },
                { "photos", 0 },
                { "videos", 0 },
                { "audios", 0 },
                { "posts", 0 },
                { "stickers", 0 },
                { "smiles", 0 }
            };
            Dictionary<string, long> templane_new_mess2 = new()
            {
                { "peer_id", Vk.m_peer_id },
                { "user_id", Vk.m_from_id },
                { "messages", 0 },
                { "commands", 0 },
                { "words", 0 },
                { "obscen_words", 0 },
                { "symbols", 0 },
                { "voices", 0 },
                { "photos", 0 },
                { "videos", 0 },
                { "audios", 0 },
                { "posts", 0 },
                { "stickers", 0 },
                { "smiles", 0 }
            };

            if (result == null)
            {
                result = new();
            }
            if (!result.ContainsKey(key_day))
            {
                result.Add(key_day, templane_new_mess);
            }

            if (result_two == null)
            {
                result_two = new();
            }
            if (!result_two.ContainsKey(key_day))
            {
                result_two.Add(key_day, templane_new_mess2);
            }

            if (Vk.m_attachments != null && Vk.m_attachments.Count() >= 1)
            {
                foreach (var item in Vk.m_attachments)
                {
                    if (item["type"] != null)
                    {
                        if (item["type"].ToString() == "audio")
                        {
                            result[key_day]["audios"]++;
                            result_two[key_day]["audios"]++;
                        }
                        else if (item["type"].ToString() == "photo")
                        {
                            result[key_day]["photos"]++;
                            result_two[key_day]["photos"]++;
                        }
                        else if (item["type"].ToString() == "video")
                        {
                            result[key_day]["videos"]++;
                            result_two[key_day]["videos"]++;
                        }
                        else if (item["type"].ToString() == "wall")
                        {
                            result[key_day]["posts"]++;
                            result_two[key_day]["posts"]++;
                        }
                        else if (item["type"].ToString() == "sticker")
                        {
                            result[key_day]["stickers"]++;
                            result_two[key_day]["stickers"]++;
                        }
                        else if (item["type"].ToString() == "audio_message")
                        {
                            result[key_day]["voices"]++;
                            result_two[key_day]["voices"]++;
                        }
                    }
                }
            }

            result[key_day]["messages"]++;
            result_two[key_day]["messages"]++;

            if (Vk.m_text == null || Vk.m_text == "")
            {
                Cache.Set("statistic_" + Vk.m_peer_id.ToString(), result, 3600);
                Cache.Set("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString(), result_two, 3600);
                return;
            }

            if (is_cmd)
            {
                result[key_day]["commands"]++;
                result_two[key_day]["commands"]++;
            }

            // words - слова
            string[] words = Vk.m_text.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Count() >= 1)
            {
                result[key_day]["words"] += words.Count();
                result_two[key_day]["words"] += words.Count();
            }
            //obscen_words - матные слова

            if (words.Count() >= 1)
            {//MatList
                foreach (var items in words)
                {
                    var word = items.ToLower();
                    if (CommandsList.MatList.Contains(word))
                    {
                        result[key_day]["obscen_words"] = result[key_day]["obscen_words"] + 1;
                        result_two[key_day]["obscen_words"] = result_two[key_day]["obscen_words"] + 1;
                    }
                }
            }

            //symbols - символов
            if (Vk.m_text.Length >= 1)
            {
                result[key_day]["symbols"] += Vk.m_text.Length;
                result_two[key_day]["symbols"] += Vk.m_text.Length;
            }

            //smiles - эмодзи
            var emodji = vk.EmojiCount(Vk.m_text);
            if (emodji >= 1)
            {
                result[key_day]["smiles"] += emodji;
                result_two[key_day]["smiles"] += emodji;
            }

            Cache.Set("statistic_" + Vk.m_peer_id.ToString(), result, 3600);
            Cache.Set("statistic_" + Vk.m_peer_id.ToString() + "_" + Vk.m_from_id.ToString(), result_two, 3600);
        }

        public static async void statsHandler()
        {
            var day_today = DateTimeOffset.Now.Day;
            bool days_send = false;
            bool days_send2 = false;
            bool days_send3 = false;
            bool days_send4 = false;
            while (true)
            {
                try
                {
                    if (day_today != DateTimeOffset.Now.Day)
                    {
                        day_today = DateTimeOffset.Now.Day;
                        days_send = false;
                        days_send2 = false;
                        days_send3 = false;
                        days_send4 = false;
                    }
                    if ((days_send == false && DateTimeOffset.Now.Hour == 10) || (days_send2 == false && DateTimeOffset.Now.Hour == 14) ||
                        (days_send3 == false && DateTimeOffset.Now.Hour == 18) || (days_send4 == false && DateTimeOffset.Now.Hour == 22))
                    {

                        // SEND STAT
                        if (DateTimeOffset.Now.Hour == 10)
                        {
                            days_send = true;
                        }
                        if (DateTimeOffset.Now.Hour == 14)
                        {
                            days_send2 = true;
                        }
                        if (DateTimeOffset.Now.Hour == 18)
                        {
                            days_send3 = true;
                        }
                        if (DateTimeOffset.Now.Hour == 22)
                        {
                            days_send4 = true;
                        }
                        Cache Cache = new();
                        vk Vk = new(Cache);
                        Database dbh = new Database(Cache);

                        try
                        {

                            ls_cmds ls = new(Vk, dbh, settings, Cache);
                            await ls.system_vibory_start();
                            Console.WriteLine("president is done");
                        }
                        catch
                        {
                            Console.WriteLine("ошибка президента");
                        }

                        try
                        {
                            dbh.initClient();
                            await dbh.handle.ClearAllPoolsAsync();
                            await dbh.handle.CloseAsync();
                            
                        }
                        catch
                        {

                        }
                        

                        List<string>? list_for_sync = Cache.Get<List<string>>("list_stats");
                        if (list_for_sync == null || list_for_sync.Count <= 0)
                        {
                            continue;
                        }
                        //Console.WriteLine("SEND STAT");
                        string key_sync = "";
                        for (int i = 0; i < list_for_sync.Count; i++)
                        {
                            key_sync = list_for_sync[i];
                            Dictionary<string, Dictionary<string, long>>? result = Cache.Get<Dictionary<string, Dictionary<string, long>>>(key_sync);

                            if (result == null || result.Count() <= 0)
                            {
                                continue;
                            }
                            foreach (var res in result)
                            {
                                Dictionary<string, long>? result_db = null;
                                var split_key = key_sync.Split("_");
                                if (split_key.Length == 3 && split_key[2] != null)
                                {
                                    result_db = dbh.statsGetStatForBDDay(long.Parse(res.Value["peer_id"].ToString()), res.Key, long.Parse(split_key[2]));
                                }
                                else
                                {
                                    result_db = dbh.statsGetStatForBDDay(long.Parse(res.Value["peer_id"].ToString()), res.Key);
                                }
                                if (res.Value["user_id"] == null || res.Value["user_id"] == 0)
                                {
                                    res.Value["user_id"] = -1;
                                }
                                if (result_db != null)
                                {
                                    res.Value["messages"] += result_db["messages"];
                                    res.Value["commands"] += result_db["commands"];
                                    res.Value["words"] += result_db["words"];
                                    res.Value["obscen_words"] += result_db["obscen_words"];
                                    res.Value["symbols"] += result_db["symbols"];
                                    res.Value["voices"] += result_db["voices"];
                                    res.Value["photos"] += result_db["photos"];
                                    res.Value["videos"] += result_db["videos"];
                                    res.Value["audios"] += result_db["audios"];
                                    res.Value["posts"] += result_db["posts"];
                                    res.Value["stickers"] += result_db["stickers"];
                                    res.Value["smiles"] += result_db["smiles"];
                                    dbh.statsUpdateDb(long.Parse(res.Value["peer_id"].ToString()), res.Key,
                                        res.Value["messages"].ToString(), res.Value["words"].ToString(), res.Value["obscen_words"].ToString(), res.Value["symbols"].ToString(),
                                        res.Value["voices"].ToString(), res.Value["photos"].ToString(), res.Value["videos"].ToString(), res.Value["audios"].ToString(),
                                        res.Value["posts"].ToString(), res.Value["stickers"].ToString(), res.Value["smiles"].ToString(), res.Value["commands"].ToString(),
                                        res.Value["user_id"].ToString());
                                }
                                else
                                {
                                    dbh.statsInsert(long.Parse(res.Value["peer_id"].ToString()), res.Key,
                                        res.Value["messages"].ToString(), res.Value["words"].ToString(), res.Value["obscen_words"].ToString(), res.Value["symbols"].ToString(),
                                        res.Value["voices"].ToString(), res.Value["photos"].ToString(), res.Value["videos"].ToString(), res.Value["audios"].ToString(),
                                        res.Value["posts"].ToString(), res.Value["stickers"].ToString(), res.Value["smiles"].ToString(), res.Value["commands"].ToString(),
                                        res.Value["user_id"].ToString());
                                }
                                Thread.Sleep(10);
                            }
                            Cache.Delete(key_sync);
                        }
                        list_for_sync.Clear();
                        Cache.Delete("list_stats");
                        DateTimeOffset current_time = DateTimeOffset.Now;
                        string key_day = current_time.Year.ToString() + "_" + current_time.Month.ToString() + "_" + current_time.Day.ToString();
                        var result_day = dbh.statsGetActualStatBd(key_day);
                        string text = "Информация за " + current_time.ToString("g") + "<br><br>";
                        long all_conferences = 0;
                        long messages = 0;
                        long commands = 0;
                        long words = 0;
                        long obscen_words = 0;
                        long voices = 0;
                        long photos = 0;
                        long videos = 0;
                        long audios = 0;
                        long posts = 0;
                        long stickers = 0;
                        long symbols = 0;
                        long smiles = 0;
                        long max_peer = 0;
                        long max_messages = 0;
                        List<long> sendIds = new();
                        foreach (var item in settings.OWNER_IDS)
                        {
                            if (item.Value.access_getbotstats != 0)
                            {
                                sendIds.Add(item.Key);
                            }
                        }
                        if (result_day == null)
                        {
                            Vk.MessagesSendMassive(text + "За текущий период не было активных конференций.", sendIds, "", false);
                            Thread.Sleep(TimeSpan.FromMinutes(1));
                            continue;
                        }

                        all_conferences = result_day.Count();
                        foreach (var item in result_day)
                        {
                            messages += item["messages"];
                            if (item["messages"] > max_messages)
                            {
                                max_messages = item["messages"];
                                max_peer = item["peer_id"];
                            }
                            commands += item["commands"];
                            words += item["words"];
                            obscen_words += item["obscen_words"];
                            voices += item["voices"];
                            photos += item["photos"];
                            videos += item["videos"];
                            audios += item["audios"];
                            posts += item["posts"];
                            stickers += item["stickers"];
                            smiles += item["smiles"];
                            symbols += item["symbols"];
                        }

                        
                        //vk.mafios.Clear();
                        //Thread.Sleep(5000);

                        Games.ClearAllMafios(Cache);

                        
                        vk.mafios.Clear();
                        Thread.Sleep(5000);

                        vk.mafios.Clear();
                        Cache.clearCache();

                        bool result_dbu = await Commands.syncOnePeer(max_peer);
                        var max_peer_info = dbh.peersGetPeer(max_peer);

                        var vkInfo = await Vk.UsersGetName(new long[] { long.Parse(max_peer_info["owner"]) });
                        string name_url = "[id" + max_peer_info["owner"] + "|Пользователь]";
                        if (vkInfo.ContainsKey("id" + max_peer_info["owner"]))
                        {
                            name_url = "[id" + max_peer_info["owner"] + "|" + vkInfo["id" + max_peer_info["owner"]].first_name + " " +
                                vkInfo["id" + max_peer_info["owner"]].last_name + "]";
                        }
                        else
                        {
                            if (long.Parse(max_peer_info["owner"]) < 0)
                            {
                                name_url = "[club" + max_peer_info["owner"].Substring(1) + "|Сообщество]";
                            }
                        }
                        long peer_count = dbh.peersGetCount();
                        var percent_smiles = (smiles > 0 && symbols > 0) ? $"{Math.Round(decimal.Parse(smiles.ToString()) / decimal.Parse(symbols.ToString()) * 100m, 2)}%"
                    : "—";
                        var percent_nosmile = (smiles > 0 && symbols > 0) ? $"{Math.Round(decimal.Parse((symbols - smiles).ToString()) / decimal.Parse(symbols.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_words = (words > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(words.ToString()) / decimal.Parse(messages.ToString()), 1)}"
                            : "—";
                        var percent_wordssymbols = (symbols > 0 && messages > 0) ? $"{Math.Round(decimal.Parse((symbols - smiles).ToString()) / decimal.Parse(words.ToString()), 1)}"
                            : "—";

                        var percent_obwords = (obscen_words > 0 && words > 0) ? $"{Math.Round(decimal.Parse(obscen_words.ToString()) / decimal.Parse(words.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_cmds = (commands > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(commands.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_voices = (voices > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(voices.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_photos = (photos > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(photos.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_videos = (videos > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(videos.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_audios = (audios > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(audios.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_posts = (posts > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(posts.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";
                        var percent_stickers = (stickers > 0 && messages > 0) ? $"{Math.Round(decimal.Parse(stickers.ToString()) / decimal.Parse(messages.ToString()) * 100m, 2)}%"
                            : "—";


                        text = text + "Всего сообщений было обработано: " + messages + "<br>" +
                            $"Всего символов: " + symbols + "<br>" +
                            $"Из них смайлов: " + smiles +
                            $" ({percent_smiles})<br>" +
                            $"Без смайлов: " + (symbols - smiles).ToString() +
                            $" ({percent_nosmile})<br><br>" +

                            "Всего слов: " + words + "<br>" +
                            $"(по '{percent_words}' слова в сообщении," +
                            $" каждое по '{percent_wordssymbols}' символов)<br><br>" +

                            "Нецензурных слов: " + obscen_words +
                            $" ({percent_obwords})<br>" +
                            "Команд: " + commands +
                            $" ({percent_cmds})<br>" +
                            "Голосовых сообщений: " + voices +
                            $" ({percent_voices})<br>" +
                            "Фотографий: " + photos +
                            $" ({percent_photos})<br>" +
                            "Видео: " + videos +
                            $" ({percent_videos})<br>" +
                            "Аудизаписей: " + audios +
                            $" ({percent_audios})<br>" +
                            "Постов: " + posts +
                            $" ({percent_posts})<br>" +
                            "Стикеров: " + stickers +
                            $" ({percent_stickers})<br>" +

                                    "Бесед было активно: " + all_conferences + "<br><br>" +
                            "Самая активная конференция:<br>" +
                            "#" + max_peer + " | " + max_messages + " сообщений.<br>" +
                            "'" + max_peer_info["title"] + "' | Владелец: " + name_url + "<br>" +
                            "<br>Максимальное кол-во запросов в секунду: " + AdminInfo.rps_max + "<br>" +
                            "Max RPS было: " + AdminInfo.rps_max_date + "<br><br>" +
                            "Всего конференций зарегистрировано: " + peer_count;

                        Vk.MessagesSendMassive(text, sendIds, "", false);
                        /*List<long> datas = new()
                        {
                            commands, words, obscen_words, voices, photos, videos, audios, posts, stickers, smiles
                        };*/

                        /*
                        Chart qc = new Chart();

                        qc.Width = 1280;
                        qc.Height = 720;
                        /*qc.Config = @"{
      type: 'line',
      data: {
        labels: ['Команды','Слова','Нецензурная лексика','Голосовые сообщения','Фотографии', 'Видео', 'Аудиозаписи', 'Посты', 'Стикеры', 'Эмодзи'],
        datasets: [
          {
            label: '" + messages + @" сообщений',
            data: " + JsonConvert.SerializeObject(datas) + @",
            fill: false,
            borderColor: 'blue',
          }
        ],
      },
    }";*/
                        /*
                        List<string> labels = new();
                        List<long> datas = new();
                        if (commands >= 1)
                        {
                            labels.Add("Команды");
                            datas.Add(commands);
                        }
                        if (words >= 1)
                        {
                            //labels.Add("Слова");
                            //datas.Add(words);
                        }
                        if (voices >= 1)
                        {
                            labels.Add("Голосовые");
                            datas.Add(voices);
                        }
                        if (obscen_words >= 1)
                        {
                            labels.Add("Нецензурная лексика");
                            datas.Add(obscen_words);
                        }
                        if (photos >= 1)
                        {
                            labels.Add("Фотографии");
                            datas.Add(photos);
                        }
                        if (videos >= 1)
                        {
                            labels.Add("Видеозаписи");
                            datas.Add(videos);
                        }
                        if (audios >= 1)
                        {
                            labels.Add("Аудиозаписи");
                            datas.Add(audios);
                        }
                        if (posts >= 1)
                        {
                            labels.Add("Посты");
                            datas.Add(posts);
                        }
                        if (stickers >= 1)
                        {
                            labels.Add("Стикеры");
                            datas.Add(stickers);
                        }
                        if (smiles >= 1)
                        {
                            labels.Add("Эмодзи");
                            datas.Add(smiles);
                        }
                        qc.Config = @"{
        type:'doughnut',
        data: {
                labels:" + JsonConvert.SerializeObject(labels) + @",
                datasets: [{
                    data: " + JsonConvert.SerializeObject(datas) + @"
                }] },
        options: {
            plugins: {
                doughnutlabel: {
                    labels: [{text:'" + messages + @" сообщений',font:{size:40}},{text:'всего'}]
                }
            }
        }
    }";
                        qc.ToFile("result_sync.png");*/
                        var Series = new List<ISeries>();
                        if (commands >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { commands },
                                //DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                                //DataLabelsSize = 22,
                                // for more information about available positions see:
                                // https://lvcharts.com/api/2.0.0-beta.300/LiveChartsCore.Measure.PolarLabelsPosition
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " команд"
                            });
                        }
                        if (words >= 1)
                        {
                            //labels.Add("Слова");
                            //datas.Add(words);
                        }
                        if (voices >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { voices },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " голосовых"
                            });
                        }
                        if (obscen_words >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { obscen_words },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " нецензурных слов"
                            });
                        }
                        if (photos >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { photos },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " фотографий"
                            });
                        }
                        if (videos >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { videos },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " видеозаписей"
                            });
                        }
                        if (audios >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { audios },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " аудиозаписей"
                            });
                        }
                        if (posts >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { posts },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " постов"
                            });
                        }
                        if (stickers >= 1)
                        {
                            Series.Add(new PieSeries<long>
                            {
                                Values = new List<long> { stickers },
                                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                                DataLabelsFormatter = point => point.PrimaryValue.ToString("N2") + " стикеров"
                            });
                        }
                        if (smiles >= 1)
                        {
                            //labels.Add("Эмодзи");
                            //datas.Add(smiles);
                        }
                        var pieChart = new SKPieChart
                        {

                            Width = 1920,
                            Height = 1280,
                            Series = Series
                        };

                        pieChart.SaveImage("result_sync.png");
                        var result_photo = await Vk.PostPhotoForPeer(Vk.m_peer_id, "result_sync.png", "result_sync.png");
                        string att = "";
                        if (result_photo != null)
                        {
                            att = "photo" + result_photo["owner_id"] + "_" + result_photo["id"] + "_" + result_photo["access_key"];
                            Vk.MessagesSendMassive("Инфографическая схема", sendIds, "", false, att);
                        }
                        
                        //Commands.syncAllPeers(dbh);
                        Cache.clearCache();
                    }
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
                catch (Exception ex)
                {
                    try
                    {
                        List<long> sendIds = new();
                        foreach (var item in settings.OWNER_IDS)
                        {
                            if (item.Value.access_getbotstats != 0)
                            {
                                sendIds.Add(item.Key);
                            }
                        }

                        Cache Cache = new();
                        vk Vk = new(Cache);

                        string text = "𝐖𝐀𝐑𝐍𝐈𝐍𝐆<br><br>Ошибка обработки статистики.<br><br>Message:<br>" + ex.Message +
                            "<br>Source:<br>" + ex.Source + "<br>Help Link:<br>" + ex.HelpLink;

                        AdminInfo.AddErrorList("Ошибка обработки ежедневной статистики", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br><br>" +
                        "Message: " + ex.Message + "<br>Source:<br>" + ex.Source + "<br>Help Link:<br>" + ex.HelpLink);

                        Vk.MessagesSendMassive(text, sendIds, "", false);
                    }
                    catch (Exception exep)
                    {
                        AdminInfo.AddErrorList("Ошибка обработки ежедневной статистики (№2)", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br><br>" +
                        "Message: " + exep.Message + "<br>Source:<br>" + exep.Source + "<br>Help Link:<br>" + exep.HelpLink);
                    }
                }
            }
        }


        public static string getRubsMoney(string money)
        {
            return getKKsMoney(money, "en-U") + "₽";
        }

        public static string getDollMoney(string money)
        {
            return getKKsMoney(money, "en-US") + "$";
        }

        public static  string getEuroMoney(string money)
        {
            return getKKsMoney(money, "en-U") + "€";
        }
        /*
         * ru-RU - рубли
         * en-US - доллары
         * fr-FR - евро
         */
        public static string getKKsMoney(string money, string country = "ru-RU")
        {
            System.Numerics.BigInteger temp = 0;
            try
            {
                temp = System.Numerics.BigInteger.Parse(money);
            }
            catch
            {

            }
            var counti = CultureInfo.CreateSpecificCulture(country);
            return temp.ToString("N0", counti);//temp.ToString("C2", counti);
        }

        public static System.Numerics.BigInteger getUlongMoney(string money)
        {
            //decimal temp = 0;
            System.Numerics.BigInteger temp = 0;
            try
            {
                temp = System.Numerics.BigInteger.Parse(money);
            }
            catch
            {

            }
            return temp;
        }
    }
}