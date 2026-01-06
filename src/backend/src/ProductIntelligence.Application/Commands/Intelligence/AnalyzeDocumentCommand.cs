using MediatR;
using Microsoft.AspNetCore.Http;

namespace ProductIntelligence.Application.Commands.Intelligence;

public record AnalyzeDocumentCommand : IRequest<DocumentAnalysisResultDto>
{
    public IFormFile File { get; init; } = default!;
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}

public record DocumentAnalysisResultDto
{
    public string Summary { get; init; } = string.Empty;
    public List<SuggestedDomainDto> SuggestedDomains { get; init; } = new();
}

public record SuggestedDomainDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentName { get; init; }
    public double ConfidenceScore { get; init; }
}
