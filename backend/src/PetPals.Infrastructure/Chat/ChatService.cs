using Microsoft.EntityFrameworkCore;
using PetPals.Application.Abstractions.Chat;
using PetPals.Application.DTOs.Chat;
using PetPals.Domain.Entities;
using PetPals.Infrastructure.Persistence;

namespace PetPals.Infrastructure.Chat;

public sealed class ChatService(ApplicationDbContext db) : IChatService
{
    public Task<bool> CanAccessConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default) =>
        IsParticipantAsync(userId, conversationId, cancellationToken);

    public async Task<IReadOnlyList<ConversationDto>> GetConversationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var conversationIds = db.ConversationParticipants.Where(item => item.UserId == userId).Select(item => item.ConversationId);
        var conversations = await db.Conversations.AsNoTracking().Where(item => conversationIds.Contains(item.Id))
            .OrderByDescending(item => item.LastMessageAtUtc).ToListAsync(cancellationToken);
        return await ToConversationDtos(conversations, cancellationToken);
    }

    public async Task<ChatResult<ConversationDto>> CreateConversationAsync(Guid userId, CreateConversationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ParticipantUserId == userId || !await db.Users.AnyAsync(user => user.Id == request.ParticipantUserId, cancellationToken))
        {
            return ChatResult<ConversationDto>.Failure("The participant is invalid.");
        }

        var existing = await db.Conversations.Where(conversation =>
                conversation.Participants.Any(participant => participant.UserId == userId) &&
                conversation.Participants.Any(participant => participant.UserId == request.ParticipantUserId))
            .Where(conversation => conversation.Participants.Count == 2)
            .SingleOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return ChatResult<ConversationDto>.Success((await ToConversationDtos([existing], cancellationToken)).Single());
        }

        var conversation = new Conversation
        {
            Participants =
            [
                new ConversationParticipant { UserId = userId },
                new ConversationParticipant { UserId = request.ParticipantUserId }
            ]
        };
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync(cancellationToken);
        return ChatResult<ConversationDto>.Success((await ToConversationDtos([conversation], cancellationToken)).Single());
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(userId, conversationId, cancellationToken))
        {
            return [];
        }

        return await (from message in db.Messages.AsNoTracking()
                      join user in db.Users.AsNoTracking() on message.SenderUserId equals user.Id
                      where message.ConversationId == conversationId
                      orderby message.SentAtUtc
                      select new MessageDto(message.Id, message.ConversationId, message.SenderUserId,
                          user.DisplayName, message.Content, message.SentAtUtc)).ToListAsync(cancellationToken);
    }

    public async Task<ChatResult<MessageDto>> SendMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!await IsParticipantAsync(userId, conversationId, cancellationToken) || string.IsNullOrWhiteSpace(request.Content))
        {
            return ChatResult<MessageDto>.Failure("The conversation or message is invalid.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderUserId = userId,
            Content = request.Content.Trim()
        };
        var conversation = await db.Conversations.SingleAsync(item => item.Id == conversationId, cancellationToken);
        conversation.LastMessageAtUtc = message.SentAtUtc;
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);
        var displayName = await db.Users.Where(user => user.Id == userId).Select(user => user.DisplayName).SingleAsync(cancellationToken);
        return ChatResult<MessageDto>.Success(new MessageDto(message.Id, conversationId, userId, displayName, message.Content, message.SentAtUtc));
    }

    private async Task<bool> IsParticipantAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken) =>
        await db.ConversationParticipants.AnyAsync(item => item.ConversationId == conversationId && item.UserId == userId, cancellationToken);

    private async Task<List<ConversationDto>> ToConversationDtos(IReadOnlyList<Conversation> conversations, CancellationToken cancellationToken)
    {
        var ids = conversations.Select(conversation => conversation.Id).ToArray();
        var participants = await (from participant in db.ConversationParticipants.AsNoTracking()
                                  join user in db.Users.AsNoTracking() on participant.UserId equals user.Id
                                  where ids.Contains(participant.ConversationId)
                                  select new { participant.ConversationId, Participant = new ChatParticipantDto(user.Id, user.DisplayName) })
            .ToListAsync(cancellationToken);
        return conversations.Select(conversation => new ConversationDto(conversation.Id, conversation.LastMessageAtUtc,
            participants.Where(item => item.ConversationId == conversation.Id).Select(item => item.Participant).ToList())).ToList();
    }
}
