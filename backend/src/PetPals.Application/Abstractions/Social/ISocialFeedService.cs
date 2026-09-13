using PetPals.Application.DTOs.Social;

namespace PetPals.Application.Abstractions.Social;

public interface ISocialFeedService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PetDto>> GetMyPetsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PetDto> CreatePetAsync(Guid userId, CreatePetRequest request, CancellationToken cancellationToken = default);
    Task<PetDto?> UpdatePetAsync(Guid userId, Guid petId, UpdatePetRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePetAsync(Guid userId, Guid petId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FeedPostDto?> CreatePostAsync(Guid userId, CreatePostRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeletePostAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task<bool> AddLikeAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task<bool> RemoveLikeAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid postId, CancellationToken cancellationToken = default);
    Task<CommentDto?> AddCommentAsync(Guid userId, Guid postId, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<bool> FollowAsync(Guid followerUserId, Guid followedUserId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowAsync(Guid followerUserId, Guid followedUserId, CancellationToken cancellationToken = default);
}
