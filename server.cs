using GrandManager.engine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Utilities;

namespace GrandManager
{
    internal class server
    {
        
        public bool running = false; //Запущено ли?

        private int timeout = 8; // Лиммт времени на приём данных.
        private Encoding charEncoder = Encoding.UTF8; // Кодировка
        private Socket serverSocket; // Нащ сокет
        private string contentPath; // Корневая папка для контента

        // Поодерживаемый контент нашим сервером
        // Вы можете добавить больше
        // Смотреть здесь: http://www.webmaster-toolkit.com/mime-types.shtml
        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        { 
            //{ "extension", "content type" }
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"},
            { "json", "application/json" }
        };

        public bool start(IPAddress ipAddress, int port, int maxNOfCon, string contentPath)
        {
            if (running) return false; // Если уже запущено, то выходим

            try
            {
                // tcp/ip сокет (ipv4)
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(ipAddress, port));
                serverSocket.Listen(maxNOfCon);
                serverSocket.ReceiveTimeout = timeout;
                serverSocket.SendTimeout = timeout;
                running = true;
                this.contentPath = contentPath;
            }
            catch { return false; }

            // Наш поток ждет новые подключения и создает новые потоки.
            Thread requestListenerT = new Thread(() =>
            {
                while (running)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = serverSocket.Accept();
                        // Создаем новый поток для нового клиента и продолжаем слушать сокет.
                        try
                        {
                            Interlocked.Increment(ref AdminInfo.threads);
                            //Interlocked.Increment(ref AdminInfo.requests);
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            Task.Run(async () => handleTheRequest(clientSocket));
                        }
                        catch
                        {
                            try { clientSocket.Close(); } catch { }
                        }
                        

                        /*Thread requestHandler = new Thread(() =>
                        {
                            Interlocked.Increment(ref AdminInfo.threads);
                            //Interlocked.Increment(ref AdminInfo.requests);
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try { handleTheRequest(clientSocket); }
                            catch
                            {
                                try { clientSocket.Close(); } catch { }
                            }
                        });
                        requestHandler.Start();*/
                    }
                    catch { }
                }
            });
            requestListenerT.Start();

            return true;
        }

        public void stop()
        {
            if (running)
            {
                running = false;
                try { serverSocket.Close(); }
                catch { }
                serverSocket = null;
            }
        }

        private void handleTheRequest(Socket clientSocket)
        {
            try
            {
                AdminInfo.addRequest(false);
                byte[] buffer = new byte[10240]; // 10 kb, just in case
                int receivedBCount = clientSocket.Receive(buffer); // Получаем запрос
                string strReceived = charEncoder.GetString(buffer, 0, receivedBCount);

                //Console.WriteLine(strReceived);
                // Парсим запрос
                //string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

                string response = Encoding.UTF8.GetString(buffer, 0, receivedBCount);

                string lastline = strReceived.Substring(strReceived.LastIndexOf('\n'));


                //requestedFile = requestedFile.Replace("/", "\\").Replace("\\..", ""); // Not to go back
                //length = requestedFile.Length - start;
                //string extension = requestedFile.Substring(start, length);
                if (strReceived.LastIndexOf('\n') > 1)
                {
                    //micro_start_command = settings.GetMicroTime();
                    //Console.WriteLine(parseRecived[10]);
                    JObject parse_data = JObject.Parse(lastline); // 11 строка
                    JToken? group_id = parse_data["group_id"];
                    if (group_id == null)
                    {
                        sendResponse(clientSocket, "ok", "200", "text/html");
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    JToken? type = parse_data["type"];
                    if (type.ToString() == "confirmation")
                    {
                        sendResponse(clientSocket, settings.confirmation_token, "200", "text/html");
                        Interlocked.Decrement(ref AdminInfo.threads);
                        return;
                    }
                    if (type.ToString() == "message_new")
                    {
                        sendResponse(clientSocket, "ok", "200", "text/html");
                        //Interlocked.Increment(ref AdminInfo.threads);
                        JToken obj_mess = parse_data["object"];
                        Program.workerCommand(obj_mess);
                        return;
                    }
                    Interlocked.Decrement(ref AdminInfo.threads);
                    sendResponse(clientSocket, "ok", "200", "text/html");
                    //Console.WriteLine("Конец");
                    //sendOkResponse(clientSocket, System.IO.File.ReadAllBytes(contentPath + requestedFile), extensions[extension]);
                    //Console.WriteLine(parse_data);
                }
                else
                {
                    //Console.WriteLine(requestedFile);
                    //sendResponse(clientSocket, "1ok" + requestedFile, "200", "text/html");
                    //notFound(clientSocket);
                    Interlocked.Decrement(ref AdminInfo.threads);
                    sendResponse(clientSocket, "ok", "200", "text/html");
                }
            }
            catch
            {
                Interlocked.Decrement(ref AdminInfo.threads);
                sendResponse(clientSocket, "ok", "200", "text/html");
            }
        }
        private void notImplemented(Socket clientSocket)
        {

            sendResponse(clientSocket, "<html><head><meta " +
        "http-equiv =\"Content-Type\" content=\"text/html; " +
        "charset = utf-8\">" +
        "</head><body><h2> Grand Web" +
        "Server </h2><div> 501 - Method Not" +
        "Implemented </div></body></html> ", 
        "501 Not Implemented", "text/html");

        }

        private void notFound(Socket clientSocket)
        {

            sendResponse(clientSocket, "<html><head><meta " +
        "http-equiv =\"Content-Type\" content=\"text/html; " +
        "charset = utf-8\">" +
        "</head><body><h2> Grand Web" +
        "Server </h2><div> 404 - Method Not" +
        "Implemented </div></body></html> ",
                "404 Not Found", "text/html");
        }

        private void sendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            sendResponse(clientSocket, bContent, "200 OK", contentType);
        }

        // For strings
        private void sendResponse(Socket clientSocket, string strContent, string responseCode,
                                  string contentType)
        {
            byte[] bContent = charEncoder.GetBytes(strContent);
            sendResponse(clientSocket, bContent, responseCode, contentType);
        }

        // For byte arrays
        private void sendResponse(Socket clientSocket, byte[] bContent, string responseCode,
                                  string contentType)
        {
            try
            {
                byte[] bHeader = charEncoder.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Grand Web Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch { }
        }

        /*public EndPolong Ip; // представляет ip-адрес
        long Listen; // представляет наш port
        Socket Listener; // представляет объект, который ведет прослушивание
        public bool Active; // представляет состояние сервера, работает он(true) или нет(false)
        struct HTTPHeaders
        {
            public string Method;
            public string RealPath;
            public string File;
        }

        public server(string ip, long port)
        {
            this.Listen = port;
            this.Ip = new IPEndPoint(IPAddress.Parse(ip), Listen);
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            if (!Active)
            {
                Listener.Bind(Ip);
                Listener.Listen(16);
                Active = true;

                while (Active)
                {
                    ThreadPool.QueueUserWorkItem(
                            new WaitCallback(ClientThread),
                            Listener.Accept()
                            );
                }
            }
            else
                Console.WriteLine("Server was started");
        }

        public void Stop()
        {
            if (Active)
            {
                Listener.Close();
                Active = false;
            }
            else
                Console.WriteLine("Server was stopped");
        }

        public void ClientThread(object client)
        {
            new Client((Socket)client);
        }

        */
        
    }
}
