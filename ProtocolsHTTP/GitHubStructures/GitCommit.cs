using Newtonsoft.Json;

namespace Protocols.GitHubStructures
{
    public class GitCommit
    {
        [JsonProperty("author")]
        public GitUser Author { get; set; }

        [JsonProperty("committer")]
        public GitUser Committer { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
