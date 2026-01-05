using System;

namespace ProductIntelligence.Core.Entities
{
    public class RoadmapItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Quarter { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Priority { get; set; } // 1: Good to have, 2: Important, 3: Must have
        public int SortOrder { get; set; }
        public string? ExternalId { get; set; } // Azure DevOps Feature ID
        public int Progress { get; set; }
        public string? Color { get; set; }
        public DateTime? TargetDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
