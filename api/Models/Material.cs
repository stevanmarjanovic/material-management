namespace MaterialManagement.Api.Models;

/// <summary>A fabric (one name + color, bought in one store). Each purchase of it adds a piece.</summary>
public class Material
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public List<FiberPart>? Composition { get; set; }
    public double? RollWidthCm { get; set; }
    public int? WeightGsm { get; set; }
    public string? NeedleType { get; set; }
    public int? NeedleSize { get; set; }
    public string? Certificate { get; set; }
    public Wash? Wash { get; set; }
    public Bleach? Bleach { get; set; }
    public Iron? Iron { get; set; }
    public DryClean? DryClean { get; set; }
    public TumbleDry? TumbleDry { get; set; }
    public string? Link { get; set; }
    public int? StoreId { get; set; }
    public Store? Store { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
    public string? PreviewColor { get; set; } // average photo color, #rrggbb
    public List<Purchase> Purchases { get; set; } = [];
    public List<Piece> Pieces { get; set; } = [];
    public List<Cut> Cuts { get; set; } = [];
}
