using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

[Index(nameof(Code), IsUnique = true)]
public class Position : DictionaryEntity;
