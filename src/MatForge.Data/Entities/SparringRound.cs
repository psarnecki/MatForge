using System.ComponentModel.DataAnnotations;
using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class SparringRound
{
    public int Id { get; set; }
    public int TrainingSessionId { get; set; }
    public TrainingSession TrainingSession { get; set; } = null!;
    public int Order { get; set; }
    public RoundResult Result { get; set; }
    public int? TechniqueId { get; set; }
    public Technique? Technique { get; set; }
    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
