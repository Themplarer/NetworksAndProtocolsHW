using System;
using Newtonsoft.Json;

namespace Protocols.GitHubStructures
{
    public class User
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("site_admin")]
        public bool IsSiteAdmin { get; set; }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("hireable")]
        public bool? IsHireable { get; set; }

        [JsonProperty("bio")]
        public string Bio { get; set; }

        [JsonProperty("public_repos")]
        public int PublicReposCount { get; set; }

        [JsonProperty("public_gists")]
        public int PublicGistsCount { get; set; }

        [JsonProperty("followers")]
        public int FollowersCount { get; set; }

        [JsonProperty("following")]
        public int FollowingCount { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}
