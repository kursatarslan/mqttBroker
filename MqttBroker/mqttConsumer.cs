using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MqttBroker.Enums;
using MqttBroker.Models;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;

namespace MqttBroker
{
    public class MqttConsumer
    {
        private readonly IMqttServerOptions _optionsBuilder;


        private readonly IMqttServer _server;

        public MqttConsumer()
        {
            /*
            MqttServerOptions validator = new MqttServerOptions();
            validator.ConnectionValidator = new IMqttServerConnectionValidator(
            {

                c.ReturnCode = MqttConnectReturnCode.ConnectionAccepted;
                Console.WriteLine("Connexion OK");
            }


            MqttServerOptions subValidator = new MqttServerOptions();
            subValidator.SubscriptionInterceptor = context =>
            {
                context.AcceptSubscription = true;
                Console.WriteLine("Subscribe OK");

            };

            */

            //var certificate = new X509Certificate(@"C:\Users\StreamX\Desktop\TestCa.crt", "");
            //MqttServerOptions certifOption = new MqttServerOptions();
            //certifOption.TlsEndpointOptions.Certificate = certificate.Export(X509ContentType.Cert);
            //certifOption.TlsEndpointOptions.IsEnabled = true;
            var conUserValidator = new MqttServerOptions
            {
                ConnectionValidator = new MqttServerConnectionValidatorDelegate(p =>
                {
                    //if (p.ClientId != "SpecialClient") return;
                    if (p.Username != "test" || p.Password != "test")
                        p.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
                })
                /*
                ApplicationMessageInterceptor = new MqttServerApplicationMessageInterceptorDelegate(context =>
                {
                    if (!MqttTopicFilterComparer.IsMatch(context.ApplicationMessage.Topic, "platooning"))
                    {
                        // Replace the payload with the timestamp. But also extending a JSON 
                        // based payload with the timestamp is a suitable use case.
                        context.ApplicationMessage.Payload = Encoding.UTF8.GetBytes(DateTime.Now.ToString("O"));
                    }
 
                    if (context.ApplicationMessage.Topic == "not_allowed_topic")
                    {
                        context.AcceptPublish = false;
                        context.CloseConnection = true;
                    }
                }),
                SubscriptionInterceptor = new MqttServerSubscriptionInterceptorDelegate(context =>
                {
                    if (context.TopicFilter.Topic.StartsWith("admin/foo/bar") && context.ClientId != "theAdmin")
                    {
                        //context.AcceptSubscription = false;
                    }
 
                    if (context.TopicFilter.Topic.StartsWith("the/secret/stuff") && context.ClientId != "Imperator")
                    {
                        //context.AcceptSubscription = false;
                        //context.CloseConnection = true;
                    }
                })*/
            };

            _optionsBuilder = new MqttServerOptionsBuilder()
                .WithClientCertificate()
                .WithConnectionBacklog(100)
                .WithDefaultEndpointPort(1883)
                .WithConnectionValidator(conUserValidator.ConnectionValidator)
                .WithApplicationMessageInterceptor(conUserValidator.ApplicationMessageInterceptor)
                .WithSubscriptionInterceptor(conUserValidator.SubscriptionInterceptor)
                .WithPersistentSessions()
                // .WithEncryptionCertificate(certifOption.TlsEndpointOptions.Certificate)
                //.WithEncryptedEndpoint()
                .Build();
            _server = new MqttFactory().CreateMqttServer();
        }

        public event EventHandler<byte[]> DataReceived;

