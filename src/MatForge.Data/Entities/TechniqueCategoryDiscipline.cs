namespace MatForge.Data.Entities;

public class TechniqueCategoryDiscipline
{
    public int TechniqueCategoryId { get; set; }
    public TechniqueCategory TechniqueCategory { get; set; } = null!;
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
}
