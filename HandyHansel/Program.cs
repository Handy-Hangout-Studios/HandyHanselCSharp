using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using HandyHansel.Commands;

namespace HandyHansel
{
    class Program
    {
        static DiscordClient discord;
        static CommandsNextExtension commands;
        static InteractivityExtension interactivity;

        static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            Config config = new Config();
            discord = new DiscordClient(config.ClientConfig);
            commands = discord.UseCommandsNext(config.CommandsConfig);
            interactivity = discord.UseInteractivity(config.InteractivityConfig);

            //commands.RegisterCommands<DNDCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            commands.RegisterCommands<GeneralCommands>();
            //commands.RegisterCommands<MinecraftCommands>(); // Is currently empty and so becomes NULL during the typeof used in the DSharpPlus.CommandsNext code
            commands.RegisterCommands<TimeCommands>();

            //discord.MessageCreated += PingMessageCreated;

            await discord.ConnectAsync();
            await Task.Delay(-1);
        }

        //private static async Task PingMessageCreated(MessageCreateEventArgs e)
        //{
        //    if (e.Message.Content.ToLower().StartsWith("ping"))
        //        await e.Message.RespondAsync("pong!");
        //}
    }
}
