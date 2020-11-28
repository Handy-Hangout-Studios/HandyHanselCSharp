using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HandyHansel.Utilities
{
    public static class InteractivityExtensionExtensionMethods
    {
        public static async Task<Reaction> AddAndWaitForYesNoReaction(this InteractivityExtension interactivity, DiscordMessage msg, DiscordUser user)
        {
            DiscordClient client = interactivity.Client;

            await msg.CreateReactionAsync(DiscordEmoji.FromName(client, ":regional_indicator_y:"));
            await msg.CreateReactionAsync(DiscordEmoji.FromName(client, ":regional_indicator_n:"));

            InteractivityResult<MessageReactionAddEventArgs> interactivityResult = await interactivity.WaitForReactionAsync(msg, user);

            if (interactivityResult.TimedOut || interactivityResult.Result.Emoji.Equals(DiscordEmoji.FromName(client, ":regional_indicator_n:")))
            {
                DiscordMessage snark = await interactivityResult.Result.Channel.SendMessageAsync($"{user.Mention}, well then why did you get my attention! Thanks for wasting my time. Let me clean up now. :triumph:");
                await Task.Delay(5000);
                await interactivityResult.Result.Channel.DeleteMessagesAsync(new List<DiscordMessage> { msg, snark });
                return interactivityResult.TimedOut ? Reaction.None : Reaction.No;
            }

            await msg.DeleteAllReactionsAsync();
            return Reaction.Yes;
        }
    }

    public enum Reaction
    {
        Yes,
        No,
        None
    }
}
