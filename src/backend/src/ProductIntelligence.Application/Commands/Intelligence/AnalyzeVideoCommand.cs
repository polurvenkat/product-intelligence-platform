using MediatR;
using Microsoft.AspNetCore.Http;

namespace ProductIntelligence.Application.Commands.Intelligence;

public record AnalyzeVideoCommand : IRequest<DocumentAnalysisResultDto>
{
    public IFormFile File { get; init; } = default!;
    public Guid? UserId { get; set; }
    public Guid? OrganizationId { get; set; }
}
