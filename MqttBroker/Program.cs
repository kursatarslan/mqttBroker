using System;
using System.Collections;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MqttBroker
{
    internal class Program
    {
        public static IConfiguration Configuration;
        public static ILoggerFactory LoggerFactory;
        private static void Main(string[] args)
        {
            var services = new ServiceCollection();
            ConfigureServices(services, new DbContextOptionsBuilder());
            //var mqttConsumer = new MqttConsumer();
            //mqttConsumer.DataReceived += MqttConsumer_DataReceived;
            //mqttConsumer.StartConsume();
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

            Configuration = builder.Build();

            var connectionstring = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<MqttBrokerDbContext>(options => options.UseNpgsql(connectionstring));
            services.AddSingleton<MqttConsumer>();
            var serviceProvider = services.BuildServiceProvider();
            var mqttBroker = serviceProvider.GetService<MqttConsumer>();
            mqttBroker.StartConsume();
            mqttBroker.DataReceived += MqttConsumer_DataReceived;
        }

        private static void MqttConsumer_DataReceived(object sender, byte[] e)
        {
            var bitarry = new BitArray(e);
            Console.WriteLine("Data received ===>" + FunctionHelpers.ToBitString(bitarry,0,bitarry.Length) );
        }
    }
    
    class LoggingConfig
    {
        public string LogPath {get;set;}
        public LogLevel Level {get;set;}
    }
}