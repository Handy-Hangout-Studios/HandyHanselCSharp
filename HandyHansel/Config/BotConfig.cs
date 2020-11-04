namespace HandyHansel
{
    public class BotConfig
    {
        public static readonly string Section = "BotConfig";
        // For bot login
        public string BotToken { get; set; }

        // For logging purposes
        public ulong DevId { get; set; }

        public DatabaseConfig Database { get; set; }
    }
}