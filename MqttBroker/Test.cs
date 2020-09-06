using System;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;

namespace MqttBroker
{
    public class MQTTService
    {
        private static ILogger<MQTTService> mqttServiceLogger;

        private readonly MQTTConfiguration mqttConfiguration;


        /// <summary>
        ///     TODO: Implement client connection validator here
        /// </summary>
        private readonly Action<MqttConnectionValidatorContext> MQTTConnectionValidator = c =>
        {
            LogMessage(mqttServiceLogger, c);
        };

        private IMqttServer mqttServer;

        private IMqttServerOptions mqttServerOptions;


        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="logger"></param>
        public MQTTService(ILogger<MQTTService> logger)
        {
            mqttServiceLogger = logger;

            mqttConfiguration = new MQTTConfiguration
            {
                BrokerHostName = "127.0.0.1",
                BrokerPort = 1883,
                MqttSslProtocol = SslProtocols.None,
                UseSSL = false
            };
            BuildServerOptions();
            CreateMqttServer();
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await StartMqttServerAsync();
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                mqttServiceLogger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }


        private void BuildServerOptions()
        {
            try
            {
                var ipAddress = IPAddress.Parse(mqttConfiguration.BrokerHostName);
                var optionsBuilder = new MqttServerOptionsBuilder();

                if (mqttConfiguration.UseSSL)
                    //// TODO: Implement insert certification
                    optionsBuilder.WithClientCertificate()
                        .WithEncryptionSslProtocol(mqttConfiguration.MqttSslProtocol);

                optionsBuilder.WithDefaultEndpointBoundIPAddress(ipAddress)
                    .WithDefaultEndpointPort(mqttConfiguration.BrokerPort)
                    .WithConnectionValidator(MQTTConnectionValidator)
                    .WithSubscriptionInterceptor(c =>
                    {
                        c.AcceptSubscription = true;
                        LogMessage(mqttServiceLogger, c, true);
                    })
                    .WithApplicationMessageInterceptor(c =>
                    {
                        c.AcceptPublish = true;
                        LogMessage(mqttServiceLogger, c);
                    });

                mqttServerOptions = optionsBuilder.Build();
            }
            catch (Exception ex)
            {
                mqttServiceLogger.LogError(ex.Message);
                throw;
            }
        }

        private void CreateMqttServer()
        {
            try
            {
                mqttServer = new MqttFactory().CreateMqttServer();

                //// Add handlers for server
                mqttServer.UseClientConnectedHandler(ClientConnectedHandler);
                mqttServer.UseClientDisconnectedHandler(ClientDisconnectedHandler);

                mqttServer.ClientSubscribedTopicHandler = new MqttServerClientSubscribedHandlerDelegate(args =>
                {
                    try
                    {
                        var clientId = args.ClientId;
                        var topicFilter = args.TopicFilter;
                        mqttServiceLogger.LogInformation(
                            $"[{DateTime.Now}] Client '{clientId}' subscribed to {topicFilter.Topic} {topicFilter.QualityOfServiceLevel}.");
                    }
                    catch (Exception ex)
                    {
                        mqttServiceLogger.LogError(ex.Message);
                    }
                });

                mqttServer.ClientUnsubscribedTopicHandler = new MqttServerClientUnsubscribedTopicHandlerDelegate(args =>
                {
                    try
                    {
                        var clientID = args.ClientId;
                        var topicFilter = args.TopicFilter;

                        mqttServiceLogger.LogInformation(
                            $"[{DateTime.Now}] Client '{clientID}' un-subscribed to {topicFilter}.");
                    }
                    catch (Exception ex)
                    {
                        mqttServiceLogger.LogError(ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                mqttServiceLogger.LogError(ex.Message);
                throw;
            }
        }

        private async Task StartMqttServerAsync()
        {
            try
            {
                if (mqttServerOptions == null) throw new ArgumentNullException(nameof(mqttServerOptions));

                await mqttServer.StartAsync(mqttServerOptions);
            }
            catch (Exception ex)
            {
                mqttServiceLogger.LogError(ex.Message);
                throw;
            }
        }


        public static void ClientConnectedHandler(MqttServerClientConnectedEventArgs args)
        {
            try
            {
                var clientID = args.ClientId;
            }
            catch (Exception ex)
            {
                mqttServiceLogger.LogError(ex.Message);
            }
        }

        public static void ClientDisconnectedHandler(MqttServerClientDisconnectedEventArgs args)
        {
            try
            {
                var clientID = args.ClientId;
                var mqttClientDisconnectType = args.DisconnectType;
            }
            catch (Exception ex)
            {
                mqttServiceLogger.LogError(ex.Message);
            }
        }

        /// <summary>
        ///     Logs the message from the MQTT subscription interceptor context.
        /// </summary>
        /// <param name="context">The MQTT subscription interceptor context.</param>
        /// <param name="successful">A <see cref="bool" /> value indicating whether the subscription was successful or not.</param>
        private static void LogMessage(ILogger<MQTTService> logger, MqttSubscriptionInterceptorContext context,
            bool successful)
        {
            if (context == null) return;

            logger.LogInformation(successful
                ? $"New subscription: ClientId = {context.ClientId}, TopicFilter = {context.TopicFilter}"
                : $"Subscription failed for clientId = {context.ClientId}, TopicFilter = {context.TopicFilter}");
        }

        /// <summary>
        ///     Logs the message from the MQTT message interceptor context.
        /// </summary>
        /// <param name="context">The MQTT message interceptor context.</param>
        private static void LogMessage(ILogger<MQTTService> logger, MqttApplicationMessageInterceptorContext context)
        {
            if (context == null) return;

            var payload = context.ApplicationMessage?.Payload == null
                ? null
                : Encoding.UTF8.GetString(context.ApplicationMessage?.Payload);

            logger.LogInformation(
                $"Message: ClientId = {context.ClientId}, Topic = {context.ApplicationMessage?.Topic},"
                + $" Payload = {payload}, QoS = {context.ApplicationMessage?.QualityOfServiceLevel},"
                + $" Retain-Flag = {context.ApplicationMessage?.Retain}");
        }

        /// <summary>
        ///     Logs the message from the MQTT connection validation context.
        /// </summary>
        /// <param name="context">The MQTT connection validation context.</param>
        private static void LogMessage(ILogger<MQTTService> logger, MqttConnectionValidatorContext context)
        {
            if (context == null) return;

            logger.LogInformation(
                $"New connection: ClientId = {context.ClientId}, Endpoint = {context.Endpoint},"
                + $" Username = {context.Username}, CleanSession = {context.CleanSession}");
        }
    }


    internal class MQTTConfiguration
    {
        public string BrokerHostName { get; set; }
        public bool UseSSL { get; set; }
        public SslProtocols MqttSslProtocol { get; set; }
        public int BrokerPort { get; set; } = 1883;
    }
}