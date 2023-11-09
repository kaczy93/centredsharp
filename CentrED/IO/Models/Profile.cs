using System.Text.Json.Serialization;

namespace CentrED.IO.Models
{
    public class Profile
    {
        [JsonIgnore]
        public string Name { get; set; }
        public string Hostname { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 2597;
        public string Username { get; set; } = "";
        public string ClientPath { get; set; } = "";
        public string ClientVersion { get; set; } = "";
        public Dictionary<string, RadarFavorite> RadarFavorites { get; set; } = new();
    }
}
