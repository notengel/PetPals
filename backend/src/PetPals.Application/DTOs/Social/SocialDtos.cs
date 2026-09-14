using System.ComponentModel.DataAnnotations;

namespace PetPals.Application.DTOs.Social;

public sealed record UserProfileDto(
    Guid UserId,
    string DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? BannerUrl,
    bool IsPublic,
    double? Latitude,
    double? Longitude);

public sealed class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string DisplayName { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; init; }

    [MaxLength(500)]
    public string? AvatarUrl { get; init; }

    [MaxLength(500)]
    public string? BannerUrl { get; init; }

    public bool? IsPublic { get; init; }

    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public sealed record PetDto(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string Species,
    string? Breed,
    DateOnly? BirthDate,
    string? Sex,
    string? Description,
    string? PrimaryImageUrl);

public sealed record PetPhotoDto(Guid Id, Guid PetId, string ImageUrl, DateTime CreatedAtUtc);

public sealed class SetPetPrimaryPhotoRequest
{
    [Required]
    public Guid PhotoId { get; init; }
}

public class PetRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Species { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? Breed { get; init; }

    public DateOnly? BirthDate { get; init; }

    [MaxLength(30)]
    public string? Sex { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(500)]
    public string? PrimaryImageUrl { get; init; }
}

public sealed class CreatePetRequest : PetRequest;

public sealed class UpdatePetRequest : PetRequest;

public sealed class CreatePostRequest
{
    [Required]
    [MaxLength(2000)]
    public string Content { get; init; } = string.Empty;

    public Guid? PetId { get; init; }

    [MaxLength(500)]
    public string? MediaUrl { get; init; }
}

public sealed record FeedPostDto(
    Guid Id,
    Guid AuthorUserId,
    string AuthorDisplayName,
    Guid? PetId,
    string? PetName,
    string Content,
    string? MediaUrl,
    DateTime CreatedAtUtc,
    int LikesCount,
    int CommentsCount,
    bool IsLiked);

public sealed record CommentDto(
    Guid Id,
    Guid UserId,
    string UserDisplayName,
    string Content,
    DateTime CreatedAtUtc);

public sealed class CreateCommentRequest
{
    [Required]
    [MaxLength(1000)]
    public string Content { get; init; } = string.Empty;
}
