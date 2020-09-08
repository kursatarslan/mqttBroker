using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using Newtonsoft.Json;

namespace FollowingVehicle
{
    internal class Program
    {
        private static IMqttClient mqttClient;
        private static IMqttClientOptions mqttOptions;
        private static string server;
        private static string username;
        private static string apiKey;
        private static string clientId;

        private static void Main(string[] args)
        {
            var factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            clientId = "followingVehicle";
            const string mqttUri = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;

            server = mqttUri;
            username = mqttUser;
            apiKey = mqttPassword;

            Console.WriteLine($"MQTT Server:{server} Username:{username} ClientID:{clientId}");

            // wolkabout formatted client state update topic
            var topicD2C = "platooning/message/followingvehicle";

            mqttOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(server)
                .WithCredentials(username, apiKey)
                .WithClientId(clientId)
                //.WithTls()
                .Build();
            mqttClient.ConnectAsync(mqttOptions).Wait();
            mqttClient.UseConnectedHandler(e => { Console.WriteLine("Connected successfully with MQTT Brokers."); });
            mqttClient.UseDisconnectedHandler(e =>
            {
                new MqttClientDisconnectedHandlerDelegate(e => MqttClient_Disconnected(e));
                Console.WriteLine("Disconnected from MQTT Brokers.Client Was Connected " + e.ClientWasConnected);
            });
            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;

                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
                        Console.WriteLine($"Topic: {topic}. Message Received: {JsonConvert.SerializeObject(payload, Formatting.Indented)}");
                        var platoonId = topic.Replace("platooning/" + clientId + "/", "").Split("/").Last();
                        if (payload.Maneuver == 2 )
                        { 
                            
                            _ = SubscribeAsync("platooning/broadcast/" + platoonId + "/#");
                            Console.WriteLine("Client SubscribeAsync as  " + "platooning/broadcast/" + platoonId + "/#");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
            });
            
            do {
                while (!Console.KeyAvailable) {
                    if (Console.ReadKey(true).Key == ConsoleKey.S)
                    {
                        _ = SubscribeAsync("platooning/" + clientId + "/#");
                        Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + clientId + "/#");
                    }else if (Console.ReadKey(true).Key == ConsoleKey.P)
                    {
                        var message = new BitArray(61);
                        message.Set(0, false);
                        message.Set(1, false);
                        message.Set(2, true);
                        //string message = HelperFunctions.RandomString(5,true);
                        _ = PublishAsync("platooning/message/" + clientId ,
                            Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                        Console.WriteLine("Client Publish as  " + "platooning/message/" + clientId + "  payload => " +
                                          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                    }
                }       
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
        
        public static async Task PublishAsync(string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .WithRetainFlag(retainFlag)
                .Build());
        }

        public static async Task SubscribeAsync(string topic, int qos = 1)
        {
            await mqttClient.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .Build());
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