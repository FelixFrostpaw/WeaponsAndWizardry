using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Net;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;

namespace WeaponsAndWizardry.Repositories
{
    public class AdventuresRepository : IRepositoryWithDelete<Adventure>
    {
        private readonly Container Container;
        public static readonly string containerId = "Adventures";

        public static async Task<Container> CreateContainer(Database database)
        {
            return await database.CreateContainerIfNotExistsAsync(containerId, "/id");
        }

        public AdventuresRepository(Container container)
        {
            this.Container = container;
        }

        public async Task<Adventure> Get(string id)
        {
            try
            {
                ItemResponse<Adventure> itemResponse = await Container.ReadItemAsync<Adventure>(id, new PartitionKey(id));
                return itemResponse.Resource;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<bool> Create(Adventure adventure)
        {
            try
            {
                ItemResponse<Adventure> itemResponse = await Container.CreateItemAsync(adventure);
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return false;
            }
        }

        public async Task<bool> Update(Adventure adventure)
        {
            try
            {
                await Container.ReplaceItemAsync(adventure, adventure.Id, new PartitionKey(adventure.Id), new ItemRequestOptions { IfMatchEtag = adventure.ETag });
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                return false;
            }
        }

        public async Task<bool> Delete(string id)
        {
            try
            {
                await Container.DeleteItemAsync<Adventure>(id, new PartitionKey(id));
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public FeedIterator<Adventure> GetAll()
        {
            FeedIterator<Adventure> feedIterator =
                Container
                    .GetItemLinqQueryable<Adventure>()
                    .ToFeedIterator();
            return feedIterator;
        }

        public async Task<bool> AddLogToAdventure(string adventureId, string logEntry)
        {
            try
            {
                bool success = false;
                while (!success)
                {
                    Adventure adventure = await Get(adventureId);

                    if (adventure == null) return false;

                    adventure.AddLog(logEntry);
                    success = await Update(adventure);
                }
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<bool> RegenerateMessages(string adventureId)
        {
            try
            {
                bool success = false;
                while (!success)
                {
                    Adventure adventure = await Get(adventureId);

                    if (adventure == null) return false;

                    adventure.RegenerateMessage = true;
                    success = await Update(adventure);
                }
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<bool> SetNewChannelsForMessages
        (
            string adventureId,
            ulong adventureMessage
        )
        {
            try
            {
                bool success = false;
                while (!success)
                {
                    Adventure adventure = await Get(adventureId);

                    if (adventure == null) return false;

                    adventure.AdventureMessage = adventureMessage;
                    adventure.RegenerateMessage = false;
                    success = await Update(adventure);
                }
                return true;
            }
            catch (CosmosException cosmosException) when (cosmosException.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
