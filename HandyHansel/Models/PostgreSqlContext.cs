using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Models
{
    public class PostgreSqlContext : DbContext
    {
        public PostgreSqlContext() : base()
        {

        }

        public PostgreSqlContext(DbContextOptions<PostgreSqlContext> options) : base(options)
        {

        }

        public DbSet<GuildTimeZone> GuildTimeZones { get; private set; }
        public DbSet<GuildEvent> GuildEvents { get; private set; }
        
        public DbSet<ScheduledEvent> ScheduledEvents { get; private set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQL_CONN_STRING"));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
        }

        public override int SaveChanges()
        {
            ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}
