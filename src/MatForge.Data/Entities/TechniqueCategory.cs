using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[Index(nameof(Code), IsUnique = true)]
public class TechniqueCategory : DictionaryEntity
{
    public List<TechniqueCategoryDiscipline> Disciplines { get; set; } = [];
}
