using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;
using Npgsql;

namespace HandyHansel.Models
{
    public class BotAccessProviderBuilder : IBotAccessProviderBuilder
    {
        public BotAccessProviderBuilder(IOptions<BotConfig> config, ILoggerFactory loggerFactory, IClock clock)
        {
            NpgsqlConnectionStringBuilder connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = config.Value.Database.Host,
                Port = config.Value.Database.Port,
                Database = config.Value.Database.Name,
                Username = config.Value.Database.Username,
                Password = config.Value.Database.Password,
                Pooling = config.Value.Database.Pooling,
            };
            this.connectionString = connectionStringBuilder.ConnectionString;
            this.loggerFactory = loggerFactory;
            this.clock = clock;
        }

        public IBotAccessProvider Build()
        {
            return new BotAccessPostgreSqlProvider(new PostgreSqlContext(this.connectionString, this.loggerFactory), this.clock);
        }

        private readonly string connectionString;
        private readonly ILoggerFactory loggerFactory;
        private readonly IClock clock;
    }
}