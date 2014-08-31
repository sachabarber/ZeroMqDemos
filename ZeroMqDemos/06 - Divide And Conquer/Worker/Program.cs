using System;
using System.Threading;
using NetMQ;


namespace Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Task Worker
            // Connects PULL socket to tcp://localhost:5557
            // collects workload for socket from Ventilator via that socket
            // Connects PUSH socket to tcp://localhost:5558
            // Sends results to Sink via that socket
            Console.WriteLine("====== WORKER ======");


            using (NetMQContext ctx = NetMQContext.Create())
            {
                //socket to receive messages on
                using (var receiver = ctx.CreatePullSocket())
                {
                    receiver.Connect("tcp://localhost:5557");

                     //socket to send messages on
                    using (var sender = ctx.CreatePushSocket())
                    {
                        sender.Connect("tcp://localhost:5558");

                        //process tasks forever
                        while (true)
                        {
                            //workload from the vetilator is a simple delay
                            //to simulate some work being done, see
                            //Ventilator.csproj Proram.cs for the workload sent
                            //In real life some more meaningful work would be done
                            string workload = receiver.ReceiveString();

                            //simulate some work being done
                            Thread.Sleep(int.Parse(workload));

                            //send results to sink, sink just needs to know worker
                            //is done, message content is not important, just the precence of
                            //a message means worker is done. 
                            //See Sink.csproj Proram.cs 
                            Console.WriteLine("Sending to Sink");
                            sender.Send(string.Empty);
                        }
                    }

                }
            }

        }
    }
}
