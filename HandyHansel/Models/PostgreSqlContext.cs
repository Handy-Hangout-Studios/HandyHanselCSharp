using Microsoft.EntityFrameworkCore;
using System;

namespace HandyHansel.Models
{
    public class PostgreSqlContext : DbContext
    {
        // ReSharper disable once RedundantBaseConstructorCall
        public PostgreSqlContext() : base()
        {

        }

        public PostgreSqlContext(DbContextOptions<PostgreSqlContext> options) : base(options)
        {

        }

        // ReSharper disable four UnusedAutoPropertyAccessor.Local
        public DbSet<UserTimeZone> UserTimeZones { get; private set; }
        public DbSet<GuildEvent> GuildEvents { get; private set; }
        public DbSet<ScheduledEvent> ScheduledEvents { get; private set; }
        public DbSet<GuildPrefix> GuildPrefixes { get; private set; }

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
