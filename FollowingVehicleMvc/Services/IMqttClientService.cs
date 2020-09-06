using Microsoft.Extensions.Hosting;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Receiving;

namespace FollowingVehicleMvc.Services
{
    public interface IMqttClientService : IHostedService,
        IMqttClientConnectedHandler,
        IMqttClientDisconnectedHandler,
        IMqttApplicationMessageReceivedHandler
    {
    }
}