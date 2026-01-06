using MediatR;

namespace ProductIntelligence.Application.Commands.Intelligence;

public record AnalyzeGitHubRepoCommand : IRequest<DocumentAnalysisResultDto>
{
    public string RepoUrl { get; init; } = string.Empty;
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}
