namespace PetPals.Application.DTOs.Chat;

public sealed record ConversationDto(Guid Id, DateTime? LastMessageAtUtc, IReadOnlyList<ChatParticipantDto> Participants);

public sealed record ChatParticipantDto(Guid UserId, string DisplayName);

public sealed record MessageDto(Guid Id, Guid ConversationId, Guid SenderUserId, string SenderDisplayName, string Content, DateTime SentAtUtc);

public sealed record CreateConversationRequest(Guid ParticipantUserId);

public sealed record SendMessageRequest(string Content);

public sealed record ChatResult<T>(bool Succeeded, T? Data, string? Error)
{
    public static ChatResult<T> Success(T data) => new(true, data, null);
    public static ChatResult<T> Failure(string error) => new(false, default, error);
}
