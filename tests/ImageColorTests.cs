using MaterialManagement.Api;
using SkiaSharp;

public class ImageColorTests
{
    [Fact]
    public void AverageCenter_IgnoresTheEdges()
    {
        // 200x100 red image with a blue 60x60 center: the 50x50 center square is all blue.
        using var bitmap = new SKBitmap(200, 100);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.Red);
            canvas.DrawRect(70, 20, 60, 60, new SKPaint { Color = new SKColor(0x12, 0x34, 0xff) });
        }
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);

        Assert.Equal("#1234ff", ImageColor.AverageCenter(png.ToArray()));
    }

    [Fact]
    public void AverageCenter_ReturnsNullForNonImage() => Assert.Null(ImageColor.AverageCenter([1, 2, 3]));
}
