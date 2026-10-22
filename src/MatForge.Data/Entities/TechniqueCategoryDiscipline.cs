using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[PrimaryKey(nameof(TechniqueCategoryId), nameof(DisciplineId))]
public class TechniqueCategoryDiscipline
{
    public int TechniqueCategoryId { get; set; }
    public TechniqueCategory TechniqueCategory { get; set; } = null!;
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
}
