namespace FashionStore.Application.Dtos.Response;

public sealed record MainCarouselResponse(string Id, string Title, string? Subtitle, string ButtonText, string? LinkUrl, string ImageUrl);
public sealed record MainCarouselImageResponse(byte[] Data, string ContentType, string? FileName);
