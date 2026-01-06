namespace ProductIntelligence.Application.Interfaces.Infrastructure;

public interface IGitHubService
{
    Task<string> GetRepositoryContentAsync(string repoUrl, CancellationToken cancellationToken);
}
