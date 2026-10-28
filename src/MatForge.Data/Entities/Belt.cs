using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// Belt codes are unique within a discipline
[Index(nameof(DisciplineId), nameof(Code), IsUnique = true)]
public class Belt : DictionaryEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int Rank { get; set; }
    public int MinDegree { get; set; }
    public int MaxDegree { get; set; }
}
