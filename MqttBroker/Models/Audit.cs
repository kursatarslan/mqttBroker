﻿using System;

namespace MqttBroker.Models
{
    public class Audit
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string ClientId { get; set; }
        public string VechicleId { get; set; }
        public DateTime CreationDate { get; set; } = DateTime.Now;
        public string Payload { get; set; }
    }
}