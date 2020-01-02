using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;
using WeaponsAndWizardry.Repositories;

namespace WeaponsAndWizardry.Modules
{
    [Group("adventure")]
    [Alias("a", "adv", "adventures")]
    public class AdventureModule : ModuleBase<SocketCommandContext>
    {
        private readonly PlayersRepository PlayersRepository;
        private readonly AdventuresRepository AdventuresRepository;

        public AdventureModule(PlayersRepository playersRepository, AdventuresRepository adventuresRepository)
        {
            PlayersRepository = playersRepository;
            AdventuresRepository = adventuresRepository;
        }

        [RequireContext(ContextType.Guild)]
        [Command("start")]
        [Alias("begin", "join")]
        public async Task Start([Remainder] string args = "")
        {
            Adventure.Rank adventureRank;
            args = (args ?? "").ToString().ToLower().Trim();
            if (args.StartsWith('f'))
            {
                adventureRank = Adventure.Rank.Frontline;
            }
            else if (args.StartsWith('m'))
            {
                adventureRank = Adventure.Rank.Midline;
            }
            else if (args.StartsWith('b'))
            {
                adventureRank = Adventure.Rank.Backline;
            }
            else
            {
                await Context.User.SendMessageAsync("Invalid input! You need to specify a rank! front, mid, or back!");
                return;
            }   

            Player player = await PlayersRepository.Get(Context.User.Id.ToString());
            if (!(await ModuleLibrary.PlayerExists(player, Context.User))) return;
            if (!(await ModuleLibrary.PlayerSetup(player, Context.User))) return;
            if (!(await ModuleLibrary.PlayerHasNoAdventure(player, Context.User))) return;

            // Create an adventure if it doesn't already exist.
            await AdventuresRepository.Create
            (
                new Adventure()
                {
                    Id = Context.Channel.Id.ToString(),
                    Channel = Context.Channel.Id,
                    Guild = Context.Guild.Id,
                    StartTime = DateTime.UtcNow,
                    RegenerateMessage = true
                }
            );

            await PlayersRepository.AddPlayerToAdventure
            (
                playerId: Context.User.Id.ToString(),
                adventureId: Context.Channel.Id.ToString(),
                adventureRank: adventureRank
            );

            await AdventuresRepository.AddLogToAdventure
            (
                adventureId: Context.Channel.Id.ToString(),
                logEntry: $"<@{player.Id}> joined the Adventure at the {adventureRank}!"
            );
        }

        [RequireContext(ContextType.Guild)]
        [Command("stop")]
        [Alias("end")]
        public async Task Stop()
        {
            await PlayersRepository.RemovePlayersFromAdventure(Context.Channel.Id.ToString());
            bool adventureDeleted = await AdventuresRepository.Delete(Context.Channel.Id.ToString());
            if (adventureDeleted)
            {
                await ReplyAsync("Ending adventure!");
                await PlayersRepository.RemovePlayersFromAdventure(Context.Channel.Id.ToString());
            }
            else
            {
                await Context.User.SendMessageAsync("No adventure to stop!");
            }
        }
    }
}
