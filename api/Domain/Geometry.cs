namespace MaterialManagement.Api;

/// <summary>Axis-aligned rectangle in cm. X = across the width, Y = along the length.</summary>
public record Rect(double X, double Y, double W, double H)
{
    public double R => X + W;
    public double B => Y + H;
    public double Area => W * H;

    public double OverlapArea(Rect o) =>
        Math.Max(0, Math.Min(R, o.R) - Math.Max(X, o.X)) * Math.Max(0, Math.Min(B, o.B) - Math.Max(Y, o.Y));
}

/// <summary>A piece's shape is a set of non-overlapping rectangles, so any 90° outline works.</summary>
public static class Geometry
{
    const double Eps = 1e-6;

    public static double Area(IEnumerable<Rect> shape) => shape.Sum(r => r.Area);

    /// <summary>
    /// Removes <paramref name="cut"/> from the shape. Returns the leftover pieces (one per
    /// connected region, each shifted so its top-left is 0,0), or an error message.
    /// </summary>
    public static (List<List<Rect>>? pieces, string? error) Cut(List<Rect> shape, Rect cut)
    {
        if (cut.W <= 0 || cut.H <= 0) return (null, "Cut width and height must be > 0.");
        var onFabric = shape.Sum(r => r.OverlapArea(cut));
        if (Math.Abs(onFabric - cut.Area) > Eps) return (null, "The cut must lie completely on the fabric.");

        var left = shape.SelectMany(r => Subtract(r, cut)).ToList();
        return (Components(left).Select(Normalize).ToList(), null);
    }

    // Rectangle minus rectangle = up to 4 rectangles (top band, bottom band, left/right of the cut).
    static IEnumerable<Rect> Subtract(Rect r, Rect c)
    {
        if (r.OverlapArea(c) <= 0) { yield return r; yield break; }
        if (c.Y > r.Y) yield return new(r.X, r.Y, r.W, c.Y - r.Y);
        if (c.B < r.B) yield return new(r.X, c.B, r.W, r.B - c.B);
        var y0 = Math.Max(r.Y, c.Y);
        var y1 = Math.Min(r.B, c.B);
        if (c.X > r.X) yield return new(r.X, y0, c.X - r.X, y1 - y0);
        if (c.R < r.R) yield return new(c.R, y0, r.R - c.R, y1 - y0);
    }

    // Two rectangles are connected when they share an edge segment of positive length.
    static bool Touch(Rect a, Rect b)
    {
        var yOverlap = Math.Min(a.B, b.B) - Math.Max(a.Y, b.Y) > Eps;
        var xOverlap = Math.Min(a.R, b.R) - Math.Max(a.X, b.X) > Eps;
        return (yOverlap && (Math.Abs(a.R - b.X) < Eps || Math.Abs(b.R - a.X) < Eps))
            || (xOverlap && (Math.Abs(a.B - b.Y) < Eps || Math.Abs(b.B - a.Y) < Eps));
    }

    static List<List<Rect>> Components(List<Rect> rects)
    {
        var groups = new List<List<Rect>>();
        var seen = new bool[rects.Count];
        for (var i = 0; i < rects.Count; i++)
        {
            if (seen[i]) continue;
            var group = new List<Rect>();
            var stack = new Stack<int>([i]);
            seen[i] = true;
            while (stack.Count > 0)
            {
                var cur = rects[stack.Pop()];
                group.Add(cur);
                for (var j = 0; j < rects.Count; j++)
                    if (!seen[j] && Touch(cur, rects[j])) { seen[j] = true; stack.Push(j); }
            }
            groups.Add(group);
        }
        return groups;
    }

    static List<Rect> Normalize(List<Rect> shape)
    {
        var dx = shape.Min(r => r.X);
        var dy = shape.Min(r => r.Y);
        return shape.Select(r => r with { X = r.X - dx, Y = r.Y - dy }).ToList();
    }
}
