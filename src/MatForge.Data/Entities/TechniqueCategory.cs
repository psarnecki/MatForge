namespace MatForge.Data.Entities;

public class TechniqueCategory : DictionaryEntity
{
    public List<TechniqueCategoryDiscipline> Disciplines { get; set; } = [];
}
