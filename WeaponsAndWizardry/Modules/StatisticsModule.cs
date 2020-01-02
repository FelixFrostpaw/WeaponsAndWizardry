using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;
using WeaponsAndWizardry.Repositories;

namespace WeaponsAndWizardry.Modules
{
    public class StatisticsModule : ModuleBase<SocketCommandContext>
    {
        private readonly PlayersRepository PlayersRepository;
        private readonly AdventuresRepository AdventuresRepository;

        public StatisticsModule(PlayersRepository playersRepository, AdventuresRepository adventuresRepository)
        {
            PlayersRepository = playersRepository;
            AdventuresRepository = adventuresRepository;
        }

        [RequireContext(ContextType.Guild)]
        [Command("w")]
        public async Task GetAdventureContext()
        {
            bool success = await AdventuresRepository.RegenerateMessages(adventureId: Context.Channel.Id.ToString());

            if (!success)
            {
                await Context.User.SendMessageAsync("Adventure does not exist!");
            }
        }

        [Command("help")]
        public async Task Help(params string[] objects)
        {
            EmbedBuilder builder = new EmbedBuilder();

            builder.AddField("class CLASS", "Select a class. There are 7. Fighter, Rogue, Ranger, Cleric, Wizard, Bard, Barbarian.");
            builder.AddField("register", "Register an account.");
            builder.AddField("adventure", "Start an adventure.");
            await Context.User.SendMessageAsync("", false, builder.Build());
        }

        [Command("class")]
        public async Task Class([Remainder] string args = null)
        {
            if (string.IsNullOrWhiteSpace(args))
            {
                Player player = await PlayersRepository.Get(Context.User.Id.ToString());
                await Context.User.SendMessageAsync($"Your Class is {(string.IsNullOrWhiteSpace(player.Class) ? "Not Set!" : player.Class)}");
                return;
            }

            args = args.ToLower().Trim();
            args = args.First().ToString().ToUpper() + args.Substring(1);

            HashSet<string> classes = new HashSet<string>() { "Fighter", "Rogue", "Ranger", "Cleric", "Wizard", "Bard", "Barbarian" };

            if (classes.Contains(args))
            {
                bool success = false;
                while (!success)
                {
                    Player player = await PlayersRepository.Get(Context.User.Id.ToString());
                    player.Class = args;
                    success = await PlayersRepository.Update(player);
                }
                await Context.User.SendMessageAsync($"Class set to {args}!");
            }
            else
            {
                await Context.User.SendMessageAsync("Invalid Class Selection!");
            }
        }

        [Command("register")]
        public async Task Register()
        {
            Player player = new Player();
            player.Id = Context.User.Id.ToString();
            player.Health = 1000;
            player.MaxHealth = 1000;
            player.GameStatus = Player.PlayerGameStatus.Idle;
            await PlayersRepository.Create(player);
            await Context.User.SendMessageAsync("You have successfully registered! Make sure to pick a Class!");
        }

        [RequireContext(ContextType.Guild)]
        [Command("move")]
        public async Task Move([Remainder] string args = "")
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
            if (!(await ModuleLibrary.PlayerHasAdventure(player, Context.User))) return;

            Adventure.Rank? oldRank = player.AdventureRank;

            bool success = await PlayersRepository.ChangePlayerAdventureRank
            (
                playerId: Context.User.Id.ToString(),
                adventureRank: adventureRank
            );

            if (!success)
            {
                await Context.User.SendMessageAsync("Failed to move! Most likely, you've been removed from the adventure!");
                return;
            }

            await AdventuresRepository.AddLogToAdventure
            (
                adventureId: Context.Channel.Id.ToString(),
                logEntry: $"<@{player.Id}> moved from the {oldRank} to the {adventureRank}!"
            );
        }
    }
}
