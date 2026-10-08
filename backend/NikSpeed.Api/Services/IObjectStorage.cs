namespace NikSpeed.Api.Services;

public record StoredMedia(string Key, string ContentType);

public interface IObjectStorage
{
    Task<StoredMedia> SaveAsync(IFormFile file, string key, string contentType, CancellationToken cancellationToken);
    Task<(Stream Content, string ContentType)?> OpenReadAsync(string key, CancellationToken cancellationToken);
}
