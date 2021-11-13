using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Protocols.GitHubStructures;
// ReSharper disable MemberCanBePrivate.Global

namespace Protocols
{
    public class GitHubInteractor
    {
        private readonly string token;
        private readonly string username;
        private readonly HttpClient client = new();

        public GitHubInteractor(string token, string username)
        {
            this.token = Uri.EscapeUriString(token);
            this.username = Uri.EscapeUriString(username);
        }

        private HttpRequestMessage CreateGHRequest(string path)
        {
            var builder = new UriBuilder("https://api.github.com" + Uri.EscapeUriString(path)) { UserName = username };
            var request = new HttpRequestMessage(HttpMethod.Get, builder.Uri);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("User-Agent", "Very rare user-agent");
            request.Headers.Add("Authorization", $"token {token}");
            return request;
        }

        private async Task<IEnumerable<T>> ReadAllByPagesAsync<T>(string basePath)
        {
            var res = Enumerable.Empty<T>();
            var counter = 1;
            var hasNext = true;

            while (hasNext)
            {
                var request = CreateGHRequest($"{basePath}?per_page=100&page={counter}");
                var response = await client.SendAsync(request);
                var responseStr = Uri.UnescapeDataString(await response.Content.ReadAsStringAsync());

                if (!response.IsSuccessStatusCode)
                {
                    if (IsEmptyRepository(responseStr))
                        return res;
                    throw new ArgumentException(request.RequestUri.AbsoluteUri + response.ReasonPhrase);
                }

                var iterationStepCollection = JsonConvert.DeserializeObject<List<T>>(responseStr);
                hasNext = iterationStepCollection!.Count > 0;
                res = res.Concat(iterationStepCollection);
                counter++;
            }

            return res;
        }

        public async Task<User> GetUser(string user)
        {
            var request = CreateGHRequest($"/users/{user}");
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new ArgumentException(response.ReasonPhrase);

            var responseStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<User>(responseStr);
        }

        private async Task<IEnumerable<Repository>> GetRepositoriesInternal(string subPath) =>
            await ReadAllByPagesAsync<Repository>($"/{subPath}/repos");

        public async Task<IEnumerable<Repository>> GetOrganisationRepositories(string organisationName) =>
            await GetRepositoriesInternal($"orgs/{organisationName}");

        public async Task<IEnumerable<Repository>> GetUserRepositories(string userName) =>
            await GetRepositoriesInternal($"users/{userName}");

        public async Task<IEnumerable<Repository>> GetRepositories(User user) =>
            await GetRepositoriesInternal($"{(user.Type == "orgs" ? "orgs" : "users")}/{user.Login}");

        public async Task<Repository> GetRepository(string owner, string repositoryName)
        {
            var request = CreateGHRequest($"/repos/{owner}/{repositoryName}");
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new ArgumentException(response.ReasonPhrase);

            var responseStr = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Repository>(responseStr);
        }

        public async Task<Repository> GetRepository(User user, string repositoryName) =>
            await GetRepository(user.Login, repositoryName);

        public async Task<IEnumerable<Commit>> GetCommits(string owner, string repositoryName) =>
            await ReadAllByPagesAsync<Commit>($"/repos/{owner}/{repositoryName}/commits");

        public async Task<IEnumerable<Commit>> GetCommits(Repository repository) =>
            await GetCommits(repository.Owner.Login, repository.Name);

        public async Task<IEnumerable<Commit>> GetCommits(User user, string repositoryName) =>
            await GetCommits(user.Login, repositoryName);

        private static bool IsEmptyRepository(string str) =>
            JsonConvert.DeserializeObject<ErrorResponse>(str)!.Message.Contains("empty");

        private class ErrorResponse
        {
            [JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}
