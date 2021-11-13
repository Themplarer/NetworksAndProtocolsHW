using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms.DataVisualization.Charting;

namespace Protocols
{
    public static class Program
    {
        private static string token;
        private static string username;

        private static async Task Main()
        {
            using (var f = new StreamReader(File.OpenRead("../../secret.txt")))
            {
                token = await f.ReadLineAsync();
                username = await f.ReadLineAsync();
            }

            MakeChart(await GetMostActiveContributorsInOrganizationAsync("skbkontur"));
        }

        private static async Task<IReadOnlyCollection<(string, int)>> GetMostActiveContributorsInOrganizationAsync(
            string orgName, int limit = 10)
        {
            var commitsByEmail = new Dictionary<string, int>();

            var interactor = new GitHubInteractor(token, username);
            var reps = await interactor.GetOrganisationRepositories(orgName);

            foreach (var repo in reps)
            foreach (var commit in await interactor.GetCommits(repo))
                if (!commit.GitCommit.Message.Contains("Merge pull request"))
                {
                    var email = commit.GitCommit.Author.Email;
                    commitsByEmail.TryGetValue(email, out var res);
                    commitsByEmail[email] = res + 1;
                }

            return commitsByEmail
                .OrderByDescending(p => p.Value)
                .Take(limit)
                .Select(p => (p.Key, p.Value))
                .ToArray();
        }

        private static void MakeChart(IReadOnlyCollection<(string, int)> items)
        {
            var chart = new Chart();
            chart.ChartAreas.Add(new ChartArea("Default"));
            chart.Width = 1000;
            chart.Height = 800;
            chart.Titles.Add($"Top-{items.Count()} most active users");
            chart.Series.Add(new Series("Data"));

            foreach (var (item1, item2) in items)
                chart.Series["Data"].Points.AddXY(item1, item2);

            chart.SaveImage("chart.png", ChartImageFormat.Png);
        }
    }
}