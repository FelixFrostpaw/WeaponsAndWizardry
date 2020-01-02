using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;

namespace WeaponsAndWizardry.Repositories
{
    public class PlayersRepository : IRepository<Player>
    {
        private readonly Container Container;
        public static readonly string containerId = "Players";

        public static async Task<Container> CreateContainer(Database database)
        {
            return await database.CreateContainerIfNotExistsAsync(containerId, "/id");
        }

        public PlayersRepository(Container container)
        {
            this.Container = container;
        }

        public async Task<Player> Get(string id)
        {
            try
            {
                ItemResponse<Player> itemResponse = await Container.ReadItemAsync<Player>(id, new PartitionKey(id));
                return itemResponse.Resource;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound) 
            {
                return null;
            }
        }

        public async Task<bool> Create(Player player)
        {
            try
            {
                ItemResponse<Player> itemResponse = await Container.CreateItemAsync(player);
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return false;
            }
        }

        public async Task<bool> Update(Player player)
        {
            try
            {
                await Container.ReplaceItemAsync(player, player.Id, new PartitionKey(player.Id), new ItemRequestOptions { IfMatchEtag = player.ETag });
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
        }

        internal FeedIterator<Player> GetAll()
        {
            FeedIterator<Player> feedIterator =
                Container
                    .GetItemLinqQueryable<Player>()
                    .ToFeedIterator();
            return feedIterator;
        }

        public FeedIterator<Player> GetAllAdventuringPlayers()
        {
            FeedIterator<Player> feedIterator = 
                Container
                    .GetItemLinqQueryable<Player>()
                    .Where(u => u.Adventure != null)
                    .ToFeedIterator();
            return feedIterator;
        }

        public FeedIterator<Player> GetAdventuringPlayers(string adventureId)
        {
            FeedIterator<Player> feedIterator =
            Container
                .GetItemLinqQueryable<Player>()
                .Where(u => u.Adventure == adventureId)
                .OrderBy(u => u.AdventureJoinTime)
                .ToFeedIterator();
            return feedIterator;
        }

        public async Task<IEnumerable<Player>> GetAdventuringPlayersAsEnumerable(string adventureId)
        {
            List<Player> players = new List<Player>();
            FeedIterator<Player> feedIterator = GetAdventuringPlayers(adventureId);
            while (feedIterator.HasMoreResults)
            {
                FeedResponse<Player> feedResponse = await feedIterator.ReadNextAsync();
                players.AddRange(feedResponse.Resource);
            }
            return players;
        }

        public async Task RemovePlayersFromAdventure(string adventureId)
        {
            FeedIterator<Player> feedIterator = GetAdventuringPlayers(adventureId);
            List<Task> playerTasks = new List<Task>();
            while (feedIterator.HasMoreResults)
            {
                foreach (Player player in await feedIterator.ReadNextAsync())
                {
                    Task playerTask = RemovePlayerFromAdventure(player);
                    playerTasks.Add(playerTask);
                }
            }
            await Task.WhenAll(playerTasks.ToArray());
        }

        public async Task RemovePlayerFromAdventure(Player player)
        {
            bool success = false;
            while (!success)
            {
                player.GameStatus = Player.PlayerGameStatus.Idle;
                player.Adventure = null;
                player.AdventureJoinTime = null;
                player.Mana = 0;
                player.AdventureRank = null;
                success = await Update(player);

                if (!success)
                {
                    player = await Get(player.Id);
                    if (player.Adventure == null) return;
                }
            }
        }

        public async Task AddPlayerToAdventure(
            string playerId,
            string adventureId,
            Adventure.Rank adventureRank
        )
        {
            bool success = false;
            while (!success)
            {
                Player player = await Get(playerId);
                player.GameStatus = Player.PlayerGameStatus.Adventure;
                player.Adventure = adventureId;
                player.AdventureJoinTime = DateTime.UtcNow;
                player.AdventureRank = adventureRank;
                player.Mana = 0;
                player.Health = player.MaxHealth;
                success = await Update(player);
            }
        }

        public async Task<bool> SetNewChannelsForMessages
        (
            string playerId,
            ulong playerSheetMessage
        )
        {
            try
            {
                bool success = false;
                while (!success)
                {
                    Player player = await Get(playerId);

                    if (player == null) return false;

                    player.PlayerSheetMessage = playerSheetMessage;
                    player.RegenerateMessage = false;
                    success = await Update(player);
                }
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<bool> ChangePlayerAdventureRank
        (
            string playerId,
            Adventure.Rank adventureRank
        )
        {
            bool success = false;
            while (!success)
            {
                Player player = await Get(playerId);

                if (player.GameStatus != Player.PlayerGameStatus.Adventure) return false;

                player.AdventureRank = adventureRank;
                success = await Update(player);
            }
            return true;
        }
    }
}
