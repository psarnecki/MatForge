using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[Index(nameof(Code), IsUnique = true)]
public class TrainingCharacter : DictionaryEntity
{
    public bool AllowsRounds { get; set; }
}
