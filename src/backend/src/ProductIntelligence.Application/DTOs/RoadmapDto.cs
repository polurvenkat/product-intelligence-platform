using System;

namespace ProductIntelligence.Application.DTOs
{
    public class RoadmapItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Quarter { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int SortOrder { get; set; }
        public string? ExternalId { get; set; }
        public int Progress { get; set; }
        public string? Color { get; set; }
        public DateTime? TargetDate { get; set; }
    }

    public class CreateRoadmapItemDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Quarter { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; }
        public int SortOrder { get; set; }
        public string? ExternalId { get; set; }
        public int Progress { get; set; }
        public string? Color { get; set; }
        public DateTime? TargetDate { get; set; }
    }

    public class UpdateRoadmapItemDto : CreateRoadmapItemDto
    {
        public int Id { get; set; }
    }
}
