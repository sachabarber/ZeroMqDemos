using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;
using NetMQ;
using NetMQ.Sockets;

namespace ZeroMqIdentity
{
    public class Program : IDisposable
    {
        private List<RequestSocket> clients = new List<RequestSocket>();

        public void Run()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateRouterSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    CreateClient(ctx, "A_");
                    CreateClient(ctx, "B_");
                    CreateClient(ctx, "C_");
                    CreateClient(ctx, "D_");

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

                        var clientAddress = clientMessage[0];
                        var clientOriginalMessage = clientMessage[2].ConvertToString();
                        string response = string.Format("{0} back from server", 
                            clientOriginalMessage);

                        // "B_" client is special
                        if (clientOriginalMessage.StartsWith("B_"))
                        {
                            response = string.Format(
                                "special Message for 'B' back from server");
                        }

                        var messageToClient = new NetMQMessage();
                        messageToClient.Append(clientAddress);
                        messageToClient.AppendEmptyFrame();
                        messageToClient.Append(response);
                        server.SendMessage(messageToClient);
                    }
                }
            }

            Console.ReadLine();
        }

        private static void Main(string[] args)
        {
            Program p = new Program();
            p.Run();
        }


        private void CreateClient(NetMQContext ctx, string prefix)
        {
            Task.Run(() =>
            {
                var client = ctx.CreateRequestSocket();
                clients.Add(client);
                client.Connect("tcp://127.0.0.1:5556");
                client.Send(string.Format("{0}Hello", prefix));

                //read client message
                var echoedServerMessage = client.ReceiveString();
                Console.WriteLine(
                    "\r\nClient Prefix is : '{0}', Server Message : '{1}'",
                    prefix, echoedServerMessage);

            });
        }

        public void Dispose()
        {
            foreach (var client in clients)
            {
                client.Dispose();
            }
        }
    }
}
