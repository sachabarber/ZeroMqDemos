using System;
using NetMQ;


namespace Ventilator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Task Ventilator
            // Binds PUSH socket to tcp://localhost:5557
            // Sends batch of tasks to workers via that socket
            Console.WriteLine("====== VENTILATOR ======");
           
            using (NetMQContext ctx = NetMQContext.Create())
            {
                //socket to send messages on
                using (var sender = ctx.CreatePushSocket())
                {
                    sender.Bind("tcp://*:5557");

                    using (var sink = ctx.CreatePushSocket())
                    {
                        sink.Connect("tcp://localhost:5558");

                        Console.WriteLine("Press enter when worker are ready");
                        Console.ReadLine();
                        
                        //the first message it "0" and signals start of batch
                        //see the Sink.csproj Program.cs file for where this is used
                        Console.WriteLine("Sending start of batch to Sink");    
                        sink.Send("0");



                        Console.WriteLine("Sending tasks to workers");

                        //initialise random number generator
                        Random rand= new Random(0);

                        //expected costs in Ms
                        int totalMs = 0;
                        
                        //send 100 tasks (workload for tasks, is just some random sleep time that
                        //the workers can perform, in real life each work would do more than sleep
                        for (int taskNumber = 0; taskNumber < 100; taskNumber++)
                        {
                            //Random workload from 1 to 100 msec
                            int workload = rand.Next(0, 100);
                            totalMs += workload;
                            Console.WriteLine("Workload : {0}", workload);
                            sender.Send(workload.ToString());
                        }
                        Console.WriteLine("Total expected cost : {0} msec", totalMs);
                        Console.WriteLine("Press Enter to quit");
                        Console.ReadLine();
                    }
                }
            }
        }
    }
}
