using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PetPals.Application.Abstractions.Chat;
using PetPals.Application.DTOs.Chat;
using System.Security.Claims;

namespace PetPals.API.Hubs;

[Authorize]
public sealed class ChatHub(IChatService chatService) : Hub
{
    public async Task JoinConversation(Guid conversationId)
    {
        if (!await chatService.CanAccessConversationAsync(CurrentUserId(), conversationId, Context.ConnectionAborted))
        {
            throw new HubException("Conversation not found.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
    }

    public async Task SendMessage(Guid conversationId, string content)
    {
        var result = await chatService.SendMessageAsync(CurrentUserId(), conversationId,
            new SendMessageRequest(content), Context.ConnectionAborted);
        if (!result.Succeeded)
        {
            throw new HubException(result.Error);
        }

        await Clients.Group(GroupName(conversationId)).SendAsync("MessageReceived", result.Data, Context.ConnectionAborted);
    }

    private Guid CurrentUserId() => Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string GroupName(Guid conversationId) => $"conversation:{conversationId}";
}
