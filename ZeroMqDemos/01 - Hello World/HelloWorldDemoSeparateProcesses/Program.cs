using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelloWorldDemoSeparateProcesses;

namespace HelloWorldDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Server server = new Server();
            server.Run();

            foreach (Client client in Enumerable.Range(0, 5).Select(
                x => new Client(string.Format("client {0}", x))))
            {
                client.Run();
            }

            Console.ReadLine();
        }
    }
}
