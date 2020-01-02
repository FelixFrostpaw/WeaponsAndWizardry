using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace WeaponsAndWizardry.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService CommandService;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly IServiceProvider ServiceProvider;
        private string[] Prefixes;

        public CommandHandlingService(IServiceProvider serviceProvider)
        {
            CommandService = serviceProvider.GetRequiredService<CommandService>();
            DiscordSocketClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            ServiceProvider = serviceProvider;

            CommandService.CommandExecuted += CommandExecutedAsync;
            DiscordSocketClient.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync(string[] prefixes)
        {
            Prefixes = prefixes;
            await CommandService.AddModulesAsync(Assembly.GetEntryAssembly(), ServiceProvider);
        }

        public async Task MessageReceivedAsync(SocketMessage socketMessage)
        {
            // Ignore system messages, or messages that are not from users (such as messages from bots)
            if (!(socketMessage is SocketUserMessage message) || message.Source != MessageSource.User)
            {
                return;
            }

            // This value holds the offset where the prefix ends.
            int argumentPosition = 0;
            // If the message does not include a mention 
            // of this bot or our bot's prefix, then return early.
            if (!PrefixChecker(message, ref argumentPosition))
            {
                return;
            }

            var context = new SocketCommandContext(DiscordSocketClient, message);
            // we will handle the result in CommandExecutedAsync
            await CommandService.ExecuteAsync(context, argumentPosition, ServiceProvider); 
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext commandContext, IResult result)
        {
            // If the command is in a Guild Channel, delete the command.
            if (commandContext.Channel is IGuildChannel)
            {
                await commandContext.Channel.DeleteMessageAsync(commandContext.Message);
            }

            // If the command is not found, tell the user no such command exists, and return.
            if (!command.IsSpecified)
            {
                await commandContext.User.SendMessageAsync($"error: the command \"{commandContext.Message.Content}\" was not found");
                return;
            }

            // If the command failed, let's notify the user that something happened.
            if (!result.IsSuccess)
            {
                await commandContext.User.SendMessageAsync($"error: {result.ToString()}");
            }
        }

        private bool PrefixChecker(SocketUserMessage socketUserMessage, ref int argumentPosition)
        {
            foreach (string prefix in Prefixes)
            {
                if (socketUserMessage.HasStringPrefix(prefix, ref argumentPosition))
                {
                    return true;
                }
            }
            if (socketUserMessage.HasMentionPrefix(DiscordSocketClient.CurrentUser, ref argumentPosition))
            {
                return true;
            }

            return false;
        }
    }
}
