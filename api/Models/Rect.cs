namespace MaterialManagement.Api.Models;

/// <summary>Axis-aligned rectangle in cm. X = across the width, Y = along the length.</summary>
public record Rect(double X, double Y, double W, double H)
{
    public double R => X + W;
    public double B => Y + H;
    public double Area => W * H;

    public double OverlapArea(Rect o) =>
        Math.Max(0, Math.Min(R, o.R) - Math.Max(X, o.X)) * Math.Max(0, Math.Min(B, o.B) - Math.Max(Y, o.Y));
}
