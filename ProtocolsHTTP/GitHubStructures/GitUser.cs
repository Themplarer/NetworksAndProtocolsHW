using System;
using Newtonsoft.Json;

namespace Protocols.GitHubStructures
{
    public class GitUser
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }
    }
}
