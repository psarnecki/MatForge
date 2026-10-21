using MatForge.Data.Enums;

namespace MatForge.Data.Entities;

public class Material : UserOwnedEntity
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public MaterialSource Source { get; set; }
    public int? CategoryId { get; set; }
    public MaterialCategory? Category { get; set; }
    public MaterialStatus Status { get; set; } = MaterialStatus.Saved;
    public string? Notes { get; set; }
}
