using System.ComponentModel.DataAnnotations;

namespace MatForge.Data.Entities;

public abstract class DictionaryEntity
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
