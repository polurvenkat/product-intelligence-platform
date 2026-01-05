namespace ProductIntelligence.Application.DTOs.AzureDevOps;

public class WorkItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedTo { get; set; } = string.Empty;
    public string CurrentLane { get; set; } = string.Empty;
    public string BoardColumn { get; set; } = string.Empty;
    public double Effort { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ChangedDate { get; set; }
    public double TotalDaysInProgress { get; set; }
    public double TotalDaysSoFar { get; set; }
    public List<LaneDurationDto> LaneDurations { get; set; } = new();
}

public class LaneDurationDto
{
    public string LaneName { get; set; } = string.Empty;
    public double Duration { get; set; }
    public DateTime EnteredDate { get; set; }
    public DateTime? LeftDate { get; set; }
}

public class FeatureWorkItemsDto
{
    public int FeatureId { get; set; }
    public string FeatureTitle { get; set; } = string.Empty;
    public double TotalEffort { get; set; }
    public double TotalDaysSoFar { get; set; }
    public List<WorkItemDto> UserStories { get; set; } = new();
    public List<WorkItemDto> Bugs { get; set; } = new();
}
