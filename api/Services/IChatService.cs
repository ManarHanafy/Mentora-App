using api.Contracts.Chats;
using api.Contracts.Common;

namespace api.Services;

public interface IChatService
{
    Task<int> CreateChatAsync(int userId, CancellationToken cancellationToken = default);
    Task<ChatResponse> SendMessageAsync(int userId, int chatId, string message, CancellationToken cancellationToken = default);
    Task<ChatDetailsResponse?> GetChatByIdAsync(int chatId, int userId, CancellationToken cancellationToken = default);
    Task<ChatMessageDetailsResponse?> GetMessageByIdAsync(int chatId, int messageId, int userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<ChatHistoryResponse>> GetUserChatsAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> EndChatAsync(int chatId, int userId, CancellationToken cancellationToken = default);
    Task<int> EndInactiveChatsAsync(int inactivityMinutes, CancellationToken cancellationToken = default);
    Task<ChatSummarizeResult?> SummarizeChatAsync(int chatId, int userId, CancellationToken cancellationToken = default);
    Task<ChatSummariesResponse?> GetChatSummaryAsync(int chatId, int userId, CancellationToken cancellationToken = default);
}
