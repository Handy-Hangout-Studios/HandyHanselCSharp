namespace HandyHansel.Models
{
    public class DataAccessProviderBuilder : IDataAccessProviderBuilder
    {
        public DataAccessProviderBuilder(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDataAccessProvider Build()
        {
            return new DataAccessPostgreSqlProvider(new PostgreSqlContext(_connectionString));
        }

        private readonly string _connectionString;
    }
}