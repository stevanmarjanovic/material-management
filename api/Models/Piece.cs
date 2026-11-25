namespace MaterialManagement.Api.Models;

public class Piece
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public int PurchaseId { get; set; }
    public List<Rect> Shape { get; set; } = [];
    // Cut pieces are kept (inactive) so the last cut can be undone.
    public bool Active { get; set; } = true;
}
