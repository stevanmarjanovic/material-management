namespace MaterialManagement.Api.Models;

public class Cut
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public int SourcePieceId { get; set; }
    public int PurchaseId { get; set; } // purchase of the piece it was cut from (for the cut's cost)
    public Purchase? Purchase { get; set; }
    public Material? Material { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public List<int> ResultPieceIds { get; set; } = [];
}
