using System.ComponentModel.DataAnnotations;
using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class Material : UserOwnedEntity
{
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Url { get; set; } = string.Empty;

    public MaterialSource Source { get; set; }
    public int? CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }
    public MaterialStatus Status { get; set; } = MaterialStatus.Saved;

    [MaxLength(2000)]
    public string? Notes { get; set; }
}