        public async Task StartConsume()
        {
            try
            {
                await _server.StartAsync(_optionsBuilder);
            }
            catch (Exception ex)
            {
                StopConsume();
                Console.WriteLine(ex.Message);
                throw;
            }
            _server.StartedHandler = new MqttServerStartedHandlerDelegate(e =>
            {
                Console.WriteLine("Mqtt Broker start");
            });
            _server.StoppedHandler = new MqttServerStoppedHandlerDelegate(e =>
            {
                Console.WriteLine("Mqtt Broker stop");
            });
            _server.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(e =>
            {
                var vehicleId = e.TopicFilter.Topic
                    .Replace("platooning/", "").Replace("/#", "");

                Console.WriteLine("Client subscribed " + e.ClientId + " topic " + e.TopicFilter.Topic + "Vehicle Id " +
                                  vehicleId);
                using var context = new MqttBrokerDbContext();
                try
                {
                    var audit = new Audit
                    {
                        ClientId = e.ClientId,
                        Type = "Sub",
                        Topic = e.TopicFilter.Topic,
                        Payload = JsonConvert.SerializeObject(e.TopicFilter, Formatting.Indented)
                    };
                    context.Audit.AddAsync(audit);
                    var subs = context.Subscribe.AsQueryable()
                        .FirstOrDefault(s => s.Topic == e.TopicFilter.Topic
                                             && s.ClientId == e.ClientId
                                             && s.QoS == e.TopicFilter.QualityOfServiceLevel.ToString());
                    if (subs != null) return;
                    var subClient = new Subscribe
                    {
                        Topic = e.TopicFilter.Topic,
                        Enable = true,
                        ClientId = e.ClientId,
                        QoS = e.TopicFilter.QualityOfServiceLevel.ToString()
                    };
                    context.Subscribe.AddAsync(subClient);
                    context.SaveChanges();
                }
                catch (Exception exception)
                {
                    var log = new Log
                    {
                        Exception = exception.StackTrace
                    };
                    context.Log.AddAsync(log);
                    context.SaveChanges();
                    Console.WriteLine(exception);
                }
            });

            _server.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(args =>
            {
                try
                {
                    var clientId = args.ClientId;
                    var topicFilter = args.TopicFilter;

                    Console.WriteLine($"[{DateTime.Now}] Client '{clientId}' un-subscribed to {topicFilter}.");
                    using var context = new MqttBrokerDbContext();
                    try
                    {
                        var sub = context.Subscribe.AsQueryable()
                            .FirstOrDefault(a => a.Topic == args.TopicFilter && a.ClientId == clientId);
                        if (sub == null) return;
                        context.Subscribe.Update(sub!);
                        context.SaveChanges();
                        sub.Enable = false;
                    }
                    catch (Exception exception)
                    {
                        var log = new Log
                        {
                            Exception = exception.StackTrace
                        };
                        context.Log.AddAsync(log);
                        context.SaveChanges();
                        Console.WriteLine(exception);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now}] Client get error " + ex.Message);
                }
            });

