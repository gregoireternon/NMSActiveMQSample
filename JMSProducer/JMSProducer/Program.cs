using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System;
using System.Threading;

namespace JMSProducer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string ip = "tcp://localhost:61616";
            Uri providerUri = new Uri(ip);
            IConnection conn = null;
            IConnectionFactory factory = new ConnectionFactory(ip);
            using (conn = factory.CreateConnection())
            {
                using(ISession ses = conn.CreateSession())
                {
                    

                    IMessageProducer prod = ses.CreateProducer(new ActiveMQTopic("myTopic"));

                    while (true)
                    {
                        Thread.Sleep(2000);
                        string message = "coucou" + DateTime.Now.ToString();
                        Console.WriteLine("Sending message : " + message);
                        IMessage myMessage = prod.CreateTextMessage(message);
                        prod.Send(myMessage,MsgDeliveryMode.Persistent, MsgPriority.Normal,TimeSpan.FromSeconds(3600));

                    }

                }
            }
                
        }
    }
}
