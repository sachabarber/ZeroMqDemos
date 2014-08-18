using System;
using NetMQ;

namespace HelloWorldDemo
{
    class Program
    {
        private static void Main(string[] args)
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateResponseSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    using (var client = ctx.CreateRequestSocket())
                    {
                        client.Connect("tcp://127.0.0.1:5556");

                        client.Send("Hello");

                        string fromClientMessage = server.ReceiveString();

                        Console.WriteLine("From Client: {0}", fromClientMessage);

                        server.Send("Hi Back");

                        string fromServerMessage = client.ReceiveString();

                        Console.WriteLine("From Server: {0}", fromServerMessage);

                        Console.ReadLine();
                    }
                }
            }
        }
    }
}
