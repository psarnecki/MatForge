using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class TrainingSession : UserOwnedEntity
{
    public DateOnly Date { get; set; }
    public SessionKind Kind { get; set; }
    public int DurationMinutes { get; set; }
    public int Feeling { get; set; }
    public string? Notes { get; set; }

    public int? DisciplineId { get; set; }
    public Discipline? Discipline { get; set; }
    public int? FormatId { get; set; }
    public DisciplineFormat? Format { get; set; }
    public int? CharacterId { get; set; }
    public TrainingCharacter? Character { get; set; }
    public int? ClubId { get; set; }
    public Club? Club { get; set; }

    public int? ActivityTypeId { get; set; }
    public ActivityType? ActivityType { get; set; }

    public List<SparringRound> Rounds { get; set; } = [];
}
