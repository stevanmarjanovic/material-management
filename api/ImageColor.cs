using SkiaSharp;

namespace MaterialManagement.Api;

public static class ImageColor
{
    /// <summary>
    /// Average color (#rrggbb) of the center square of an image: half the shorter side, so background
    /// and shadows at the edges are skipped. Returns null if the image can't be decoded.
    /// </summary>
    public static string? AverageCenter(byte[] image)
    {
        // SKBitmap.Decode throws on unknown data; SKCodec.Create returns null instead.
        using var codec = SKCodec.Create(new SKMemoryStream(image));
        if (codec == null) return null;
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap == null) return null;

        var side = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 2);
        var x0 = (bitmap.Width - side) / 2;
        var y0 = (bitmap.Height - side) / 2;
        var step = Math.Max(1, side / 100); // ~100x100 samples is plenty for an average

        long r = 0, g = 0, b = 0, n = 0;
        for (var y = y0; y < y0 + side; y += step)
            for (var x = x0; x < x0 + side; x += step)
            {
                var c = bitmap.GetPixel(x, y);
                if (c.Alpha < 128) continue; // skip transparent pixels
                r += c.Red; g += c.Green; b += c.Blue; n++;
            }
        return n == 0 ? null : $"#{r / n:x2}{g / n:x2}{b / n:x2}";
    }
}
