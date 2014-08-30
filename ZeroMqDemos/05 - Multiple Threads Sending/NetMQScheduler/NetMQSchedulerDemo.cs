using System;
using System.Threading;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.zmq;
using NUnit.Framework;
using System.Threading.Tasks;
using Poller = NetMQ.Poller;

namespace SendMore
{
    [TestFixture]
    public class PollerTests
    {
        [Test]
        public void SingleSocketPollTest()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                using (var rep = contex.CreateResponseSocket())
                {
                    rep.Bind("tcp://127.0.0.1:5002");

                    using (var req = contex.CreateRequestSocket())
                    using (Poller poller = new Poller())
                    {
                        req.Connect("tcp://127.0.0.1:5002");

                        //The ReceiveReady event is raised by the Poller
                        rep.ReceiveReady += (s, a) =>
                        {
                            bool more;
                            string m = a.Socket.ReceiveString(out more);

                            Assert.False(more);
                            Assert.AreEqual("Hello", m);

                            a.Socket.Send("World");
                        };


                        poller.AddSocket(rep);

                        Task pollerTask = Task.Factory.StartNew(poller.Start);
                        req.Send("Hello");

                        bool more2;
                        string m1 = req.ReceiveString(out more2);

                        Assert.IsFalse(more2);
                        Assert.AreEqual("World", m1);

                        poller.Stop();

                        Thread.Sleep(100);
                        Assert.IsTrue(pollerTask.IsCompleted);
                    }
                }
            }
        }
        
        [Test]
        public void AddSocketDuringWorkTest()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                // we are using three responses to make sure we actually 
                //move the correct socket and other sockets still work
                using (var router = contex.CreateRouterSocket())
                using (var router2 = contex.CreateRouterSocket())
                {
                    router.Bind("tcp://127.0.0.1:5002");
                    router2.Bind("tcp://127.0.0.1:5003");

                    using (var dealer = contex.CreateDealerSocket())
                    using (var dealer2 = contex.CreateDealerSocket())
                    using (Poller poller = new Poller())
                    {
                        dealer.Connect("tcp://127.0.0.1:5002");
                        dealer2.Connect("tcp://127.0.0.1:5003");

                        bool router1arrived = false;
                        bool router2arrived = false;


                        bool more;

                        //The ReceiveReady event is raised by the Poller
                        router2.ReceiveReady += (s, a) =>
                        {
                            router2.Receive(out more);
                            router2.Receive(out more);
                            router2arrived = true;
                        };

                        //The ReceiveReady event is raised by the Poller
                        router.ReceiveReady += (s, a) =>
                        {
                            router1arrived = true;

                            router.Receive(out more);
                            router.Receive(out more);

                            poller.AddSocket(router2);
                        };

                        poller.AddSocket(router);

                        Task task = Task.Factory.StartNew(poller.Start);

                        dealer.Send("1");
                        Thread.Sleep(300);
                        dealer2.Send("2");
                        Thread.Sleep(300);

                        poller.Stop(true);
                        task.Wait();

                        Assert.IsTrue(router1arrived);
                        Assert.IsTrue(router2arrived);
                    }
                }
            }
        }

        [Test]
        public void AddSocketAfterRemovingTest()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                // we are using three responses to make sure we actually 
                //move the correct socket and other sockets still work
                using (var router = contex.CreateRouterSocket())
                using (var router2 = contex.CreateRouterSocket())
                using (var router3 = contex.CreateRouterSocket())
                {
                    router.Bind("tcp://127.0.0.1:5002");
                    router2.Bind("tcp://127.0.0.1:5003");
                    router3.Bind("tcp://127.0.0.1:5004");

                    using (var dealer = contex.CreateDealerSocket())
                    using (var dealer2 = contex.CreateDealerSocket())
                    using (var dealer3 = contex.CreateDealerSocket())
                    using (Poller poller = new Poller())
                    {
                        dealer.Connect("tcp://127.0.0.1:5002");
                        dealer2.Connect("tcp://127.0.0.1:5003");
                        dealer3.Connect("tcp://127.0.0.1:5004");

                        bool router1arrived = false;
                        bool router2arrived = false;
                        bool router3arrived = false;


                        bool more;

                        //The ReceiveReady event is raised by the Poller
                        router.ReceiveReady += (s, a) =>
                        {
                            router1arrived = true;

                            router.Receive(out more);
                            router.Receive(out more);

                            poller.RemoveSocket(router);

                        };

                        poller.AddSocket(router);

                        //The ReceiveReady event is raised by the Poller
                        router3.ReceiveReady += (s, a) =>
                        {
                            router3.Receive(out more);
                            router3.Receive(out more);
                            router3arrived = true;
                        };

                        //The ReceiveReady event is raised by the Poller
                        router2.ReceiveReady += (s, a) =>
                        {
                            router2arrived = true;
                            router2.Receive(out more);
                            router2.Receive(out more);

                            poller.AddSocket(router3);
                        };
                        poller.AddSocket(router2);

                        Task task = Task.Factory.StartNew(poller.Start);

                        dealer.Send("1");
                        Thread.Sleep(300);
                        dealer2.Send("2");
                        Thread.Sleep(300);
                        dealer3.Send("3");
                        Thread.Sleep(300);

                        poller.Stop(true);
                        task.Wait();

                        Assert.IsTrue(router1arrived);
                        Assert.IsTrue(router2arrived);
                        Assert.IsTrue(router3arrived);
                    }
                }
            }
        }

        [Test]
        public void AddTwoSocketAfterRemovingTest()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                // we are using three responses to make sure we actually 
                //move the correct socket and other sockets still work
                using (var router = contex.CreateRouterSocket())
                using (var router2 = contex.CreateRouterSocket())
                using (var router3 = contex.CreateRouterSocket())
                using (var router4 = contex.CreateRouterSocket())
                {
                    router.Bind("tcp://127.0.0.1:5002");
                    router2.Bind("tcp://127.0.0.1:5003");
                    router3.Bind("tcp://127.0.0.1:5004");
                    router4.Bind("tcp://127.0.0.1:5005");

                    using (var dealer = contex.CreateDealerSocket())
                    using (var dealer2 = contex.CreateDealerSocket())
                    using (var dealer3 = contex.CreateDealerSocket())
                    using (var dealer4 = contex.CreateDealerSocket())
                    using (Poller poller = new Poller())
                  
                    {
                        dealer.Connect("tcp://127.0.0.1:5002");
                        dealer2.Connect("tcp://127.0.0.1:5003");
                        dealer3.Connect("tcp://127.0.0.1:5004");
                        dealer4.Connect("tcp://127.0.0.1:5005");


                        int router1arrived = 0;
                        int router2arrived = 0;
                        bool router3arrived = false;
                        bool router4arrived = false;

                        bool more;

                        //The ReceiveReady event is raised by the Poller
                        router.ReceiveReady += (s, a) =>
                        {
                            router1arrived++;

                            router.Receive(out more);
                            router.Receive(out more);

                            poller.RemoveSocket(router);

                        };

                        poller.AddSocket(router);

                        //The ReceiveReady event is raised by the Poller
                        router3.ReceiveReady += (s, a) =>
                        {
                            router3.Receive(out more);
                            router3.Receive(out more);
                            router3arrived = true;
                        };

                        //The ReceiveReady event is raised by the Poller
                        router4.ReceiveReady += (s, a) =>
                        {
                            router4.Receive(out more);
                            router4.Receive(out more);
                            router4arrived = true;
                        };

                        //The ReceiveReady event is raised by the Poller
                        router2.ReceiveReady += (s, a) =>
                        {
                            router2arrived++;
                            router2.Receive(out more);
                            router2.Receive(out more);

                            if (router2arrived == 1)
                            {
                                poller.AddSocket(router3);

                                poller.AddSocket(router4);
                            }
                        };

                        poller.AddSocket(router2);

                        Task task = Task.Factory.StartNew(poller.Start);

                        dealer.Send("1");
                        Thread.Sleep(300);
                        dealer2.Send("2");
                        Thread.Sleep(300);
                        dealer3.Send("3");
                        dealer4.Send("4");
                        dealer2.Send("2");
                        dealer.Send("1");
                        Thread.Sleep(300);

                        poller.Stop(true);
                        task.Wait();

                        router.Receive(true, out more);

                        Assert.IsTrue(more);

                        router.Receive(true, out more);

                        Assert.IsFalse(more);

                        Assert.AreEqual(1, router1arrived);
                        Assert.AreEqual(2, router2arrived);
                        Assert.IsTrue(router3arrived);
                        Assert.IsTrue(router4arrived);
                    }
                }
            }
        }
        
        [Test]
        public void CancelSocketTest()
        {
            using (NetMQContext contex = NetMQContext.Create())
            {
                // we are using three responses to make sure we actually 
                //move the correct socket and other sockets still work
                using (var router = contex.CreateRouterSocket())
                using (var router2 = contex.CreateRouterSocket())
                using (var router3 = contex.CreateRouterSocket())
                {
                    router.Bind("tcp://127.0.0.1:5002");
                    router2.Bind("tcp://127.0.0.1:5003");
                    router3.Bind("tcp://127.0.0.1:5004");

                    using (var dealer = contex.CreateDealerSocket())
                    using (var dealer2 = contex.CreateDealerSocket())
                    using (var dealer3 = contex.CreateDealerSocket())
                    using (Poller poller = new Poller())
                    {
                        dealer.Connect("tcp://127.0.0.1:5002");
                        dealer2.Connect("tcp://127.0.0.1:5003");
                        dealer3.Connect("tcp://127.0.0.1:5004");

                        bool first = true;

                        //The ReceiveReady event is raised by the Poller
                        router2.ReceiveReady += (s, a) =>
                        {
                            bool more;

                            // identity
                            byte[] identity = a.Socket.Receive(out more);

                            // message
                            a.Socket.Receive(out more);

                            a.Socket.SendMore(identity);
                            a.Socket.Send("2");
                        };

                        poller.AddSocket(router2);

                        //The ReceiveReady event is raised by the Poller
                        router.ReceiveReady += (s, a) =>
                        {
                            if (!first)
                            {
                                Assert.Fail("This should happen because we cancelled the socket");
                            }
                            first = false;

                            bool more;

                            // identity
                            a.Socket.Receive(out more);

                            string m = a.Socket.ReceiveString(out more);

                            Assert.False(more);
                            Assert.AreEqual("Hello", m);

                            // cancelling the socket
                            poller.RemoveSocket(a.Socket);
                        };

                        poller.AddSocket(router);

                        //The ReceiveReady event is raised by the Poller
                        router3.ReceiveReady += (s, a) =>
                        {
                            bool more;

                            // identity
                            byte[] identity = a.Socket.Receive(out more);

                            // message
                            a.Socket.Receive(out more);

                            a.Socket.SendMore(identity).Send("3");
                        };

                        poller.AddSocket(router3);

                        Task pollerTask = Task.Factory.StartNew(poller.Start);

                        dealer.Send("Hello");

                        // sending this should not arrive on the poller, 
                        //therefore response for this will never arrive
                        dealer.Send("Hello2");

                        Thread.Sleep(100);

                        // sending this should not arrive on the poller, 
                        //therefore response for this will never arrive						
                        dealer.Send("Hello3");

                        Thread.Sleep(500);

                        bool more2;

                        // making sure the socket defined before the one cancelled still works
                        dealer2.Send("1");
                        string msg = dealer2.ReceiveString(out more2);
                        Assert.AreEqual("2", msg);

                        // making sure the socket defined after the one cancelled still works
                        dealer3.Send("1");
                        msg = dealer3.ReceiveString(out more2);
                        Assert.AreEqual("3", msg);

                        // we have to give this some time if we want to make sure 
                        //it's really not happening and it not only because of time
                        Thread.Sleep(300);

                        poller.Stop();

                        Thread.Sleep(100);
                        Assert.IsTrue(pollerTask.IsCompleted);
                    }
                }

            }
        }
    }
}
