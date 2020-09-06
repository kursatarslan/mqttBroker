using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MqttBroker
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services, new DbContextOptionsBuilder());
            var mqttConsumer = new MqttConsumer();
            mqttConsumer.DataReceived += MqttConsumer_DataReceived;
            mqttConsumer.StartConsume();
            Console.ReadLine();
        }

        private static void ConfigureServices(IServiceCollection services, DbContextOptionsBuilder optionsBuilder)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .CreateLogger();
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) //location of the exe file
                .AddJsonFile("appsettings.json", true, true);

            var configuration = builder.Build();

            var connectionstring = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<MqttBrokerDbContext>(options => options.UseNpgsql(connectionstring));
        }

        private static void MqttConsumer_DataReceived(object sender, byte[] e)
        {
            Console.WriteLine("Data received ===>" + sender + " byte" + e);
        }
    }
}