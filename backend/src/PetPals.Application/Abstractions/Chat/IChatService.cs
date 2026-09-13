using PetPals.Application.DTOs.Chat;

namespace PetPals.Application.Abstractions.Chat;

public interface IChatService
{
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ChatResult<ConversationDto>> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default);
    Task<ChatResult<MessageDto>> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default);
}
