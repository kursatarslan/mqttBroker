using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace MqttClientTest
{
    internal class Program
    {
        private static readonly IManagedMqttClient client = new MqttFactory().CreateManagedMqttClient();

        private static void Main(string[] args)
        {
            Console.WriteLine("Client Console ");
            ConnectAsync();
            while (true)
            {
                var message = Console.ReadLine();
                PublishAsync("sayhello", message);
            }
        }

        public static async Task ConnectAsync()
        {
            var clientId = Guid.NewGuid().ToString();
            var mqttURI = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;

            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithCredentials(mqttUser, mqttPassword)
                .WithTcpServer(mqttURI, mqttPort)
                .WithCleanSession();

            var options = messageBuilder
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            await client.StartAsync(managedOptions);

            client.UseConnectedHandler(e => { Console.WriteLine("Connected successfully with MQTT Brokers."); });

            client.UseDisconnectedHandler(e => { Console.WriteLine("Disconnected from MQTT Brokers."); });
        }

        private static void Handler(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                if (string.IsNullOrWhiteSpace(topic) == false)
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, ex);
            }
        }

        public static async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .WithRetainFlag(retainFlag)
                .Build());
        }

        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }
    }
}