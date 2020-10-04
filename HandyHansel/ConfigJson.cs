namespace HandyHansel
{
    public class ConfigJson
    {
        // For bot login
        public string BotToken { get; set; }

        // For logging purposes
        public ulong DevId { get; set; }

        // For database login
        public string Host { get; set; }
        public int Port { get; set; }
        public string BotDatabase { get; set; }
        public string HangfireDatabase { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Pooling { get; set; }
    }
}