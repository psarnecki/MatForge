using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[PrimaryKey(nameof(PositionId), nameof(DisciplineId))]
public class PositionDiscipline
{
    public int PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
}
