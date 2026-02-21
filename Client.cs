using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using GrandManager.engine;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Globalization;
using System.Data;
//using GrandManager.engine.SiteEngine;
using System.Web;
using Newtonsoft.Json;
using Ubiety.Dns.Core;
using GrandManager.engine.API;

namespace GrandManager
{
    struct HTTPHeaders
    {
        public string Method;
        public string RealPath;
        public string File;
        public string Ip;
        public Dictionary<string, string> Cookies;

        public static HTTPHeaders Parse(string headers)
        {
            try
            {
                HTTPHeaders result = new HTTPHeaders();
                result.Method = Regex.Match(headers, @"\A\w[a-zA-Z]+", RegexOptions.Multiline).Value;

                string httpMethod = headers.Substring(0, headers.IndexOf(" "));

                int start = headers.IndexOf(httpMethod) + httpMethod.Length + 1;
                int length = headers.LastIndexOf("HTTP") - start - 1;
                result.File = headers.Substring(start, length);

                Match ipMatch = Regex.Match(headers, @"CF-Connecting-IP:\s*([^\r\n]+)", RegexOptions.IgnoreCase);

                if (ipMatch.Success)
                {
                    result.Ip = ipMatch.Groups[1].Value.Trim();
                }
                else
                {
                    result.Ip = "127.0.0.1";
                }

                result.Cookies = ParseCookies(headers);
                //Console.WriteLine("parse cookies: " + result.Cookies.Count);
                result.Cookies = InputSanitizer.SanitizeCookies(result.Cookies);
                
                //CF-Connecting-IP: 
                //result.File = Regex.Match(headers, @"(?<=\w\s)([\Wa-zA-Z0-9]+)(?=\sHTTP)", RegexOptions.Multiline).Value;
                result.RealPath = $"{AppDomain.CurrentDomain.BaseDirectory}{result.File}";
                return result;
            }
            catch
            {
                return new HTTPHeaders();
            }
            


        }

        public static Dictionary<string, string> ParseCookies(string headers)
        {
            Dictionary<string, string> cookies = new Dictionary<string, string>();
            try
            {
                string cookieHeader = Regex.Match(headers, @"Cookie:\s(.*?)\r\n", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    string[] cookiePairs = cookieHeader.Split(';');
                    foreach (string cookiePair in cookiePairs)
                    {
                        string[] parts = cookiePair.Trim().Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            cookies[key] = value;
                        }
                    }
                }
            }
            catch (Exception)
            {

            }

            return cookies;
        }

