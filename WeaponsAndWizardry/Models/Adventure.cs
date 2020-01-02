using System;
using System.Collections.Generic;

namespace WeaponsAndWizardry.Models
{
    public class Adventure : IModel
    {
        [Newtonsoft.Json.JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [Newtonsoft.Json.JsonProperty(PropertyName = "_etag")]
        public string ETag { get; }

        public ulong Channel { get; set; }

        public ulong Guild { get; set; }

        public DateTime StartTime { get; set; }

        public bool RegenerateMessage { get; set; }

        public ulong AdventureMessage { get; set; }

        public List<string> Logs { get; set; }
        public static readonly int LogMessageSize = 10;

        public void AddLog(string logEntry) 
        {
            if (Logs == null) Logs = new List<string>();

            Logs.Add(logEntry);
            if (Logs.Count > LogMessageSize)
            {
                Logs = Logs.GetRange(Logs.Count - LogMessageSize, LogMessageSize);
            }
        }

        public enum Rank
        {
            Frontline,
            Midline,
            Backline
        }
    }
}
