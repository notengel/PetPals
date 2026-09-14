using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Social;
using PetPals.Application.Abstractions.Storage;
using PetPals.Application.DTOs.Social;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/social")]
public sealed class SocialController(ISocialFeedService socialFeedService, IFileStorage files) : ControllerBase
{
    [HttpGet("profile/me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        return Ok(await socialFeedService.GetProfileAsync(CurrentUserId(), cancellationToken));
    }

    [HttpGet("profile/{userId:guid}")]
    public async Task<ActionResult<UserProfileDto>> GetProfile(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await socialFeedService.GetPublicProfileAsync(CurrentUserId(), userId, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("profile/me/posts")]
    public async Task<ActionResult<IReadOnlyList<FeedPostDto>>> GetMyPosts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var me = CurrentUserId();
        return Ok(await socialFeedService.GetUserPostsAsync(me, me, page, pageSize, cancellationToken));
    }

    [HttpPost("profile/me/avatar")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(IFormFile file, CancellationToken cancellationToken)
    {
        var me = CurrentUserId();
        var current = await socialFeedService.GetProfileAsync(me, cancellationToken);
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(await socialFeedService.UpdateProfileAsync(me, new UpdateProfileRequest
        {
            DisplayName = current.DisplayName, Bio = current.Bio, AvatarUrl = url,
            BannerUrl = current.BannerUrl, IsPublic = current.IsPublic,
            Latitude = current.Latitude, Longitude = current.Longitude
        }, cancellationToken));
    }

    [HttpPost("profile/me/banner")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<UserProfileDto>> UploadBanner(IFormFile file, CancellationToken cancellationToken)
    {
        var me = CurrentUserId();
        var current = await socialFeedService.GetProfileAsync(me, cancellationToken);
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        return Ok(await socialFeedService.UpdateProfileAsync(me, new UpdateProfileRequest
        {
            DisplayName = current.DisplayName, Bio = current.Bio, AvatarUrl = current.AvatarUrl,
            BannerUrl = url, IsPublic = current.IsPublic,
            Latitude = current.Latitude, Longitude = current.Longitude
        }, cancellationToken));
    }

    [HttpPut("profile/me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await socialFeedService.UpdateProfileAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpGet("pets")]
    public async Task<ActionResult<IReadOnlyList<PetDto>>> GetMyPets(CancellationToken cancellationToken)
    {
        return Ok(await socialFeedService.GetMyPetsAsync(CurrentUserId(), cancellationToken));
    }

    [HttpPost("pets")]
    public async Task<ActionResult<PetDto>> CreatePet(
        CreatePetRequest request,
        CancellationToken cancellationToken)
    {
        var pet = await socialFeedService.CreatePetAsync(CurrentUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetMyPets), pet);
    }

    [HttpPut("pets/{petId:guid}")]
    public async Task<ActionResult<PetDto>> UpdatePet(
        Guid petId,
        UpdatePetRequest request,
        CancellationToken cancellationToken)
    {
        var pet = await socialFeedService.UpdatePetAsync(CurrentUserId(), petId, request, cancellationToken);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpDelete("pets/{petId:guid}")]
    public async Task<IActionResult> DeletePet(Guid petId, CancellationToken cancellationToken)
    {
        return await socialFeedService.DeletePetAsync(CurrentUserId(), petId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("pets/{petId:guid}/photos")]
    public async Task<ActionResult<IReadOnlyList<PetPhotoDto>>> GetPetPhotos(Guid petId, CancellationToken cancellationToken) =>
        Ok(await socialFeedService.GetPetPhotosAsync(CurrentUserId(), petId, cancellationToken));

    [HttpPost("pets/{petId:guid}/photos")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<PetPhotoDto>> AddPetPhoto(Guid petId, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var url = await files.SaveAsync(stream, file.FileName, file.ContentType, cancellationToken);
        var photo = await socialFeedService.AddPetPhotoAsync(CurrentUserId(), petId, url, cancellationToken);
        return photo is null ? NotFound() : Ok(photo);
    }

    [HttpDelete("pets/{petId:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePetPhoto(Guid petId, Guid photoId, CancellationToken cancellationToken) =>
        await socialFeedService.DeletePetPhotoAsync(CurrentUserId(), petId, photoId, cancellationToken) ? NoContent() : NotFound();

    [HttpPut("pets/{petId:guid}/primary")]
    public async Task<ActionResult<PetDto>> SetPetPrimaryPhoto(Guid petId, SetPetPrimaryPhotoRequest request, CancellationToken cancellationToken)
    {
        var pet = await socialFeedService.SetPetPrimaryPhotoAsync(CurrentUserId(), petId, request.PhotoId, cancellationToken);
        return pet is null ? NotFound() : Ok(pet);
    }

    [HttpGet("feed")]
    public async Task<ActionResult<IReadOnlyList<FeedPostDto>>> GetFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await socialFeedService.GetFeedAsync(CurrentUserId(), page, pageSize, cancellationToken));
    }

    [HttpPost("posts")]
    public async Task<ActionResult<FeedPostDto>> CreatePost(
        CreatePostRequest request,
        CancellationToken cancellationToken)
    {
        var post = await socialFeedService.CreatePostAsync(CurrentUserId(), request, cancellationToken);
        return post is null ? NotFound("The selected pet does not belong to you.") : Ok(post);
    }

    [HttpDelete("posts/{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken cancellationToken)
    {
        return await socialFeedService.DeletePostAsync(CurrentUserId(), postId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("posts/{postId:guid}/likes")]
    public async Task<IActionResult> AddLike(Guid postId, CancellationToken cancellationToken)
    {
        return await socialFeedService.AddLikeAsync(CurrentUserId(), postId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("posts/{postId:guid}/likes")]
    public async Task<IActionResult> RemoveLike(Guid postId, CancellationToken cancellationToken)
    {
        return await socialFeedService.RemoveLikeAsync(CurrentUserId(), postId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpGet("posts/{postId:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(
        Guid postId,
        CancellationToken cancellationToken)
    {
        return Ok(await socialFeedService.GetCommentsAsync(postId, cancellationToken));
    }

    [HttpPost("posts/{postId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid postId,
        CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var comment = await socialFeedService.AddCommentAsync(
            CurrentUserId(), postId, request, cancellationToken);
        return comment is null ? NotFound() : Ok(comment);
    }

    [HttpPost("users/{userId:guid}/follow")]
    public async Task<IActionResult> Follow(Guid userId, CancellationToken cancellationToken)
    {
        return await socialFeedService.FollowAsync(CurrentUserId(), userId, cancellationToken)
            ? NoContent()
            : BadRequest("You cannot follow yourself or an account that does not exist.");
    }

    [HttpDelete("users/{userId:guid}/follow")]
    public async Task<IActionResult> Unfollow(Guid userId, CancellationToken cancellationToken)
    {
        return await socialFeedService.UnfollowAsync(CurrentUserId(), userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private Guid CurrentUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
