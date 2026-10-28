using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MatForge.Data.Entities;

// Competitions are listed by user and date
[Index(nameof(UserId), nameof(Date))]
public class Competition : UserOwnedEntity
{
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    public int DisciplineId { get; set; }
    public Discipline Discipline { get; set; } = null!;
    public int FormatId { get; set; }
    public DisciplineFormat Format { get; set; } = null!;

    [MaxLength(50)]
    public string? AgeCategory { get; set; }

    [MaxLength(50)]
    public string? SkillLevel { get; set; }

    [MaxLength(50)]
    public string? WeightCategory { get; set; }

    public int? Place { get; set; }

    [MaxLength(100)]
    public string? ResultNote { get; set; }

    [MaxLength(4000)]
    public string? Notes { get; set; }

    public List<Fight> Fights { get; set; } = [];
}
