using Octokit;
using ProductIntelligence.Application.Interfaces.Infrastructure;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ProductIntelligence.Infrastructure.External;

public class GitHubService : IGitHubService
{
    private readonly IGitHubClient _githubClient;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(ILogger<GitHubService> logger)
    {
        _githubClient = new GitHubClient(new ProductHeaderValue("ProductIntelligencePlatform"));
        _logger = logger;
    }

    public async Task<string> GetRepositoryContentAsync(string repoUrl, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching GitHub repository content: {RepoUrl}", repoUrl);

        try
        {
            // Parse owner and repo from URL
            var (owner, repo) = ParseRepoUrl(repoUrl);
            
            var builder = new StringBuilder();
            builder.AppendLine($"Source: GitHub Repository {repoUrl}");
            builder.AppendLine();

            // 1. Get README
            try 
            {
                var readme = await _githubClient.Repository.Content.GetReadme(owner, repo);
                builder.AppendLine("--- README.md ---");
                builder.AppendLine(readme.Content);
                builder.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch README for {Owner}/{Repo}", owner, repo);
            }

            // 2. Get high-level file structure
            try 
            {
                var contents = await _githubClient.Repository.Content.GetAllContents(owner, repo);
                builder.AppendLine("--- REPOSITORY STRUCTURE ---");
                foreach (var item in contents)
                {
                    builder.AppendLine($"{(item.Type == ContentType.Dir ? "[DIR] " : "")}{item.Name}");
                }
                builder.AppendLine();
                
                // 3. Dig into 'docs' or 'documentation' folders if they exist
                var docsFolder = contents.FirstOrDefault(c => 
                    c.Type == ContentType.Dir && 
                    (c.Name.Equals("docs", StringComparison.OrdinalIgnoreCase) || 
                     c.Name.Equals("documentation", StringComparison.OrdinalIgnoreCase)));
                
                if (docsFolder != null)
                {
                    builder.AppendLine("--- DOCUMENTATION BITS ---");
                    var docsContents = await _githubClient.Repository.Content.GetAllContents(owner, repo, docsFolder.Path);
                    foreach (var doc in docsContents.Where(d => d.Type == ContentType.File && d.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).Take(3))
                    {
                        var detail = await _githubClient.Repository.Content.GetAllContents(owner, repo, doc.Path);
                        var fileContent = detail.FirstOrDefault();
                        if (fileContent != null)
                        {
                            builder.AppendLine($"File: {doc.Path}");
                            builder.AppendLine(fileContent.Content);
                            builder.AppendLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch contents for {Owner}/{Repo}", owner, repo);
            }

            return builder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch GitHub repository: {RepoUrl}", repoUrl);
            throw new InvalidOperationException($"Could not fetch GitHub repository content for {repoUrl}. Ensure it is a public repository.", ex);
        }
    }

    private (string owner, string repo) ParseRepoUrl(string url)
    {
        // Simple parser for github.com/owner/repo
        var uri = new Uri(url);
        var pathParts = uri.AbsolutePath.Trim('/').Split('/');
        if (pathParts.Length < 2)
        {
            throw new ArgumentException("Invalid GitHub URL. Expected format: https://github.com/owner/repository");
        }
        return (pathParts[0], pathParts[1]);
    }
}
