namespace MaterialManagement.Api.Services;

public static class PhotoService
{
    /// <summary>The uploaded image's bytes; 400 if it isn't an image or is over 10 MB.</summary>
    public static async Task<byte[]> Read(IFormFile file)
    {
        if (!file.ContentType.StartsWith("image/") || file.Length > 10_000_000)
            throw new BadRequestException("Only image files up to 10 MB.");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}
