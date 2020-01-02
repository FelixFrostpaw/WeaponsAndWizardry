using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;
using WeaponsAndWizardry.Repositories;

namespace WeaponsAndWizardry.Services
{
    // A class to update Game State every second.
    public class TickService
    {
        private readonly Timer Timer;
        private readonly PlayersRepository PlayersRepository;
        private readonly AdventuresRepository AdventuresRepository;

        public TickService
        (
            PlayersRepository playersRepository,
            AdventuresRepository adventuresRepository
        )
        {
            PlayersRepository = playersRepository;
            AdventuresRepository = adventuresRepository;

            Timer = 
                new Timer
                (
                    async _ =>
                    {
                        await Tick();
                    },
                    null,
                    TimeSpan.FromSeconds(0),
                    TimeSpan.FromSeconds(1)
                );
        }

        private async Task Tick()
        {
            try
            {
                FeedIterator<Player> feedIterator = PlayersRepository.GetAllAdventuringPlayers();
                List<Task> playerTasks = new List<Task>();
                while (feedIterator.HasMoreResults)
                {
                    foreach (Player player in await feedIterator.ReadNextAsync())
                    {
                        Task playerTask = TickSinglePlayer(player);
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

        private async Task TickSinglePlayer(Player player)
        {
            bool success = false;
            while (!success)
            {
                // Increment Mana
                if (player.Mana < Player.MaxMana)
                {
                    player.Mana += 100;
                }

                success = await PlayersRepository.Update(player);

                if (!success)
                {
                    player = await PlayersRepository.Get(player.Id);
                    if (player.Adventure == null) return;
                }
            }
        }
    }
}
 