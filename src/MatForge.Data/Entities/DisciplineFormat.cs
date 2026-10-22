using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// Format codes are unique within a discipline
[Index(nameof(DisciplineId), nameof(Code), IsUnique = true)]
public class DisciplineFormat : DictionaryEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
}
