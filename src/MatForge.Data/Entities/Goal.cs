using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class Goal : UserOwnedEntity
{
    public GoalType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? TargetValue { get; set; }
    public int? CompetitionId { get; set; }
    public Competition? Competition { get; set; }
    public int? ManualProgressPercent { get; set; }
    public bool IsArchived { get; set; }
}
