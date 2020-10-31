using HandyHansel.BotDatabase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using Streamx.Linq.SQL.EFCore;
using Streamx.Linq.SQL.PostgreSQL;
using System;

namespace HandyHansel.Models
{
    public class PostgreSqlContext : DbContext
    {
        public PostgreSqlContext() : base()
        {
        }

        public PostgreSqlContext(string connectionString, ILoggerFactory loggerFactory)
        {
            this.DbConnectionString = connectionString;
            this._loggerFactory = loggerFactory;
        }

        public PostgreSqlContext(DbContextOptions<PostgreSqlContext> options) : base(options)
        {
        }

        public DbSet<UserTimeZone> UserTimeZones { get; private set; }
        public DbSet<GuildEvent> GuildEvents { get; private set; }
        public DbSet<GuildPrefix> GuildPrefixes { get; private set; }
        public DbSet<GuildKarmaRecord> GuildKarmaRecords { get; private set; }
        public DbSet<UserCard> UserCards { get; private set; }
        public DbSet<GuildBackgroundJob> GuildBackgroundJobs { get; private set; }

        private string DbConnectionString { get; set; }
        private ILoggerFactory _loggerFactory { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (string.IsNullOrWhiteSpace(this.DbConnectionString))
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

                this.DbConnectionString = connectionStringBuilder.ConnectionString;
            }

            optionsBuilder
                .UseLoggerFactory(this._loggerFactory)
                .UseNpgsql(this.DbConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            ELinq.Configuration.RegisterVendorCapabilities();
        }

        public override int SaveChanges()
        {
            this.ChangeTracker.DetectChanges();
            return base.SaveChanges();
        }
    }
}