﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace HandyHansel.Models
{
    public class BotAccessProviderBuilder : IBotAccessProviderBuilder
    {
        public BotAccessProviderBuilder(IOptions<BotConfig> config, ILoggerFactory loggerFactory)
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
            _loggerFactory = loggerFactory;
        }

        public IBotAccessProvider Build()
        {
            return new BotAccessPostgreSqlProvider(new PostgreSqlContext(_connectionString, _loggerFactory));
        }

        private readonly string _connectionString;
        private readonly ILoggerFactory _loggerFactory;
    }
}