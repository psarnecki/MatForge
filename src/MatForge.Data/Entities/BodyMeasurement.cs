using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// One measurement per user per day
[Index(nameof(UserId), nameof(Date), IsUnique = true)]
public class BodyMeasurement : UserOwnedEntity
{
    public DateOnly Date { get; set; }

    [Precision(5, 2)]
    public decimal? WeightKg { get; set; }

    [Precision(5, 1)]
    public decimal? HeightCm { get; set; }

    [Precision(4, 1)]
    public decimal? BodyFatPercent { get; set; }
}
