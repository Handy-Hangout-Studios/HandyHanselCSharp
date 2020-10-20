using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HandyHansel.Models
{
    public class PostgreSqlContext : DbContext
    {
        public PostgreSqlContext() : base()
        {
        }

        public PostgreSqlContext(string connectionString)
        {
            DbConnectionString = connectionString;
        }
        
        public PostgreSqlContext(DbContextOptions<PostgreSqlContext> options) : base(options)
        {
        }

        public DbSet<UserTimeZone> UserTimeZones { get; private set; }
        public DbSet<GuildEvent> GuildEvents { get; private set; }
        public DbSet<GuildPrefix> GuildPrefixes { get; private set; }
        
        private string DbConnectionString { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrWhiteSpace(DbConnectionString))
            {
                IConfigurationRoot config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("config.json")
                    .Build();

                BotConfig configJson = new BotConfig();
                config.GetSection(BotConfig.Section).Bind(configJson);
                
                NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
                {
                    Host = configJson.Database.Host,
                    Port = configJson.Database.Port,
                    Database = configJson.Database.Name,
                    Username = configJson.Database.Username,
                    Password = configJson.Database.Password,
                    Pooling = configJson.Database.Pooling,
                };

                DbConnectionString = connectionStringBuilder.ConnectionString;
            }

            optionsBuilder.UseNpgsql(DbConnectionString);
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