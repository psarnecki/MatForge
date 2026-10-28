using System.ComponentModel.DataAnnotations;
using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class Fight
{
    public int Id { get; set; }
    public int CompetitionId { get; set; }
    public Competition Competition { get; set; } = null!;
    public int Order { get; set; }

    [MaxLength(100)]
    public string? OpponentName { get; set; }

    public FightResult Result { get; set; }
    public FightMethod? Method { get; set; }
    public int? TechniqueId { get; set; }
    public Technique? Technique { get; set; }
    public int? DurationSeconds { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
