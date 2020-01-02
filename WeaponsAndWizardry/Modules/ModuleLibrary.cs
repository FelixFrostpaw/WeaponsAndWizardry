using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using WeaponsAndWizardry.Models;

namespace WeaponsAndWizardry.Modules
{
    public static class ModuleLibrary
    {
        public static async Task<bool> PlayerExists(Player player, SocketUser user)
        {
            if (player == null)
            {
                await user.SendMessageAsync("You have not registered!");
                return false;
            }
            return true;
        }

        public static async Task<bool> AdventureExists(Adventure adventure, SocketUser user)
        {
            if (adventure == null)
            {
                await user.SendMessageAsync("Adventure does not exist!");
                return false;
            }
            return true;
        }

        public static async Task<bool> PlayerSetup(Player player, SocketUser user)
        {
            if (!(await PlayerExists(player, user))) return false;
            if (string.IsNullOrWhiteSpace(player.Class))
            {
                await user.SendMessageAsync("You must have a Class selected!");
                return false;
            }
            return true;
        }

        public static async Task<bool> PlayerHasNoAdventure(Player player, SocketUser user)
        {
            if (!(await PlayerExists(player, user))) return false;
            if (player.Adventure != null)
            {
                await user.SendMessageAsync("You are already on an adventure!");
                return false;
            }
            return true;
        }

        public static async Task<bool> PlayerHasAdventure(Player player, SocketUser user)
        {
            if (!(await PlayerExists(player, user))) return false;
            if (player.Adventure == null)
            {
                await user.SendMessageAsync("You must be on an adventure!");
                return false;
            }
            return true;
        }
    }
}
