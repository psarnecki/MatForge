namespace MatForge.Data.Entities;

public class Technique : DictionaryEntity
{
    public int CategoryId { get; set; }
    public TechniqueCategory Category { get; set; } = null!;
}