            _server.ClientConnectedHandler = new MqttServerClientConnectedHandlerDelegate(e =>
            {
                Console.WriteLine("Client Connected " + e.ClientId);
            });
            _server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(e =>
            {
                using var context = new MqttBrokerDbContext();
                try
                {
                    var payload = FunctionHelpers.GetPayload(e.ApplicationMessage.Payload);
                    var audit = new Audit
                    {
                        ClientId = e.ClientId,
                        Type = "Pub",
                        Topic = e.ApplicationMessage.Topic,
                        Payload = JsonConvert.SerializeObject(payload, Formatting.Indented)
                    };
                    context.Audit.AddAsync(audit);
                    if (e.ClientId == null)
                    {
                        var log = new Log
                        {
                            Exception = new string("Broker publish message itself " +
                                                   JsonConvert.SerializeObject(payload, Formatting.Indented) + " " +
                                                   e.ClientId)
                        };
                        context.Log.AddAsync(log);
                        context.SaveChanges();
                        return;
                    }

                    if (payload.Maneuver == Maneuver.CreatePlatoon)
                    {
                        var vehPla = e.ApplicationMessage.Topic.Replace("platooning/message/", "");
                        var platoonId = vehPla.Split("/").Last();
                        var pla = context.Platoon.AsQueryable()
                            .FirstOrDefault(f => f.Enable && f.PlatoonRealId == platoonId);
                        if (pla == null)
                        {
                            var platoon = new Platoon()
                            {
                                Enable = true,
                                ClientId = e.ClientId,
                                IsLead = true,
                                VechicleId = vehPla.Split("/").First(),
                                PlatoonRealId = vehPla.Split("/").Last()
                            };
                            context.Platoon.AddAsync(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Creating new Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload "  + audit.Payload);
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now}] Platoon is already created Client Id " + e.ClientId +
                                              " platooning Id" + platoonId + " payload "  + audit.Payload);
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinRequest)
                    {
                        var followingVec = e.ApplicationMessage.Topic.Replace("platooning/message/", "");
                        var platoonLead = context.Platoon.AsQueryable().FirstOrDefault(f => f.IsLead && f.Enable);
                        if (platoonLead != null)
                        {
                            var platoon = new Platoon()
                            {
                                Enable = false,
                                ClientId = e.ClientId,
                                IsLead = false,
                                IsFollower = true,
                                VechicleId = followingVec,
                                PlatoonRealId = platoonLead.PlatoonRealId
                            };
                            context.Platoon.AddAsync(platoon);
                            Console.WriteLine($"[{DateTime.Now}] Join Platoon Client Id " + e.ClientId +
                                              " platooning Id" + platoon.PlatoonRealId + " payload "  + audit.Payload);
                            var message = new BitArray(61);
                            message.Set(0, false);
                            message.Set(1, false);
                            message.Set(2, true);

                            _server.PublishAsync("platooning/" + platoonLead.ClientId + "/" + followingVec + "/" + platoonLead.ClientId,
                                Encoding.ASCII.GetString(FunctionHelpers.BitArrayToByteArray(message)));
                        }
                    }
                    else if (payload.Maneuver == Maneuver.JoinAccepted)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Join accepted Client Id " + e.ClientId + " payload " +
                                          audit.Payload);
                        var followvehicleId =
                            e.ApplicationMessage.Topic.Replace("platooning/" + e.ClientId + "/", "");

                        var platoonfollow = context.Platoon.AsQueryable()
                            .FirstOrDefault(f => f.IsFollower && f.ClientId == followvehicleId);

                        if (platoonfollow != null)
                        {
                            platoonfollow.Enable = true;
                            context.Platoon.Update(platoonfollow);
                        }
                        else
                        {
                            var platoonlead = context.Platoon.AsQueryable()
                                .FirstOrDefault(f => f.IsLead && f.Enable);
                            if (platoonlead != null)
                            {
                                var platoon = new Platoon()
                                {
                                    Enable = true,
                                    ClientId = e.ClientId,
                                    IsLead = false,
                                    IsFollower = true,
                                    VechicleId = followvehicleId,
                                    PlatoonRealId = platoonlead.PlatoonRealId
                                };
                                context.Platoon.AddAsync(platoon);
                            }
                        }
                    }
                    else
                    {
                        var log = new Log
                        {
                            Exception = new string("Unknown Maneuver " +
                                                   JsonConvert.SerializeObject(payload, Formatting.Indented) + " " +
                                                   e.ClientId)
                        };
                        context.Log.AddAsync(log);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    var log = new Log
                    {
                        Exception = exception.StackTrace
                    };
                    context.Log.AddAsync(log);
                    context.SaveChanges();
                }

                context.SaveChanges();
                OnDataReceived(e.ApplicationMessage.Payload);
                //Console.WriteLine("Message Received");
                //Console.WriteLine(e.ClientId + " " + e.ApplicationMessage.Topic);
                //Console.WriteLine(e.ClientId + " " + e.ApplicationMessage.ConvertPayloadToString());
            });
        }


        private void StopConsume()
        {
            _server.StopAsync();
            _server.ClientDisconnectedHandler = new MqttServerClientDisconnectedHandlerDelegate(e =>
            {
                Console.WriteLine("Client Disconnect" + e.ClientId);
                Console.WriteLine("Client DisconnectType" + e.DisconnectType);
            });
        }

        protected virtual void OnDataReceived(byte[] e)
        {
            DataReceived?.Invoke(this, e);
        }
    }
}