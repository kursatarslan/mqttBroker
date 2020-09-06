using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FollowingVehicle
{
    internal class Program
    {
        private static IMqttClient mqttClient;
        private static IMqttClientOptions mqttOptions;
        private static string server;
        private static string username;
        private static string apiKey;
        private static string clientID;

        private static void Main(string[] args)
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var clientId = Guid.NewGuid().ToString();
            const string mqttUri = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;

            server = mqttUri;
            username = mqttUser;
            apiKey = mqttPassword;
            clientID = clientId;

            Console.WriteLine($"MQTT Server:{server} Username:{username} ClientID:{clientID}");

            // wolkabout formatted client state update topic
            var topicD2C = "platooning/message/followingvehicle";

            mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(server)
                .WithCredentials(username, apiKey)
                .WithClientId(clientID)
                //.WithTls()
                .Build();

            mqttClient.UseDisconnectedHandler(
                new MqttClientDisconnectedHandlerDelegate(e => MqttClient_Disconnected(e)));
            mqttClient.ConnectAsync(mqttOptions).Wait();

            while (true)
            {
                var payloadJObject = new JObject();

                var temperature = 22.0 + DateTime.UtcNow.Millisecond / 1000.0;
                var humidity = 50 + DateTime.UtcNow.Millisecond / 100.0;

                payloadJObject.Add("Temperature", temperature);
                payloadJObject.Add("Humidity", humidity);

                var payload = JsonConvert.SerializeObject(payloadJObject);
                Console.WriteLine($"Topic:{topicD2C} Payload:{payload}");

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topicD2C)
                    .WithPayload(payload)
                    .WithAtLeastOnceQoS()
                    .Build();

                // the code that you want to measure comes here

                Console.WriteLine("PublishAsync start");
                var watch = Stopwatch.StartNew();
                mqttClient.PublishAsync(message).Wait();
                watch.Stop();
                Console.WriteLine("Time taken: {0}ms", watch.Elapsed.TotalMilliseconds);


                Thread.Sleep(110);
            }
        }

        private static async void MqttClient_Disconnected(MqttClientDisconnectedEventArgs e)
        {
            Debug.WriteLine("Disconnected");
            await Task.Delay(TimeSpan.FromSeconds(5));

            try
            {
                await mqttClient.ConnectAsync(mqttOptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Reconnect failed {0}", ex.Message);
            }
        }
    }
}