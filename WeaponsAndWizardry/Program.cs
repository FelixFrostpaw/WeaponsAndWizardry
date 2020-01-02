using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using WeaponsAndWizardry.Repositories;
using WeaponsAndWizardry.Services;

namespace WeaponsAndWizardry
{
    class Program
    {
        public static readonly string WWGameStatus = "!w help";
        public static readonly string[] WWPrefixes = { "!w ", "!w" };

        public static readonly string WWCosmosDBEndpoint = "WWCosmosDBEndpoint";
        public static readonly string WWCosmosDBKey = "WWCosmosDBKey";
        public static readonly string WWDatabaseId = "WWDatabaseId";
        public static readonly string WWDiscordToken = "WWDiscordToken";

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            // Create Configuration, to grab Secrets from there if possible.
            IConfigurationRoot Configuration = BootstrapConfiguration();

            // Get all of our Secrets, first from Configuration, and defaulting to Enviornment Variables if they don't exist there.
            string cosmosDBEndpoint = Configuration[WWCosmosDBEndpoint] ?? Environment.GetEnvironmentVariable(WWCosmosDBEndpoint);
            string cosmosDBKey = Configuration[WWCosmosDBKey] ?? Environment.GetEnvironmentVariable(WWCosmosDBKey);
            string databaseName = Configuration[WWDatabaseId] ?? Environment.GetEnvironmentVariable(WWDatabaseId);

            // Create a Database.
            Database database = await CreateDatabaseAsync(
                cosmosDBEndpoint: cosmosDBEndpoint,
                cosmosDBKey: cosmosDBKey,
                databaseName: databaseName
            );

            using (ServiceProvider services = await ConfigureServicesAsync(database))
            {
                DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                string discordToken = Configuration[WWDiscordToken] ?? Environment.GetEnvironmentVariable(WWDiscordToken);

                await client.LoginAsync(TokenType.Bot, discordToken);
                await client.SetGameAsync(WWGameStatus, type: ActivityType.Playing);
                await client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync(WWPrefixes);

                await Task.Delay(-1);
            }
        }

        private async Task<Database> CreateDatabaseAsync(
            string cosmosDBEndpoint,
            string cosmosDBKey,
            string databaseName
        )
        {
            CosmosClient cosmosClient = new CosmosClient(
                cosmosDBEndpoint, 
                cosmosDBKey, 
                new CosmosClientOptions
                {
                    ConnectionMode = ConnectionMode.Direct
                });
            Database database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            return database;
        }

        private async Task<ServiceProvider> ConfigureServicesAsync(Database database)
        {
            DiscordSocketClient client = new DiscordSocketClient();

            Container playersRepositoryContainer = await PlayersRepository.CreateContainer(database);
            PlayersRepository playersRepository = new PlayersRepository(playersRepositoryContainer);

            Container adventuresRepositoryContainer = await AdventuresRepository.CreateContainer(database);
            AdventuresRepository adventuresRepository = new AdventuresRepository(adventuresRepositoryContainer);

            TickService tickService = new TickService(playersRepository, adventuresRepository);
            UIService uiService = new UIService(client, playersRepository, adventuresRepository);

            return new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<HttpClient>()
                .AddSingleton(tickService)
                .AddSingleton(uiService)
                .AddSingleton(playersRepository)
                .AddSingleton(adventuresRepository)
                .BuildServiceProvider();
        }

        private static IConfigurationRoot BootstrapConfiguration()
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            if (string.IsNullOrWhiteSpace(env))
            {
                env = "Development";
            }

            var builder = new ConfigurationBuilder();

            if (env == "Development")
            {
                builder.AddUserSecrets<Program>();
            }

            return builder.Build();
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
    }
}
