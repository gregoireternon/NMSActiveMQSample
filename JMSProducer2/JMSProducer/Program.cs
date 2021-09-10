using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using JMSProducer;
using System;
using System.Threading;

namespace JMSProducer2
{
    class Program
    {
        static Thread replyThread = null;
        static IDestination replyQueue;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string ip = "tcp://localhost:61616";
            Uri providerUri = new Uri(ip);
            IConnection conn = null;
            IConnectionFactory factory = new ConnectionFactory(ip);

            StartReplyThread();


            using (conn = factory.CreateConnection())
            {
                using(ISession ses = conn.CreateSession())
                {
                    

                    IMessageProducer prod = ses.CreateProducer(new ActiveMQTopic("myTopic.paris"));
                    int i= 1;
                    while (true)
                    {
                        i = (i + 1) % 10;
                        Thread.Sleep(400);
                        string message = "paris" + DateTime.Now.ToString();
                        Console.WriteLine("Sending message : " + message);
                        IMessage m = prod.CreateObjectMessage(new Entity()
                        {
                            Nom = "TT",
                            Prenom = message
                        });
                        //((ActiveMQObjectMessage)m).Formatter = new JSonFormatter
                        Console.WriteLine("canal : " + i);
                        m.Properties.SetInt("Canal", i);
                        m.NMSReplyTo=replyQueue;
                        //m.NMSCorrelationID = Guid.NewGuid().ToString();
                        prod.Send(m, MsgDeliveryMode.Persistent, MsgPriority.Normal, TimeSpan.FromSeconds(3600));

                        IMessage myMessage = prod.CreateTextMessage(message);
                        prod.Send(myMessage,MsgDeliveryMode.Persistent, MsgPriority.Normal,TimeSpan.FromSeconds(3600));

                    }

                }
            }
                
        }

        private static void StartReplyThread()
        {
            replyThread = new Thread(() =>
            {
                string ip = "tcp://localhost:61616";
                Uri providerUri = new Uri(ip);
                IConnection conn = null;
                IConnectionFactory factory = new ConnectionFactory(ip);
                
                using (conn = factory.CreateConnection())
                {
                    
                    conn.Start();
                    using (ISession ses = conn.CreateSession())
                    {
                        replyQueue = new ActiveMQQueue("prod1ReplyQueue");
                        //replyQueue = ses.CreateTemporaryQueue();
                        IMessageConsumer replyConsumer = ses.CreateConsumer(replyQueue);
                        //replyConsumer.Listener += new MessageListener(ReplyReceived);
                        while (true)
                        {
                            IMessage response = replyConsumer.Receive();
                            Console.WriteLine("Reply Receive: " + ((ITextMessage)response).Text);

                        }
                    }

                }
                Console.WriteLine("Reply Dest connection closed");

            });

            replyThread.Start();
        }

        private static void ReplyReceived(IMessage message)
        {
            Console.WriteLine("Reply Receive: " + ((ITextMessage)message).Text);
        }
    }

}


