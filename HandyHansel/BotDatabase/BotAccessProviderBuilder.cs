using Microsoft.Extensions.Options;
using Npgsql;

namespace HandyHansel.Models
{
    public class BotAccessProviderBuilder : IBotAccessProviderBuilder
    {
        public BotAccessProviderBuilder(IOptions<BotConfig> config)
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
            _connectionString = connectionStringBuilder.ConnectionString;
        }

        public IBotAccessProvider Build()
        {
            return new BotAccessPostgreSqlProvider(new PostgreSqlContext(_connectionString));
        }

        private readonly string _connectionString;
    }
}