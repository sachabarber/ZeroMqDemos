using System;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace HelloWorldDemo
{
    sealed class Client
    {
        private readonly string clientName;

        public Client(string clientName)
        {
            this.clientName = clientName;
        }

        public void Run()
        {
            Task.Run(() =>
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
            });

        }
    }
}
