using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace HandyHansel
{
    internal class NormHostedService : IHostedService
    {
        private readonly BotService _discordBot;
        public NormHostedService(BotService bot)
        {
            this._discordBot = bot;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await this._discordBot.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this._discordBot.StopAsync();
        }
    }
}
