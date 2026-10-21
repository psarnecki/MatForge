namespace MatForge.Data.Entities;

public class BodyMeasurement : UserOwnedEntity
{
    public DateOnly Date { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? BodyFatPercent { get; set; }
}
