using System;

namespace MqttBroker.Models
{
    public class Log
    {
        public int Id { get; set; }
        public string Exception { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
    }
}