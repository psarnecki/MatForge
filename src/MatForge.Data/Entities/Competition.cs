namespace MatForge.Data.Entities;

public class Competition : UserOwnedEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public string? Location { get; set; }
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int FormatId { get; set; }
    public DisciplineFormat Format { get; set; } = null!;
    public string? AgeCategory { get; set; }
    public string? SkillLevel { get; set; }
    public string? WeightCategory { get; set; }
    public int? Place { get; set; }
    public string? ResultNote { get; set; }
    public string? Notes { get; set; }

    public List<Fight> Fights { get; set; } = [];
}
