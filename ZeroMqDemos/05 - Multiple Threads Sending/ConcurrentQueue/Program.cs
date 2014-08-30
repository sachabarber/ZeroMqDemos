using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace ConcurrentQueueDemo
{
    public class Program
    {
        public void Run()
        {
            //NOTES
            //1. Use many threads each writing to ConcurrentQueue
            //2. Extra thread to read from ConcurrentQueue, and this is the one that 
            //   will deal with writing to the server
            ConcurrentQueue<string> messages = new ConcurrentQueue<string>();
            int delay = 3000;
            Poller poller = new Poller();

            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateRouterSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    //start some threads, where each thread, will use a client side
                    //broker (simple thread that monitors a CooncurrentQueue), where
                    //ONLY the client side broker talks to the server
                    for (int i = 0; i < 3; i++)
                    {
                        Task.Factory.StartNew((state) =>
                        {
                            while (true)
                            {
                                messages.Enqueue(state.ToString());
                                Thread.Sleep(delay);
                            }

                        }, string.Format("client {0}", i), TaskCreationOptions.LongRunning);
                    }

                    //single sender loop
                    Task.Factory.StartNew((state) =>
                    {
                        var client = ctx.CreateDealerSocket();
                        client.Connect("tcp://127.0.0.1:5556");
                        client.ReceiveReady += Client_ReceiveReady;
                        poller.AddSocket(client);

                        while (true)
                        {
                            string clientMessage = null;
                            if (messages.TryDequeue(out clientMessage))
                            {
                                var messageToServer = new NetMQMessage();
                                messageToServer.AppendEmptyFrame();
                                messageToServer.Append(clientMessage);
                                client.SendMessage(messageToServer);
                            }
                        }

                    }, TaskCreationOptions.LongRunning);


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