        public static string FileExtention(string file)
        {
            return Regex.Match(file, @"(?<=[\W])\w+(?=[\W]{0,}$)").Value;
        }
    }

    internal class Client
    {
        Socket client; // подключенный клиент
        HTTPHeaders Headers; // распарсенные заголовки
        private Encoding charEncoder = Encoding.UTF8;
        public string ip = "";
        
        public Client(Socket c)
        {
            client = c;
            handldddd();
        }

        public async Task handldddd()
        {
            try
            {
                byte[] buffer = new byte[1228800]; // 1200 kb, just in case
                //var responseBytes = new byte[1228800];

                var receivedBCount = await client.ReceiveAsync(buffer, SocketFlags.None); // Получаем запрос

                if (receivedBCount == 0)  //  Client disconnected
                {
                    sendResponse(client, "ok", "200", "text/html", null, null);
                    client.Close();
                    return;
                }

                string request = charEncoder.GetString(buffer, 0, receivedBCount);

                if (request == "")
                {
                    try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                    client.Close();
                    return;
                }

                Headers = HTTPHeaders.Parse(request);
               
                if (Headers.RealPath == null)
                {
                    if(Headers.File == null)
                    {
                        SendError(404);
                        client.Close();
                        return;
                    }
                    
                }
                else
                {
                    if (Headers.RealPath.IndexOf("..") != -1)
                    {
                        SendError(404);
                        client.Close();
                        return;
                    }
                }
                


                //if (System.IO.File.Exists(Headers.RealPath))
                GetSheet(Headers, request);
                //else
                //SendError(404);
                //client.Close();
            }
            catch
            {
                try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                return;
            }
            
        }
        
        
        public async Task GetSheet(HTTPHeaders head, string request)
        {
            try
            {
                string content_type = GetContentType(head);

                Dictionary<string, string> args = new();


                if (head.Method == "GET")
                {
                    

                    Interlocked.Decrement(ref AdminInfo.threads);

                    if (head.Ip != "127.0.0.1")
                    {
                        Cache cache = new();
                        var target_connects = cache.Get<Dictionary<string, string>>("anf_site_" + head.Ip);
                        if (target_connects != null)
                        {
                            if (target_connects.ContainsKey("last_date"))
                            {
                                var time_one = long.Parse(target_connects["last_date"]);
                                var connects = long.Parse(target_connects["connects"]);
                                var time_now = DateTimeOffset.Now.ToUnixTimeSeconds();
                                if (time_now <= time_one && connects >= 2)
                                {

                                    target_connects["connects"] = "0";
                                    cache.Set("anf_site_" + head.Ip, target_connects, 5);
                                    SendError(429);// TO MANY REQUESTS
                                    client.Close();
                                    return;
                                }
                                else
                                {
                                    if (time_now > time_one)
                                    {
                                        connects = 0;
                                    }
                                    else
                                    {
                                        connects++;
                                    }
                                    
                                    target_connects["connects"] = connects.ToString();
                                    target_connects["last_date"] = time_now.ToString();
                                    cache.Set("anf_site_" + head.Ip, target_connects, 5);
                                }
                            }
                            else
                            {
                                target_connects["connects"] = "1";
                                target_connects["last_date"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                                cache.Set("anf_site_" + head.Ip, target_connects, 5);
                            }
                        }
                        else
                        {
                            target_connects = new Dictionary<string, string>();
                            target_connects["last_date"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                            target_connects["connects"] = "1";
                            cache.Set("anf_site_" + head.Ip, target_connects, 5);
                        }
                    }

                    if (head.File.Contains('?'))
                    {
                        int pos = head.File.LastIndexOf('?');
                        string parames = head.File.Substring(pos + 1);
                        head.File = head.File.Substring(0, pos); // Обновление head.File, как в исходном коде

                        string[] keyValueParames = parames.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                        if (keyValueParames.Length >= 1)
                        {
                            foreach (var item in keyValueParames)
                            {
                                string[] keyValParam = item.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                                if (keyValParam.Length > 0)
                                {
                                    string key = Uri.UnescapeDataString(keyValParam[0]);
                                    string value = "";

                                    if (keyValParam.Length > 1)
                                    {
                                        value = Uri.UnescapeDataString(keyValParam[1]);
                                    }
                                    if (!string.IsNullOrEmpty(key))
                                    {
                                        args.Add(key, value);
                                    }
                                }
                            }
                        }
                        //Console.WriteLine(args.ToString());
                    }

                    ip = ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

                    args = InputSanitizer.SanitizeArgs(args);

                    if (head.File.StartsWith("/api/"))
                    {
                        //Console.WriteLine("parse cookies: " + head.Cookies.Count);
                        //Console.WriteLine(request);
                        string method = head.File.Substring("/api/".Length);

                        if (LoaderAPI.nameCmd_CommandInfo.ContainsKey(method))
                        {
                            var CmdEngine = LoaderAPI.nameCmd_CommandInfo[method];

                            var cache = new Cache();

                            var response = await SysUtils.CallCmd<AnswerTemplane>(CmdEngine.nameOfClass, CmdEngine.nameOfMethod, cache, new Database(cache), args, head.Cookies, false, head.Ip);

                            if (response != null)
                            {
#pragma warning disable CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
                                Dictionary<string, string> targetCookie = response.Cookies;
                                if (head.Cookies.Count > 0 || targetCookie.Count > 0)
                                {
                                    foreach (var setCookie in targetCookie.ToList())
                                    {
                                        if (head.Cookies.ContainsKey(setCookie.Key))
                                        {
                                            if (head.Cookies[setCookie.Key] == setCookie.Value)
                                            {
                                                targetCookie.Remove(setCookie.Key);
                                            }
                                        }
                                    }
                                }

                                sendResponse(client, response.htmlResponse.ToString(), "200", "text/html", targetCookie, response.RemoveCookies);
#pragma warning restore CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
                            }
                            else
                            {
                                //Console.WriteLine(response.htmlResponse);
                                //Console.WriteLine(response.ToString());
                                Console.WriteLine(CmdEngine.nameOfMethod);
                                sendResponse(client, method + " | " + head.File + " | " + ip + " | ", "200", "text/html"); 
                                //SendError(502);
                            }
                        }
                        else
                        {
                            sendResponse(client, method + " | " + head.File, "200", "text/html");

                            //SendError(502);
                        }
                        // string response = await engine.SiteEngine.Profile.Work(args, head);
                        // if (response == "502")
                        //{
                        //   SendError(502);
                        //}
                        //sendResponse(client, response, "200", "text/html");
                        return;
                    }

                    else
                    {
                        if (System.IO.File.Exists(head.RealPath))
                        {
                            FileStream fs = new FileStream(head.RealPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            string headers = $"HTTP/1.1 200 OK\nContent-type: {content_type}\nContent-Length: {fs.Length}\n\n";
                            // OUTPUT HEADERS    
                            byte[] data_headers = Encoding.UTF8.GetBytes(headers);
                            client.Send(data_headers, data_headers.Length, SocketFlags.None);
                            while (fs.Position < fs.Length)
                            {
                                byte[] data = new byte[1024];
                                long length = fs.Read(data, 0, data.Length);
                                client.Send(data, data.Length, SocketFlags.None);
                            }
                        }
                        else
                        {
                            //sendResponse(client, head.RealPath, "200", "text/html");
                            SendError(404);
                        }
                    }

                    return;
                }
                else
                {
                    if (head.File == "/handler")
                    {

                        try
                        {
                            if (request.LastIndexOf('\n') > 1)
                            {
                                string lastline = request.Substring(request.LastIndexOf('\n'));
                                // DATA
                                if (lastline == "" || lastline == null)
                                {
                                    try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                                    client.Close();
                                    return;
                                }

                                
                                var settingsr = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, MaxDepth = 256 };
                                var _jsonSerializer = JsonSerializer.Create(settingsr);
                                var parse_data = (JObject)JsonConvert.DeserializeObject(lastline, settingsr);


                                //var parse_data = JObject.Parse(lastline); // 11 строка
                                if (parse_data == null)
                                {
                                    try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                                    return;
                                }
                                JToken? group_id = parse_data["group_id"];

                                if (group_id == null)
                                {
                                    try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                                    return;
                                }
                                JToken? type = parse_data["type"];
                                if (type.ToString() == "confirmation")
                                {
                                    sendResponse(client, settings.confirmation_token, "200", "text/html", null, null);
                                    return;
                                }

                                if (type.ToString() == "message_new")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    
                                    //Interlocked.Increment(ref AdminInfo.threads);

                                    JToken obj_mess = parse_data["object"];
                                    Program.workerCommand(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "message_edit")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    client.Close();
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerCommand(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "message_event")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    client.Close();
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerCommand(obj_mess);
                                    return;
                                }
                                if (type.ToString() == "like_add")//like_remove
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    client.Close();
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerLike(obj_mess);
                                    return;
                                }
                                if (type.ToString() == "like_remove")//wall_reply_new
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    client.Close();
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerDisLike(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "wall_reply_new")//
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerComment(obj_mess);
                                    return;
                                }
                                if (type.ToString() == "wall_reply_delete")// 
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerDeleteComment(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "wall_repost")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerRepost(obj_mess);
                                    return;

                                }

                                if (type.ToString() == "group_leave")// 
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.workerDeleteComment(obj_mess);
                                    return;
                                }


                                if (type.ToString() == "donut_subscription_create")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.DONUT_WORKER(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "donut_subscription_prolonged")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.DONUT_PROLONGERWORKER(obj_mess);
                                    return;
                                }

                                if (type.ToString() == "donut_subscription_cancelled")
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    Program.DONUT_CANCELRWORKER(obj_mess);
                                    return;
                                }


                                if (type.ToString() == "group_join")// 
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    //Interlocked.Increment(ref AdminInfo.threads);
                                    JToken obj_mess = parse_data["object"];
                                    //Program.workerDeleteComment(obj_mess);
                                    return;
                                }
                                else
                                {
                                    try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                    client.Close();
                                }
                                return;
                            }
                            else
                            {
                                try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                                client.Close();
                                return;
                            }
                        }
                        catch
                        {
                            try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                        }
                    }
                    
                    else if (head.File.StartsWith("/api/"))
                    {
                        if (head.File.Contains('?'))
                        {
                            int pos = head.File.LastIndexOf('?');
                            string parames = head.File.Substring(pos + 1);
                            head.File = head.File.Substring(0, pos); // Обновление head.File, как в исходном коде

                            string[] keyValueParames = parames.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                            if (keyValueParames.Length >= 1)
                            {
                                foreach (var item in keyValueParames)
                                {
                                    string[] keyValParam = item.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (keyValParam.Length > 0)
                                    {
                                        string key = Uri.UnescapeDataString(keyValParam[0]);
                                        string value = "";

                                        if (keyValParam.Length > 1)
                                        {
                                            value = Uri.UnescapeDataString(keyValParam[1]);
                                        }

                                        if (!string.IsNullOrEmpty(key))
                                        {
                                            args.Add(key, value);
                                        }
                                    }
                                }
                            }
                            //Console.WriteLine(args.ToString());
                        }

                        string method = head.File.Substring("/api/".Length);

                        if (LoaderAPI.nameCmd_CommandInfo.ContainsKey(method))
                        {
                            var CmdEngine = LoaderAPI.nameCmd_CommandInfo[method];

                            var cache = new Cache();

                            var response = await SysUtils.CallCmd<AnswerTemplane>(CmdEngine.nameOfClass, CmdEngine.nameOfMethod, cache, new Database(cache), args, head.Cookies, true, ip);

                            if (response != null)
                            {
#pragma warning disable CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
                                Dictionary<string, string> targetCookie = response.Cookies;
                                if (head.Cookies.Count > 0 || targetCookie.Count > 0)
                                {
                                    foreach (var setCookie in targetCookie.ToList())
                                    {
                                        if (head.Cookies.ContainsKey(setCookie.Key))
                                        {
                                            if (head.Cookies[setCookie.Key] == setCookie.Value)
                                            {
                                                targetCookie.Remove(setCookie.Key);
                                            }
                                        }
                                    }
                                }

                                sendResponse(client, response.htmlResponse.ToString(), "200", "text/html", targetCookie, response.RemoveCookies);
#pragma warning restore CS8604 // Возможно, аргумент-ссылка, допускающий значение NULL.
                            }
                            else
                            {
                                SendError(502);
                            }
                        }
                        else
                        {
                            SendError(502);
                        }
                        // string response = await engine.SiteEngine.Profile.Work(args, head);
                        // if (response == "502")
                        //{
                        //   SendError(502);
                        //}
                        //sendResponse(client, response, "200", "text/html");
                        return;
                    }
                    else
                    {
                        try { sendResponse(client, "ok", "200", "text/html"); } catch { }
                        return;
                    }
                }
                
                
            }
            catch (Exception ex)
            {
                try { sendResponse(client, "ok", "200", "text/html", null, null); } catch { }
                AdminInfo.AddErrorList("Ошибка обработки запроса сайта", "Дата: " + DateTimeOffset.Now.ToString("G") + "<br><br>" +
                    "Метод: " + head.Method.ToString() + " | Файл: " + head.File.ToString() + "<br>" +
                    "Request:<br>" + request + "<br><br>Exception: " + ex.Message + "<br>Source: " + ex.Source + "<br><br>" +
                    "Stack trace: " + ex.StackTrace);
                Console.WriteLine(ex.Message);
                return;
                //Console.WriteLine($"Func: GetSheet()    link: {head.RealPath}\nException: {ex}/nMessage: {ex.Message}");
            }
        }

        
        public static Dictionary<string, string>? getDataHash(Dictionary<string, string> args, HTTPHeaders head, Cache cache)
        {
            Dictionary<string, string>? data = null;
            if (!args.ContainsKey("hash"))
            {
                if (head.Ip != "127.0.0.1")
                {
                    data = cache.Get<Dictionary<string, string>>("hash_" + head.Ip);
                    if (data != null)
                    {
                        cache.Set("hash_" + head.Ip, data, 15);
                    }
                }
                else
                {
                    data = null;
                }
            }
            else
            {
                if (args.ContainsKey("hash"))
                {
                    var datrter_hash = HttpUtility.UrlDecode(args["hash"]);
                    data = cache.Get<Dictionary<string, string>>("hash_" + datrter_hash);
                    if (data != null)
                    {
                        cache.Set("hash_" + datrter_hash, data, 15);
                    }
                }
                else
                {
                    data = null;
                }
            }
            
            return data;
        }


        public static string getDataTemplane(string name, Cache cache)
        {
            string data = "";
            var res = cache.Get<string>("templane_" + name);
            if (res == null || res == "" || res == "not")
            {
                if (res == "not")
                {
                    return data;
                }
                string path = @"/root/vk/templanes/" + name;

                if (!System.IO.File.Exists(path))
                {
                    path = "templanes/" + name;
                }

                if (!System.IO.File.Exists(path))
                {
                    cache.Set("templane_" + name, "not", 50);
                    return data;
                }

                using (FileStream fstreams = File.OpenRead(path))
                {
                    // выделяем массив для считывания данных из файла
                    byte[] buffer = new byte[fstreams.Length];
                    // считываем данные
                    fstreams.Read(buffer, 0, buffer.Length);
                    // декодируем байты в строку
                    res = Encoding.Default.GetString(buffer);
                }

                cache.Set("templane_" + name, res, 50);

                return res;
            }
            else
            {

                return res;
            }
        }


        public string getRubsMoney(string money)
        {
            return getKKsMoney(money, "en-U") + "₽";
        }

        public string getDollMoney(string money)
        {
            return getKKsMoney(money, "en-US") + "$";
        }

        public string getEuroMoney(string money)
        {
            return getKKsMoney(money, "en-U") + "€";
        }
        /*
         * ru-RU - рубли
         * en-US - доллары
         * fr-FR - евро
         */
        public string getKKsMoney(string money, string country = "ru-RU")
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


        
        public async void SendError(long code)
        {
            string html = $"<html><head><title></title></head><body><h1>Error {code}</h1></body></html>";
            string headers = $"HTTP/1.1 {code} OK\nContent-type: text/html\nContent-Length: {html.Length}\n\n{html}";
            byte[] data = Encoding.UTF8.GetBytes(headers);
            await client.SendAsync(data, SocketFlags.None);
            client.Close();
        }

        string GetContentType(HTTPHeaders head)
        {
            string result = "";
            string format = HTTPHeaders.FileExtention(Headers.File);
            switch (format)
            {
                //image
                case "gif":
                case "jpeg":
                case "pjpeg":
                case "png":
                case "tiff":
                case "webp":
                    result = $"image/{format}";
                    break;
                case "svg":
                    result = $"image/svg+xml";
                    break;
                case "ico":
                    result = $"image/vnd.microsoft.icon";
                    break;
                case "wbmp":
                    result = $"image/vnd.map.wbmp";
                    break;
                case "jpg":
                    result = $"image/jpeg";
                    break;
                // text
                case "css":
                    result = $"text/css";
                    break;
                case "html":
                    result = $"text/{format}";
                    break;
                case "javascript":
                case "js":
                    result = $"text/javascript";
                    break;
                case "php":
                    result = $"text/html";
                    break;
                case "htm":
                    result = $"text/html";
                    break;
                default:
                    result = "application/unknown";
                    break;
            }
            return result;
        }


        private void sendResponse(Socket clientSocket, string strContent, string responseCode,
                                  string contentType, Dictionary<string, string>? setCookies = null, Dictionary<string, string>? removeCookies = null)
        {
            byte[] bContent = charEncoder.GetBytes(strContent);
            sendResponse(clientSocket, bContent, responseCode, contentType, setCookies, removeCookies);

        }

        // For byte arrays
        // headerBuilder.Append($"Set-Cookie: {cookieName}=; Max-Age=0; Path=/\r\n");
        private async void sendResponse(Socket clientSocket, byte[] bContent, string responseCode,
                                  string contentType, Dictionary<string, string>? setCookies = null, Dictionary<string, string>? removeCookies = null)
        {
            try
            {
                StringBuilder headerBuilder = new StringBuilder();
                headerBuilder.Append($"HTTP/1.1 {responseCode}\r\n");
                headerBuilder.Append($"Content-Length: {bContent.Length.ToString()}\r\n");
                headerBuilder.Append($"Content-Type: {contentType}\r\n");

                // Добавляем заголовки Set-Cookie
                if (setCookies != null)
                {
                    foreach (var cookie in setCookies)
                    {
                        headerBuilder.Append($"Set-Cookie: {cookie.Key}={cookie.Value}; Path=/; HttpOnly; Secure\r\n"); // Path=/ устанавливает cookie для всего домена
                    }
                }

                if (removeCookies != null)
                {
                    foreach (var cookie in removeCookies)
                    {
                        headerBuilder.Append($"Set-Cookie: {cookie.Key}=; Path=/; Max-Age=0; HttpOnly; Secure\r\n"); // Path=/ устанавливает cookie для всего домена
                    }
                }

                headerBuilder.Append("\r\n");
                byte[] bHeader = charEncoder.GetBytes(headerBuilder.ToString());
                await clientSocket.SendAsync(bHeader, SocketFlags.None);
                await clientSocket.SendAsync(bContent, SocketFlags.None);
                clientSocket.Close();
            }
            catch (Exception)
            {

            }
        }


        public static string setMenu(string templane, Dictionary<string, string>UserData)
        {
            long user_id = long.Parse(UserData["userid"]);
            if (settings.OWNER_IDS.ContainsKey(user_id))
            {
                var templanesMenu = templane.Split("<!-- GRANDMENU -->");
                if (templanesMenu == null || templanesMenu.Length <= 1)
                {
                    return "502";
                }

                templanesMenu[1] = "";
                templanesMenu[1] = "<!-- GRANDMENU --><div id=\"headerMenu\" class=\"headerMenu\" " +
                    "style =\"width:100%;height:auto !important;\">\r\n<div class=\"container\">\r\n" +
                    "<div class=\"navbar-header\">\r\n<button title=\"\" type=\"button\" class=\"navbar-toggle\" " +
                    "data-bs-toggle=\"collapse\" data-bs-target=\".headerMenu-navbar-collapse\">\r\n<span class=\"icon-bar\">" +
                    "</span>\r\n<span class=\"icon-bar\"></span>\r\n<span class=\"icon-bar\"></span>\r\n</button>\r\n" +
                    "</div>\r\n<div class=\"headerMenu-navbar-collapse collapse\">\r\n<ul class=\"nav navbar-nav\">\r\n" +
                    "<li class=\"nav-item\">\r\n<a href=\"profile?hash=%hash%\" class=\"nav-link\">Профиль</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"peers?hash=%hash%\" class=\"nav-link\">Беседы</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"admintickets?hash=%hash%\" class=\"nav-link\">Тикеты</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"botbans?hash=%hash%\" class=\"nav-link\">Блокировки</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"admin?hash=%hash%\" class=\"nav-link\">Админ Панель</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"cassa?hash=%hash%\" class=\"nav-link\">Пополнить счёт</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"shop?hash=%hash%\" class=\"nav-link\">Магазин</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"exit?hash=%hash%\" class=\"nav-link\">Выход</a>\r\n</li>\r\n</ul>" +
                    "\r\n</div>\r\n</div>\r\n</div>";

                templane = string.Join("", templanesMenu);
            }
            else
            {
                var templanesMenu = templane.Split("<!-- GRANDMENU -->");
                if (templanesMenu == null || templanesMenu.Length <= 1)
                {
                    return "502";
                }

                templanesMenu[1] = "";
                templanesMenu[1] = "<!-- GRANDMENU --><div id=\"headerMenu\" class=\"headerMenu\" " +
                    "style =\"width:100%;height:auto !important;\">\r\n<div class=\"container\">\r\n" +
                    "<div class=\"navbar-header\">\r\n<button title=\"\" type=\"button\" class=\"navbar-toggle\" " +
                    "data-bs-toggle=\"collapse\" data-bs-target=\".headerMenu-navbar-collapse\">\r\n<span class=\"icon-bar\">" +
                    "</span>\r\n<span class=\"icon-bar\"></span>\r\n<span class=\"icon-bar\"></span>\r\n</button>\r\n" +
                    "</div>\r\n<div class=\"headerMenu-navbar-collapse collapse\">\r\n<ul class=\"nav navbar-nav\">\r\n" +
                    "<li class=\"nav-item\">\r\n<a href=\"profile?hash=%hash%\" class=\"nav-link\">Профиль</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"peers?hash=%hash%\" class=\"nav-link\">Беседы</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"tickets?hash=%hash%\" class=\"nav-link\">Тикеты</a>\r\n" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"cassa?hash=%hash%\" class=\"nav-link\">Пополнить счёт</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"shop?hash=%hash%\" class=\"nav-link\">Магазин</a>\r\n</li>" +
                    "</li>\r\n<li class=\"nav-item\">\r\n<a href=\"exit?hash=%hash%\" class=\"nav-link\">Выход</a>\r\n</li>\r\n</ul>" +
                    "\r\n</div>\r\n</div>\r\n</div>";

                templane = string.Join("", templanesMenu);
            }
            return templane;
        }

    }
}
