using Microsoft.EntityFrameworkCore;
using PetPals.Application.Abstractions.Social;
using PetPals.Application.DTOs.Social;
using PetPals.Domain.Entities;
using PetPals.Infrastructure.Identity;
using PetPals.Infrastructure.Persistence;

namespace PetPals.Infrastructure.Social;

public sealed class SocialFeedService(ApplicationDbContext db) : ISocialFeedService
{
    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().SingleAsync(user => user.Id == userId, cancellationToken);
        var profile = await db.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        return profile is null
            ? new UserProfileDto(userId, user.DisplayName, null, null, null, true, null, null)
            : ToDto(profile);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleAsync(user => user.Id == userId, cancellationToken);
        var profile = await db.UserProfiles
            .SingleOrDefaultAsync(profile => profile.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            db.UserProfiles.Add(profile);
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.Bio = request.Bio?.Trim();
        profile.AvatarUrl = request.AvatarUrl?.Trim();
        profile.BannerUrl = request.BannerUrl?.Trim();
        if (request.IsPublic.HasValue) profile.IsPublic = request.IsPublic.Value;
        profile.Latitude = request.Latitude;
        profile.Longitude = request.Longitude;
        user.DisplayName = profile.DisplayName;

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(profile);
    }

    public async Task<UserProfileDto?> GetPublicProfileAsync(
        Guid viewerUserId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == targetUserId, cancellationToken);
        if (profile is null)
        {
            var user = await db.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == targetUserId, cancellationToken);
            return user is null ? null : new UserProfileDto(targetUserId, user.DisplayName, null, null, null, true, null, null);
        }
        if (!profile.IsPublic && viewerUserId != targetUserId) return null;
        return ToDto(profile);
    }

    public async Task<IReadOnlyList<FeedPostDto>> GetUserPostsAsync(
        Guid viewerUserId,
        Guid targetUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var profile = await db.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == targetUserId, cancellationToken);
        if (profile is not null && !profile.IsPublic && viewerUserId != targetUserId)
            return [];
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        return await QueryPosts(viewerUserId).Where(p => p.AuthorUserId == targetUserId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PetDto>> GetMyPetsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await db.Pets.AsNoTracking()
            .Where(pet => pet.OwnerUserId == userId)
            .OrderBy(pet => pet.Name)
            .Select(pet => new PetDto(
                pet.Id,
                pet.OwnerUserId,
                pet.Name,
                pet.Species,
                pet.Breed,
                pet.BirthDate,
                pet.Sex,
                pet.Description,
                pet.PrimaryImageUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<PetDto> CreatePetAsync(
        Guid userId,
        CreatePetRequest request,
        CancellationToken cancellationToken = default)
    {
        var pet = new Pet
        {
            OwnerUserId = userId,
            Name = request.Name.Trim(),
            Species = request.Species.Trim(),
            Breed = request.Breed?.Trim(),
            BirthDate = request.BirthDate,
            Sex = request.Sex?.Trim(),
            Description = request.Description?.Trim(),
            PrimaryImageUrl = request.PrimaryImageUrl?.Trim()
        };

        db.Pets.Add(pet);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(pet);
    }

    public async Task<PetDto?> UpdatePetAsync(
        Guid userId,
        Guid petId,
        UpdatePetRequest request,
        CancellationToken cancellationToken = default)
    {
        var pet = await db.Pets.SingleOrDefaultAsync(
            pet => pet.Id == petId && pet.OwnerUserId == userId,
            cancellationToken);

        if (pet is null)
        {
            return null;
        }

        pet.Name = request.Name.Trim();
        pet.Species = request.Species.Trim();
        pet.Breed = request.Breed?.Trim();
        pet.BirthDate = request.BirthDate;
        pet.Sex = request.Sex?.Trim();
        pet.Description = request.Description?.Trim();
        pet.PrimaryImageUrl = request.PrimaryImageUrl?.Trim();

        await db.SaveChangesAsync(cancellationToken);
        return ToDto(pet);
    }

    public async Task<bool> DeletePetAsync(
        Guid userId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        var pet = await db.Pets.SingleOrDefaultAsync(
            pet => pet.Id == petId && pet.OwnerUserId == userId,
            cancellationToken);

        if (pet is null)
        {
            return false;
        }

        db.Pets.Remove(pet);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PetPhotoDto>> GetPetPhotosAsync(
        Guid userId,
        Guid petId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsPetAsync(userId, petId, cancellationToken)) return [];
        return await db.PetPhotos.AsNoTracking()
            .Where(photo => photo.PetId == petId)
            .OrderByDescending(photo => photo.CreatedAtUtc)
            .Select(photo => new PetPhotoDto(photo.Id, photo.PetId, photo.ImageUrl, photo.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PetPhotoDto?> AddPetPhotoAsync(
        Guid userId,
        Guid petId,
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        var pet = await db.Pets.SingleOrDefaultAsync(p => p.Id == petId && p.OwnerUserId == userId, cancellationToken);
        if (pet is null) return null;
        var photo = new PetPhoto { PetId = petId, ImageUrl = imageUrl.Trim() };
        db.PetPhotos.Add(photo);
        if (string.IsNullOrWhiteSpace(pet.PrimaryImageUrl)) pet.PrimaryImageUrl = photo.ImageUrl; // ponytail: primera foto = perfil
        await db.SaveChangesAsync(cancellationToken);
        return new PetPhotoDto(photo.Id, photo.PetId, photo.ImageUrl, photo.CreatedAtUtc);
    }

    public async Task<bool> DeletePetPhotoAsync(
        Guid userId,
        Guid petId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        if (!await OwnsPetAsync(userId, petId, cancellationToken)) return false;
        var photo = await db.PetPhotos.SingleOrDefaultAsync(p => p.Id == photoId && p.PetId == petId, cancellationToken);
        if (photo is null) return false;
        db.PetPhotos.Remove(photo);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PetDto?> SetPetPrimaryPhotoAsync(
        Guid userId,
        Guid petId,
        Guid photoId,
        CancellationToken cancellationToken = default)
    {
        var pet = await db.Pets.SingleOrDefaultAsync(p => p.Id == petId && p.OwnerUserId == userId, cancellationToken);
        var photo = await db.PetPhotos.AsNoTracking().SingleOrDefaultAsync(p => p.Id == photoId && p.PetId == petId, cancellationToken);
        if (pet is null || photo is null) return null;
        pet.PrimaryImageUrl = photo.ImageUrl;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(pet);
    }

    private async Task<bool> OwnsPetAsync(Guid userId, Guid petId, CancellationToken cancellationToken) =>
        await db.Pets.AnyAsync(pet => pet.Id == petId && pet.OwnerUserId == userId, cancellationToken);

    public async Task<IReadOnlyList<FeedPostDto>> GetFeedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        return await (
            from post in db.Posts.AsNoTracking()
            join author in db.Users.AsNoTracking() on post.AuthorUserId equals author.Id
            join pet in db.Pets.AsNoTracking() on post.PetId equals pet.Id into petGroup
            from pet in petGroup.DefaultIfEmpty()
            where post.IsVisible
            orderby post.CreatedAtUtc descending
            select new FeedPostDto(
                post.Id,
                post.AuthorUserId,
                author.DisplayName,
                post.PetId,
                pet == null ? null : pet.Name,
                post.Content,
                post.MediaUrl,
                post.CreatedAtUtc,
                db.PostLikes.Count(like => like.PostId == post.Id),
                db.Comments.Count(comment => comment.PostId == post.Id),
                db.PostLikes.Any(like => like.PostId == post.Id && like.UserId == userId)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<FeedPostDto?> CreatePostAsync(
        Guid userId,
        CreatePostRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PetId is not null && !await db.Pets.AnyAsync(
                pet => pet.Id == request.PetId && pet.OwnerUserId == userId,
                cancellationToken))
        {
            return null;
        }

        var post = new Post
        {
            AuthorUserId = userId,
            PetId = request.PetId,
            Content = request.Content.Trim(),
            MediaUrl = request.MediaUrl?.Trim()
        };

        db.Posts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        return await GetPostAsync(userId, post.Id, cancellationToken);
    }

    public async Task<bool> DeletePostAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await db.Posts.SingleOrDefaultAsync(
            post => post.Id == postId && post.AuthorUserId == userId,
            cancellationToken);

        if (post is null)
        {
            return false;
        }

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AddLikeAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Posts.AnyAsync(post => post.Id == postId && post.IsVisible, cancellationToken))
        {
            return false;
        }

        if (!await db.PostLikes.AnyAsync(
                like => like.PostId == postId && like.UserId == userId,
                cancellationToken))
        {
            db.PostLikes.Add(new PostLike { PostId = postId, UserId = userId });
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RemoveLikeAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var like = await db.PostLikes.SingleOrDefaultAsync(
            like => like.PostId == postId && like.UserId == userId,
            cancellationToken);

        if (like is null)
        {
            return false;
        }

        db.PostLikes.Remove(like);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from comment in db.Comments.AsNoTracking()
            join user in db.Users.AsNoTracking() on comment.UserId equals user.Id
            where comment.PostId == postId
            orderby comment.CreatedAtUtc
            select new CommentDto(
                comment.Id,
                comment.UserId,
                user.DisplayName,
                comment.Content,
                comment.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CommentDto?> AddCommentAsync(
        Guid userId,
        Guid postId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Posts.AnyAsync(post => post.Id == postId && post.IsVisible, cancellationToken))
        {
            return null;
        }

        var comment = new Comment
        {
            PostId = postId,
            UserId = userId,
            Content = request.Content.Trim()
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync(cancellationToken);

        var user = await db.Users.AsNoTracking().SingleAsync(
            currentUser => currentUser.Id == userId,
            cancellationToken);
        return new CommentDto(comment.Id, userId, user.DisplayName, comment.Content, comment.CreatedAtUtc);
    }

    public async Task<bool> FollowAsync(
        Guid followerUserId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        if (followerUserId == followedUserId || !await db.Users.AnyAsync(
                user => user.Id == followedUserId,
                cancellationToken))
        {
            return false;
        }

        if (!await db.Follows.AnyAsync(
                follow => follow.FollowerUserId == followerUserId && follow.FollowedUserId == followedUserId,
                cancellationToken))
        {
            db.Follows.Add(new Follow
            {
                FollowerUserId = followerUserId,
                FollowedUserId = followedUserId
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> UnfollowAsync(
        Guid followerUserId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        var follow = await db.Follows.SingleOrDefaultAsync(
            currentFollow => currentFollow.FollowerUserId == followerUserId &&
                             currentFollow.FollowedUserId == followedUserId,
            cancellationToken);

        if (follow is null)
        {
            return false;
        }

        db.Follows.Remove(follow);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<FeedPostDto?> GetPostAsync(
        Guid userId,
        Guid postId,
        CancellationToken cancellationToken)
    {
        return await (
            from post in db.Posts.AsNoTracking()
            join author in db.Users.AsNoTracking() on post.AuthorUserId equals author.Id
            join pet in db.Pets.AsNoTracking() on post.PetId equals pet.Id into petGroup
            from pet in petGroup.DefaultIfEmpty()
            where post.Id == postId
            select new FeedPostDto(
                post.Id,
                post.AuthorUserId,
                author.DisplayName,
                post.PetId,
                pet == null ? null : pet.Name,
                post.Content,
                post.MediaUrl,
                post.CreatedAtUtc,
                db.PostLikes.Count(like => like.PostId == post.Id),
                db.Comments.Count(comment => comment.PostId == post.Id),
                db.PostLikes.Any(like => like.PostId == post.Id && like.UserId == userId)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<FeedPostDto> QueryPosts(Guid userId) =>
        from post in db.Posts.AsNoTracking()
        join author in db.Users.AsNoTracking() on post.AuthorUserId equals author.Id
        join pet in db.Pets.AsNoTracking() on post.PetId equals pet.Id into petGroup
        from pet in petGroup.DefaultIfEmpty()
        where post.IsVisible
        select new FeedPostDto(
            post.Id,
            post.AuthorUserId,
            author.DisplayName,
            post.PetId,
            pet == null ? null : pet.Name,
            post.Content,
            post.MediaUrl,
            post.CreatedAtUtc,
            db.PostLikes.Count(like => like.PostId == post.Id),
            db.Comments.Count(comment => comment.PostId == post.Id),
            db.PostLikes.Any(like => like.PostId == post.Id && like.UserId == userId));

    private static UserProfileDto ToDto(UserProfile profile) =>
        new(profile.UserId, profile.DisplayName, profile.Bio, profile.AvatarUrl, profile.BannerUrl, profile.IsPublic, profile.Latitude, profile.Longitude);

    private static PetDto ToDto(Pet pet) =>
        new(pet.Id, pet.OwnerUserId, pet.Name, pet.Species, pet.Breed, pet.BirthDate, pet.Sex, pet.Description, pet.PrimaryImageUrl);

}
