using Newtonsoft.Json;

namespace Protocols.GitHubStructures
{
    public class Commit
    {
        [JsonProperty("sha")]
        public string Id { get; set; }

        [JsonProperty("author")]
        public User Author { get; set; }

        [JsonProperty("committer")]
        public User Committer { get; set; }

        [JsonProperty("commit")]
        public GitCommit GitCommit { get; set; }
    }
}
