using PetPals.Application.Abstractions.Storage;

namespace PetPals.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!Allowed.Contains(ext))
            throw new InvalidOperationException("Only image files (jpg, png, webp, gif) are allowed.");
        if (content.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("Image must be 5MB or less.");

        // ponytail: no IWebHostEnvironment here (classlib); wwwroot lives under the API output dir.
        var dir = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var file = File.Create(Path.Combine(dir, name));
        if (content.CanSeek) content.Seek(0, SeekOrigin.Begin);
        await content.CopyToAsync(file, cancellationToken);
        return $"/uploads/{name}";
    }
}
