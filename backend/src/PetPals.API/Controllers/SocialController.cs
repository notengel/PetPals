using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Social;
using PetPals.Application.DTOs.Social;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/social")]
public sealed class SocialController(ISocialFeedService socialFeedService) : ControllerBase
{
    [HttpGet("profile/me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken cancellationToken)
    {
        return Ok(await socialFeedService.GetProfileAsync(CurrentUserId(), cancellationToken));
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
