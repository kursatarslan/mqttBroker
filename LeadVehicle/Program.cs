using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Diagnostics;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

namespace LeadVehicle
{
    internal class Program
    {
        private const string leadvehicle = "leadvehicle";


        public static IManagedMqttClient client =
            new MqttFactory().CreateManagedMqttClient(new MqttNetLogger("MyCustomId"));

        private static void Main(string[] args)
        {
            Console.WriteLine("Client console ");
            _ = ConnectAsync();
            do {
                while (!Console.KeyAvailable) {
                    if (Console.ReadKey(true).Key == ConsoleKey.S)
                    {
                        _ = SubscribeAsync("platooning/" + leadvehicle + "/#");
                        Console.WriteLine("Client SubscribeAsync as  " + "platooning/" + leadvehicle + "/#");
                    }else if (Console.ReadKey(true).Key == ConsoleKey.P)
                    {
                        var message = new BitArray(61);
                        message.Set(0, true);
                        message.Set(1, true);
                        message.Set(2, true);
                        //string message = HelperFunctions.RandomString(5,true);
                        _ = PublishAsync("platooning/message/" + leadvehicle,
                            Encoding.ASCII.GetString(BitArrayToByteArray(message)));
                        Console.WriteLine("Client Publish as  " + "platooning/message/" + leadvehicle + "  payload => " +
                                          Encoding.ASCII.GetString(BitArrayToByteArray(message)));
                    }
                }       
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
        }
        //public event EventHandler<MqttClientConnectedEventArgs> Connected;
        //public event EventHandler<MqttClientDisconnectedEventArgs> Disconnected;
        //public event EventHandler<MqttApplicationMessageReceivedEventArgs> MessageReceived;

        private static byte[] GenerateMessage()
        {
            //0x111 
            return new byte[4];
        }

        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            var ret = new byte[(bits.Length - 1) / 8 + 1];
            bits.CopyTo(ret, 0);
            return ret;
        }

        private static async Task ConnectAsync()
        {
            var clientId = "leadVehicle";
            const string mqttUri = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;

            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithCredentials(mqttUser, mqttPassword)
                .WithTcpServer(mqttUri, mqttPort)
                .WithKeepAlivePeriod(new TimeSpan(0, 0, 30))
                .WithCleanSession();

            var options = messageBuilder
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(options)
                .Build();

            await client.StartAsync(managedOptions);

            client.UseConnectedHandler(e => { Console.WriteLine("Connected successfully with MQTT Brokers."); });
            client.UseApplicationMessageReceivedHandler(e =>
            {
                Console.WriteLine("Connected UseApplicationMessageReceivedHandler with MQTT Brokers." + e.ApplicationMessage);
            });

            client.UseDisconnectedHandler(e =>
            {
                new MqttClientDisconnectedHandlerDelegate(e => MqttClient_Disconnected(e));
                Console.WriteLine(" Client Was Connected " + e.ClientWasConnected);
                Console.WriteLine("Disconnected from MQTT Brokers.");
            });
            client.UseApplicationMessageReceivedHandler(e =>
            {
                try
                {
                    var topic = e.ApplicationMessage.Topic;

                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        var stringpayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                        var bitArray = new BitArray(e.ApplicationMessage.Payload);
                        var payload = HelperFunctions.GetPayload(e.ApplicationMessage.Payload);
                        Console.WriteLine($"Topic: {topic}. Message Received: {payload}");
                        var followingVehicle = topic.Replace("platooning/" + leadvehicle + "/", "");
                        if (payload.Maneuver == 1 && !string.IsNullOrWhiteSpace(followingVehicle))
                        {
                            payload.Maneuver = 2;
                            var myBA = new BitArray(3);
                            var message = ToBitString(myBA, 0, 3);
                            myBA.Set(0, false);
                            myBA.Set(0, true);
                            myBA.Set(0, false);
                            _ = PublishAsync("platooning/" + followingVehicle, message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
            });
        }

        public static string ToBitString(BitArray bits, int indexStart, int indexFinish)
        {
            var sb = new StringBuilder();

            for (var i = indexStart; i < indexFinish; i++)
            {
                var c = bits[i] ? '1' : '0';
                sb.Append(c);
            }

            return sb.ToString();
        }

        private static async void MqttClient_Disconnected(MqttClientDisconnectedEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            try
            {
                await ConnectAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reconnect failed {0}", ex.Message);
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

        public static async Task SubscribeAsync(string topic, int qos = 1)
        {
            await client.SubscribeAsync(new TopicFilterBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel) qos)
                .Build());
        }
    }
}