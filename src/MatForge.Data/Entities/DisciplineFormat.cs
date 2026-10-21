namespace MatForge.Data.Entities;

public class DisciplineFormat : DictionaryEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
}
