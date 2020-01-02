using System;

namespace WeaponsAndWizardry.Models
{
    public class Player : IModel
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "_etag")]
        public string ETag { get; set; }

        public bool RegenerateMessage { get; set; }

        public ulong PlayerPrivateMessage { get; set; }
        public ulong PlayerSheetMessage { get; set; }

        public PlayerGameStatus GameStatus { get; set; }

        public string Class { get; set; }

        public string Adventure { get; set; }

        public DateTime? AdventureJoinTime { get; set; }

        public Adventure.Rank? AdventureRank { get; set; }

        public int Health { get; set; }

        public int MaxHealth { get; set; }

        public int Mana { get; set; }

        public static readonly int MaxMana = 6000;

        public enum PlayerGameStatus
        {
            Idle,
            Adventure
        }
    }
}
