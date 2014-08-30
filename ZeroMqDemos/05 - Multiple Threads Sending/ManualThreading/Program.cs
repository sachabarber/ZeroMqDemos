using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;
using NetMQ.Sockets;

namespace ManualThreadingDemo
{
    public class Program
    {
        public void Run()
        {

            //NOTES
            //1. Use ThreadLocal<DealerSocket> where each thread has
            //  its own client DealerSocket to talk to server
            //2. Each thread can send using it own socket
            //3. Each thread socket is added to poller
            
            ThreadLocal<DealerSocket> clientSocketPerThread = 
                new ThreadLocal<DealerSocket>();
            int delay = 3000;
            Poller poller = new Poller();

            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateRouterSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    //start some threads, each with its own DealerSocket
                    //to talk to the server socket. Creates lots of sockets, 
                    //but no nasty race conditions no shared state, each 
                    //thread has its own socket, happy days
                    for (int i = 0; i < 3; i++)
                    {
                        Task.Factory.StartNew((state) =>
                        {
                            DealerSocket client = null;

                            if (!clientSocketPerThread.IsValueCreated)
                            {
                                client = ctx.CreateDealerSocket();
                                client.Connect("tcp://127.0.0.1:5556");
                                client.ReceiveReady += Client_ReceiveReady;
                                clientSocketPerThread.Value = client;
                                poller.AddSocket(client);
                            }
                            else
                            {
                                client = clientSocketPerThread.Value;
                            }

                            while (true)
                            {
                                var messageToServer = new NetMQMessage();
                                messageToServer.AppendEmptyFrame();
                                messageToServer.Append(state.ToString());
                                client.SendMessage(messageToServer);
                                Thread.Sleep(delay);
                            }

                        },string.Format("client {0}", i), TaskCreationOptions.LongRunning);
                    }

                    //start the poller
                    Task task = Task.Factory.StartNew(poller.Start);

                    //server loop
                    while (true)
                    {
                        var clientMessage = server.ReceiveMessage();
                        Console.WriteLine("========================");
                        Console.WriteLine(" INCOMING CLIENT MESSAGE ");
                        Console.WriteLine("========================");
                        for (int i = 0; i < clientMessage.FrameCount; i++)
                        {
                            Console.WriteLine("Frame[{0}] = {1}", i,
                                clientMessage[i].ConvertToString());
                        }

                        if (clientMessage.FrameCount == 3)
                        {
                            var clientAddress = clientMessage[0];
                            var clientOriginalMessage = clientMessage[2].ConvertToString();
                            string response = string.Format("{0} back from server {1}",
                                clientOriginalMessage, DateTime.Now.ToLongTimeString());
                            var messageToClient = new NetMQMessage();
                            messageToClient.Append(clientAddress);
                            messageToClient.AppendEmptyFrame();
                            messageToClient.Append(response);
                            server.SendMessage(messageToClient);
                        }
                    }
                }
            }
        }

        void Client_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            bool hasmore = false;
            e.Socket.Receive(out hasmore);
            if (hasmore)
            {
                string result = e.Socket.ReceiveString(out hasmore);
                Console.WriteLine("REPLY " + result);
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }




    }
}
