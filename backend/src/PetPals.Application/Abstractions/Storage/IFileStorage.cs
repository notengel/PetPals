namespace PetPals.Application.Abstractions.Storage;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
}

// ponytail: local disk in dev, swap for S3/Cloudinary impl if prod needs it.
