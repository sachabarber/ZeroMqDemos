using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;

namespace NetMQSchedulerDemo
{
    public class Client : IDisposable
    {
        private readonly NetMQContext context;
        private readonly string address;
        private Poller poller;
        private NetMQScheduler scheduler;
        private NetMQSocket clientSocket;

        public Client(NetMQContext context, string address)
        {
            this.context = context;
            this.address = address;
        }

        public void Start()
        {
            poller = new Poller();
            clientSocket = context.CreateDealerSocket();
            clientSocket.ReceiveReady += clientSocket_ReceiveReady;
            clientSocket.Connect(address);
            scheduler = new NetMQScheduler(context, poller);
            Task.Factory.StartNew(poller.Start, TaskCreationOptions.LongRunning);
        }

        void clientSocket_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            string result = e.Socket.ReceiveString();
            Console.WriteLine("REPLY " + result);
        }

        public async Task SendMessage(NetMQMessage message)
        {
            // instead of creating inproc socket which listen to messages and then send 
            //to the server we just creating task and run a code on
            // the poller thread which the the thread of the clientSocket
            Task task = new Task(() => clientSocket.SendMessage(message));
            task.Start(scheduler);
            await task;
            await ReceiveMessage();
        }


        public async Task ReceiveMessage()
        {
            Task task = new Task(() =>
            {
                var result = clientSocket.ReceiveString();
                Console.WriteLine("REPLY " + result);
            });
            task.Start(scheduler);
            await task;
        }


        public void Dispose()
        {
            scheduler.Dispose();
            clientSocket.Dispose();
            poller.Stop();
        }
    }
}
