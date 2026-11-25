using MaterialManagement.Api.Domain;
using MaterialManagement.Api.Models;

public class GeometryTests
{
    static readonly List<Rect> Fabric = [new(0, 0, 150, 200)];

    [Fact]
    public void HoleInMiddle_KeepsOnePieceWithRightArea()
    {
        var (pieces, error) = Geometry.Cut(Fabric, new(50, 50, 20, 30));
        Assert.Null(error);
        Assert.Single(pieces!);
        Assert.Equal(150 * 200 - 20 * 30, Geometry.Area(pieces![0]), 6);
    }

    [Fact]
    public void StripAcrossWidth_SplitsIntoTwoNormalizedPieces()
    {
        var (pieces, _) = Geometry.Cut(Fabric, new(0, 80, 150, 40));
        Assert.Equal(2, pieces!.Count);
        Assert.All(pieces, p => Assert.Equal(0, p.Min(r => r.Y)));
        Assert.Equal([150 * 80, 150 * 80], pieces.Select(Geometry.Area).Order());
    }

    [Fact]
    public void CutOverMissingArea_IsRejected()
    {
        var (lShape, _) = Geometry.Cut(Fabric, new(0, 0, 50, 50));
        var (pieces, error) = Geometry.Cut(lShape![0], new(40, 0, 20, 20));
        Assert.Null(pieces);
        Assert.NotNull(error);
    }

    [Fact]
    public void CuttingEverything_LeavesNoPieces()
    {
        var (pieces, error) = Geometry.Cut(Fabric, new(0, 0, 150, 200));
        Assert.Null(error);
        Assert.Empty(pieces!);
    }
}
