using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[Index(nameof(Code), IsUnique = true)]
public class Technique : DictionaryEntity
{
    public int CategoryId { get; set; }
    public TechniqueCategory Category { get; set; } = null!;
}
