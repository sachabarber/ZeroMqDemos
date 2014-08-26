using System;
using System.Threading;
using NetMQ;
using NUnit.Framework;

namespace SendMore
{
    [TestFixture]
    public class SendMoreTests
    {
        [Test]
        public void SendMoreTest()
        {
            using (NetMQContext ctx = NetMQContext.Create())
            {
                using (var server = ctx.CreateResponseSocket())
                {
                    server.Bind("tcp://127.0.0.1:5556");

                    using (var client = ctx.CreateRequestSocket())
                    {
                        client.Connect("tcp://127.0.0.1:5556");

                        //client send message
                        client.SendMore("A");
                        client.Send("Hello");

                        //server receive 1st part
                        bool more;
                        string m = server.ReceiveString(out more);
                        Assert.AreEqual("A", m);
                        Assert.IsTrue(more);

                        //server receive 2nd part
                        string m2 = server.ReceiveString(out more);
                        Assert.AreEqual("Hello", m2);
                        Assert.False(more);

                        //server send message, this time use NetMqMessage
                        //which will be sent as frames if the client calls
                        //ReceieveMessage()
                        var m3 = new NetMQMessage();
                        m3.Append("From");
                        m3.Append("Server");
                        server.SendMessage(m3);


                        //client receive
                        var m4 = client.ReceiveMessage();
                        Assert.AreEqual(2, m4.FrameCount);
                        Assert.AreEqual("From", m4[0].ConvertToString());
                        Assert.AreEqual("Server", m4[1].ConvertToString());


                    }
                }
            }
        }
    }
}
