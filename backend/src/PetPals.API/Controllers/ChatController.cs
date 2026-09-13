using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPals.Application.Abstractions.Chat;
using PetPals.Application.DTOs.Chat;
using System.Security.Claims;

namespace PetPals.API.Controllers;

[ApiController]
[Authorize]
[Route("api/chat")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> GetConversations(CancellationToken cancellationToken) =>
        Ok(await chatService.GetConversationsAsync(CurrentUserId(), cancellationToken));

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await chatService.CreateConversationAsync(CurrentUserId(), request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> GetMessages(Guid conversationId, CancellationToken cancellationToken) =>
        Ok(await chatService.GetMessagesAsync(CurrentUserId(), conversationId, cancellationToken));

    [HttpPost("conversations/{conversationId:guid}/messages")]
    public async Task<ActionResult<MessageDto>> SendMessage(Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await chatService.SendMessageAsync(CurrentUserId(), conversationId, request, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(result.Error);
    }

    private Guid CurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
