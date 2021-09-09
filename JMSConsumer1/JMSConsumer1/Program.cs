using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Apache.NMS.ActiveMQ.Commands;
using System;
using System.Text.Json;
namespace JMSConsumer1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            string clientId = "customer1";

            IConnectionFactory factory = new    ConnectionFactory("tcp://localhost:61616/", clientId);

   //Create the connection

            using (IConnection connection =  factory.CreateConnection())

            {


                connection.Start();

                //Create the Session

                using (ISession session = connection.CreateSession())

                {

                    //Create the Consumer

                    IMessageConsumer consumer = session.CreateDurableConsumer(new ActiveMQTopic("myTopic"),clientId, null,false);

                    consumer.Listener += new MessageListener(

                       consumer_Listener);

                    Console.ReadLine();

                }

            }

        }

        static void consumer_Listener(IMessage message)

        {
            if (message is ITextMessage)
            {
                Console.WriteLine("Receive: " + ((ITextMessage)message).Text);

            }
            else
            {
                Console.WriteLine("Receive: " + JsonSerializer.Serialize(((ActiveMQObjectMessage)message).Body));
            }

        }

    }

    [Serializable]
    public class Entity
    {
        public string Nom { get; set; }
        public string Prenom { get; set; }

    }
}
