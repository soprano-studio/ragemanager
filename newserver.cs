using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GrandManager.engine;

namespace GrandManager
{
    internal class newserver
    {
        
        public EndPoint Ip; // представляет ip-адрес
        int Listen; // представляет наш port
        Socket Listener; // представляет объект, который ведет прослушивание
        public bool Active; // представляет состояние сервера, работает он(true) или нет(false)
        

        public newserver(string ip, int port)
        {
            this.Listen = port;
            this.Ip = new IPEndPoint(IPAddress.Parse(ip), Listen);
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public async Task Start()
        {
            try
            {
                if (!Active)
                {
                    Listener.Bind(Ip);
                    Listener.Listen(1000);
                    Active = true;

                    Action onCompleted = () =>
                    {
                        Listener.Close();
                        Active = false;
                        //vk.addError(99999, "Error CallBack, restart server");
                        Start();
                    };

                    var thread = new Thread(
                      async () =>
                      {
                          try
                          {
                              while (Active)
                              {
                                  //Socket clientSocket;
                                  try
                                  {
                                      var clientSocket = await Listener.AcceptAsync();


                                      ThreadPool.QueueUserWorkItem(a =>
                                      {
                                          //Interlocked.Increment(ref AdminInfo.requests);
                                          clientSocket.ReceiveTimeout = 100;
                                          clientSocket.SendTimeout = 100;
                                          try { ClientThread(clientSocket); }
                                          catch
                                          {
                                              try { clientSocket.Close(); } catch { }
                                          }
                                      });

                                  }
                                  catch { }
                              }
                              Active = false;
                          }
                          finally
                          {
                              onCompleted();
                          }
                      });
                    Console.WriteLine("Server was started");
                    thread.Start();
                }
            }
            catch
            {

            }
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

    }
}
