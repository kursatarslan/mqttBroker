﻿using System;
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
        private const string platoonId = "platoon1";


        public static IManagedMqttClient client =
            new MqttFactory().CreateManagedMqttClient(new MqttNetLogger("MyCustomId"));

        private static void Main(string[] args)
        {
            
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
                        _ = PublishAsync("platooning/message/" + leadvehicle + "/" + platoonId,
                            Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                        Console.WriteLine("Client Publish as  " + "platooning/message/" + leadvehicle + "  payload => " +
                                          Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
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

        

        private static async Task ConnectAsync()
        {
            const string mqttUri = "localhost";
            var mqttUser = "test";
            var mqttPassword = "test";
            var mqttPort = 1883;
            Console.WriteLine($"MQTT Server:{mqttUri} Username:{mqttUser} ClientID:{leadvehicle}");
            var messageBuilder = new MqttClientOptionsBuilder()
                .WithClientId(leadvehicle)
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
                Console.WriteLine("Disconnected from MQTT Brokers.Client Was Connected " + e.ClientWasConnected);
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
                        var py = HelperFunctions.ToBitString(new BitArray(e.ApplicationMessage.Payload), 0, 61);
                        Console.WriteLine($"Topic: {topic}. Message Received: {py}");
                        var followingVehicle = topic.Replace("platooning/" + leadvehicle + "/", "");
                        if (payload.Maneuver == 1 && !string.IsNullOrWhiteSpace(followingVehicle))
                        {
                            payload.Maneuver = 2;
                            var message = new BitArray(61);
                            message.Set(0, false);
                            message.Set(1, true);
                            message.Set(2, false);
                            _ = PublishAsync("platooning/" + followingVehicle+"/" + leadvehicle+"/"+ platoonId,  Encoding.ASCII.GetString(HelperFunctions.BitArrayToByteArray(message)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message, ex);
                }
            });
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