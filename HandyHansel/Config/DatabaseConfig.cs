namespace HandyHansel
{
    public class DatabaseConfig
    {
        // For database login
        public string Host { get; set; }
        public int Port { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Pooling { get; set; }
    }
}
