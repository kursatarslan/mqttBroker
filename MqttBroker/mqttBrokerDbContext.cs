using System;
using Microsoft.EntityFrameworkCore;
using MqttBroker.Models;

namespace MqttBroker
{
    public class MqttBrokerDbContext : DbContext
    {
        public MqttBrokerDbContext()

        {
        }

        public MqttBrokerDbContext(DbContextOptions<MqttBrokerDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Audit> Audit { get; set; }
        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<Platoon> Platoon { get; set; }
        public virtual DbSet<Subscribe> Subscribe { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                optionsBuilder.UseNpgsql(
                    "Host=localhost;Port=5432;Database=mqtt;Username=postgres;Password=postgres;Maximum Pool Size=90;Connection Idle Lifetime=120;Connection Pruning Interval=30;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Log>();
            modelBuilder.Entity<Platoon>();
            modelBuilder.Entity<Subscribe>();
            modelBuilder.Entity<Audit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ClientId).HasColumnName("ClientId");
                entity.Property(e => e.VechicleId).HasColumnName("VechicleId");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        private void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
        }
    }
}