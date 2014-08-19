using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ;

namespace HelloWorldSeparateServer
{
    sealed class Server
    {

        public static void Main(string[] args)
        {
            Server s = new Server();
            s.Run();
        }

        public void Run()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateResponseSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");


                    while (true)
                    {
                        string fromClientMessage = server.ReceiveString();
                        Console.WriteLine("From Client: {0} running on ThreadId : {1}",
                            fromClientMessage, Thread.CurrentThread.ManagedThreadId);
                        server.Send("Hi Back");
                    }

                }
             }
        }
    }
}
