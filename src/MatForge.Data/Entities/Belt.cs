namespace MatForge.Data.Entities;

public class Belt : DictionaryEntity
{
    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int Rank { get; set; }
    public int MinDegree { get; set; }
    public int MaxDegree { get; set; }
}
