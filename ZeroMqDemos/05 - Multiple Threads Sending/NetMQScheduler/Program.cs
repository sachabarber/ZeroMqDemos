using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using NetMQ;
using NetMQ.Sockets;
using NetMQSchedulerDemo;
using NUnit.Framework;

namespace NetMQSchedulerDemo
{
    public class Program
    {
        public void Run()
        {
            //NOTES
            //1. Use NetMQs NetMQScheduler to communicate with the 
            //   server. All Send/Receive MUST be done via the 
            //   NetMQScheduler and TPL Tasks. See the Client class 
            //   for more information on this

            int delay = 3000;

            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateRouterSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    using (var client = new Client(ctx, "tcp://127.0.0.1:5556"))
                    {
                        client.Start();

                        //start some theads, each thread will use the 
                        //Clients NetMQScheduler to send/receieve messages 
                        //to/from the server
                        for (int i = 0; i < 2; i++)
                        {
                            Task.Factory.StartNew(async (state) =>
                            {
                                while (true)
                                {
                                    var messageToServer = new NetMQMessage();
                                    messageToServer.AppendEmptyFrame();
                                    messageToServer.Append(state.ToString());
                                    await client.SendMessage(messageToServer);
                                    Thread.Sleep(delay);
                                }
                            }, string.Format("client {0}", i), TaskCreationOptions.LongRunning);
                        }

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
        }

        [STAThread]
        public static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }
    }
}
