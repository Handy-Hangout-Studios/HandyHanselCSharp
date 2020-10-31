using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HandyHansel
{
    class NormHostedService : IHostedService
    {
        private readonly BotService _discordBot;
        public NormHostedService(BotService bot)
        {
            _discordBot = bot;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _discordBot.StartAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _discordBot.StopAsync();
        }
    }
}
