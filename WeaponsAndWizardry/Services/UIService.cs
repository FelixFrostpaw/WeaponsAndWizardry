using Discord;
using Discord.WebSocket;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;
using WeaponsAndWizardry.Repositories;

namespace WeaponsAndWizardry.Services
{
    // A class to update the "UI" every second. This updates what's shown in Discord.
    public class UIService
    {
        private readonly Timer Timer;
        private readonly DiscordSocketClient DiscordSocketClient;
        private readonly PlayersRepository PlayersRepository;
        private readonly AdventuresRepository AdventuresRepository;

        public UIService
        (
            DiscordSocketClient discordSocketClient,
            PlayersRepository playersRepository,
            AdventuresRepository adventuresRepository
        )
        {
            DiscordSocketClient = discordSocketClient;
            PlayersRepository = playersRepository;
            AdventuresRepository = adventuresRepository;

            Timer =
                new Timer
                (
                    async _ =>
                    {
                        await UIUpdate();
                    },
                    null,
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(2)
                );
        }

        private async Task UIUpdate()
        {
            try
            {
                List<Task> tasks = new List<Task>();
                tasks.Add(DisplayAdventures());
                tasks.Add(DisplayPlayers());

                await Task.WhenAll(tasks.ToArray());

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }


        public static readonly int BarDisplaySize = 10;

        private async Task DisplayPlayers()
        {
            try
            {
                FeedIterator<Player> feedIterator = PlayersRepository.GetAll();
                List<Task> playerTasks = new List<Task>();
                while (feedIterator.HasMoreResults)
                {
                    foreach (Player player in await feedIterator.ReadNextAsync())
                    {
                        Task playerTask = DisplayPlayer(player);
                        playerTasks.Add(playerTask);
                    }
                }
                await Task.WhenAll(playerTasks.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task DisplayPlayer(Player player)
        {
            try
            {
                // Get the channel for this adventure..
                SocketUser user = DiscordSocketClient.GetUser(Convert.ToUInt64(player.Id));
                IDMChannel channel = await user.GetOrCreateDMChannelAsync();

                // Get the adventureMessage that we will update.
                IUserMessage playerSheetMessage;
                // If we were passed the "RegenerateMessages" flag set to true, or if there is no PlayerSheetMessage, then create a new adventureMessage.
                if (player.RegenerateMessage || player.PlayerSheetMessage == 0)
                {
                    playerSheetMessage = await channel.SendMessageAsync("<PLAYER SHEET MESSAGE>") as IUserMessage;
                }
                // If we were passed the "RegenerateMessages" flag set to false, then retrieve the existing adventureMessage.
                else
                {
                    playerSheetMessage = await channel.GetMessageAsync(player.PlayerSheetMessage) as IUserMessage;
                }

                // Alter the messages to now have new status updates.
                EmbedBuilder embedBuilder = new EmbedBuilder();

                embedBuilder.Color = Color.Blue;

                string adventureHeader = $"PLAYER SHEET";
                embedBuilder.Title = adventureHeader;

                if (player.GameStatus == Player.PlayerGameStatus.Adventure)
                {
                    double healthTicksRaw = player.Health / (double)player.MaxHealth;
                    int healthTicks = 0;
                    for (int i = 1; i < BarDisplaySize + 1; i++)
                    {
                        if (healthTicksRaw < (i / (double)BarDisplaySize)) break;
                        healthTicks += 1;
                    }

                    string healthBar = "[";
                    for (int i = 0; i < healthTicks; i++)
                    {
                        healthBar += "+";
                    }
                    for (int i = 0; i < BarDisplaySize - healthTicks; i++)
                    {
                        healthBar += "-";
                    }
                    healthBar += "]";

                    double manaTicksRaw = player.Mana / (double)Player.MaxMana;
                    int manaTicks = 0;
                    for (int i = 1; i < BarDisplaySize + 1; i++)
                    {
                        if (manaTicksRaw < (i / (double)BarDisplaySize)) break;
                        manaTicks += 1;
                    }
                    string manaBar = "[";
                    for (int i = 0; i < manaTicks; i++)
                    {
                        manaBar += "+";
                    }
                    for (int i = 0; i < BarDisplaySize - manaTicks; i++)
                    {
                        manaBar += "-";
                    }
                    manaBar += "]";

                    embedBuilder.AddField("Class", player.Class == null ? "NOT SET!" : player.Class.ToString());
                    embedBuilder.AddField("Health Points", $"{player.Health}/{player.MaxHealth} {healthBar}");
                    embedBuilder.AddField("Mana Points", $"{player.Mana}/{Player.MaxMana} {manaBar}");

                    string timePassed = (player.AdventureJoinTime == null) ? "" : (DateTime.UtcNow - player.AdventureJoinTime.Value).ToString(@"mm\:ss");
                    embedBuilder.AddField("Status", $"On Adventure ({timePassed})");
                }
                else if (player.GameStatus == Player.PlayerGameStatus.Idle)
                {
                    embedBuilder.AddField("Class", player.Class == null ? "NOT SET!" : player.Class.ToString());
                    embedBuilder.AddField("Health Points", $"{player.MaxHealth}/{player.MaxHealth}");
                    embedBuilder.AddField("Mana Points", $"{Player.MaxMana}/{Player.MaxMana}");

                    embedBuilder.AddField("Status", "Idle");
                }
                else
                {
                    throw new Exception("Invalid Player Game Status!");
                }

                await playerSheetMessage.ModifyAsync(m =>
                {
                    m.Content = "";
                    m.Embed = embedBuilder.Build();
                });

                // If we were passed the "RegenerateMessages" flag set to true, or if there was no old PlayerSheetMessage, then delete old adventureMessage if possible.
                // Then, toggle the "RegenerateMessages" flag to false, and set the new adventureMessage on the Adventure.
                if (player.RegenerateMessage || player.PlayerSheetMessage == 0)
                {
                    if (player.PlayerSheetMessage != 0)
                    {
                        await channel.DeleteMessageAsync(await channel.GetMessageAsync(player.PlayerSheetMessage));
                    }

                    await PlayersRepository.SetNewChannelsForMessages
                    (
                        playerId: player.Id,
                        playerSheetMessage: playerSheetMessage.Id
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task DisplayAdventures()
        {
            try
            {
                FeedIterator<Adventure> feedIterator = AdventuresRepository.GetAll();
                List<Task> adventureTasks = new List<Task>();
                while (feedIterator.HasMoreResults)
                {
                    foreach (Adventure adventure in await feedIterator.ReadNextAsync())
                    {
                        Task adventureTask = DisplayAdventure(adventure);
                        adventureTasks.Add(adventureTask);
                    }
                }
                await Task.WhenAll(adventureTasks.ToArray());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task DisplayAdventure(Adventure adventure)
        {
            try
            {
                // Get the channel for this adventure..
                IMessageChannel channel = DiscordSocketClient.GetChannel(adventure.Channel) as IMessageChannel;

                // Get all players associated with this adventure.
                IEnumerable<Player> players = await PlayersRepository.GetAdventuringPlayersAsEnumerable(adventure.Id);

                // Get the adventureMessage that we will update.
                IUserMessage adventureMessage;
                // If we were passed the "RegenerateMessages" flag set to true, or if there is no Adventure Message, then create a new adventureMessage.
                if (adventure.RegenerateMessage || adventure.AdventureMessage == 0)
                {
                    adventureMessage = await channel.SendMessageAsync("<ADVENTURE MESSAGE>") as IUserMessage;
                }
                // If we were passed the "RegenerateMessages" flag set to false, then retrieve the existing adventureMessage.
                else
                {
                    adventureMessage = await channel.GetMessageAsync(adventure.AdventureMessage) as IUserMessage;
                }

                // Alter the messages to now have new status updates.
                EmbedBuilder embedBuilder = new EmbedBuilder();

                embedBuilder.Color = Color.Blue;

                TimeSpan timePassed = DateTime.UtcNow - adventure.StartTime;
                string adventureHeader = $"ADVENTURE - {timePassed.ToString(@"mm\:ss")}";
                embedBuilder.Title = adventureHeader;

                List<string> frontlinePlayers = new List<string>();
                List<string> midlinePlayers = new List<string>();
                List<string> backlinePlayers = new List<string>();

                foreach (Player player in players)
                {
                    double healthTicksRaw = player.Health / (double)player.MaxHealth;
                    int healthTicks = 0;
                    for (int i = 1; i < BarDisplaySize + 1; i++)
                    {
                        if (healthTicksRaw < (i / (double)BarDisplaySize)) break;
                        healthTicks += 1;
                    }

                    string healthBar = "[";
                    for (int i = 0; i < healthTicks; i++)
                    {
                        healthBar += "+";
                    }
                    for (int i = 0; i < BarDisplaySize - healthTicks; i++)
                    {
                        healthBar += "-";
                    }
                    healthBar += "]";

                    double manaTicksRaw = player.Mana / (double)Player.MaxMana;
                    int manaTicks = 0;
                    for (int i = 1; i < BarDisplaySize + 1; i++)
                    {
                        if (manaTicksRaw < (i / (double)BarDisplaySize)) break;
                        manaTicks += 1;
                    }
                    string manaBar = "[";
                    for (int i = 0; i < manaTicks; i++)
                    {
                        manaBar += "+";
                    }
                    for (int i = 0; i < BarDisplaySize - manaTicks; i++)
                    {
                        manaBar += "-";
                    }
                    manaBar += "]";

                    string display = $"<@{player.Id}> ({player.Class}) \n HP {player.Health}/{player.MaxHealth} {healthBar} \n MP {player.Mana}/{Player.MaxMana} {manaBar}";

                    switch (player.AdventureRank)
                    {
                        case Adventure.Rank.Frontline:
                            frontlinePlayers.Add(display);
                            break;
                        case Adventure.Rank.Midline:
                            midlinePlayers.Add(display);
                            break;
                        case Adventure.Rank.Backline:
                            backlinePlayers.Add(display);
                            break;
                        default:
                            throw new Exception("Invalid Rank!");
                    }
                }

                string frontlinePlayerField = string.Join("\n\n", frontlinePlayers.ToArray());
                if (string.IsNullOrEmpty(frontlinePlayerField)) frontlinePlayerField = "<No Players>";

                string midlinePlayerField = string.Join("\n\n", midlinePlayers.ToArray());
                if (string.IsNullOrEmpty(midlinePlayerField)) midlinePlayerField = "<No Players>";

                string backlinePlayerField = string.Join("\n\n", backlinePlayers.ToArray());
                if (string.IsNullOrEmpty(backlinePlayerField)) backlinePlayerField = "<No Players>";

                embedBuilder.AddField("Player Frontline", frontlinePlayerField);
                embedBuilder.AddField("Player Midline", midlinePlayerField);
                embedBuilder.AddField("Player Backline", backlinePlayerField);
                embedBuilder.AddField("\u200b", "\u200b");

                string adventureLog = string.Join("\n", (adventure.Logs ?? new List<string>()).ToArray());
                if (string.IsNullOrEmpty(adventureLog)) adventureLog = "<No Logs>";

                embedBuilder.AddField("Adventure Log", adventureLog);

                await adventureMessage.ModifyAsync(m =>
                {
                    m.Content = "";
                    m.Embed = embedBuilder.Build();
                });

                // If we were passed the "RegenerateMessages" flag set to true, or if there was no AdventureMessage, then delete old adventureMessage if possible.
                // Then, toggle the "RegenerateMessages" flag to false, and set the new adventureMessage on the Adventure.
                if (adventure.RegenerateMessage || adventure.AdventureMessage == 0)
                {
                    if (adventure.AdventureMessage != 0)
                    {
                        await channel.DeleteMessageAsync(await channel.GetMessageAsync(adventure.AdventureMessage));
                    }

                    await AdventuresRepository.SetNewChannelsForMessages
                    (
                        adventureId: adventure.Id,
                        adventureMessage: adventureMessage.Id
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
