using System;
using System.Threading;
using NetMQ;


namespace HelloWorldSeparateClient
{
    sealed class Client
    {
        private string clientName;

        public static void Main(string[] args)
        {
            Client c = new Client();
            c.clientName = args[0];
            c.Run();
        }

        public void Run()
        {
            
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var client = ctx.CreateRequestSocket())
                {
                    client.Connect("tcp://127.0.0.1:5556");
                    while (true)
                    {
                        client.Send(string.Format("Hello from client {0}", clientName));
                        string fromServerMessage = client.ReceiveString();
                        Console.WriteLine("From Server: {0} running on ThreadId : {1}",
                            fromServerMessage, Thread.CurrentThread.ManagedThreadId);
                        Thread.Sleep(5000);
                    }
                }
            }
            

        }
    }

}
