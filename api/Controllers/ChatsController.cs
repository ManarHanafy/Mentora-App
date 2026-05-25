using Microsoft.AspNetCore.Authorization;
using api.Contracts.Common;
using api.Extensions;
using api.Infrastructure.BackgroundJobs;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ChatsController(
    IChatService chatService,
    ApplicationDbContext db,
    IBackgroundTaskQueue backgroundTaskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<ChatsController> logger) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "ChatsWrite")]
    [ProducesResponseType(typeof(CreateChatResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var userExists = await db.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return NotFound(new { error = $"User {userId} not found." });

        var chatId = await chatService.CreateChatAsync(userId.Value, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CreateChatResponse(chatId));
    }

    [HttpPost("{chatId:int}/messages")]
    [Authorize(Policy = "ChatsWrite")]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendChatMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        try
        {
            var response = await chatService.SendMessageAsync(userId.Value, chatId, request.Message, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    [HttpPost("{chatId:int}/summarize")]
    [Authorize(Policy = "ChatsWrite")]
    [ProducesResponseType(typeof(ChatSummarizeResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SummarizeChat(int chatId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        try
        {
            var summarized = await chatService.SummarizeChatAsync(chatId, userId.Value, cancellationToken);
            if (summarized is null)
                return NotFound(new { error = $"Chat {chatId} not found." });

            return Ok(summarized);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }

    [HttpGet("{chatId:int}")]
    [Authorize(Policy = "ChatsRead")]
    [ProducesResponseType(typeof(ChatDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int chatId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var response = await chatService.GetChatByIdAsync(chatId, userId.Value, cancellationToken);
        return response is null
            ? NotFound(new { error = $"Chat {chatId} not found." })
            : Ok(response);
    }

    [HttpGet("{chatId:int}/summary")]
    [Authorize(Policy = "ChatsRead")]
    [ProducesResponseType(typeof(ChatSummariesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummaryByChatId(int chatId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var response = await chatService.GetChatSummaryAsync(chatId, userId.Value, cancellationToken);
        return response is null
            ? NotFound(new { error = $"Chat {chatId} not found." })
            : Ok(response);
    }

    [HttpGet("{chatId:int}/messages/{messageId:int}")]
    [Authorize(Policy = "ChatsRead")]
    [ProducesResponseType(typeof(ChatMessageDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageById(int chatId, int messageId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var response = await chatService.GetMessageByIdAsync(chatId, messageId, userId.Value, cancellationToken);
        return response is null
            ? NotFound(new { error = $"Message {messageId} not found." })
            : Ok(response);
    }

    [HttpGet]
    [Authorize(Policy = "ChatsRead")]
    [ProducesResponseType(typeof(PagedResponse<ChatHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        if (pageNumber < 1)
            return BadRequest(new { error = "pageNumber must be at least 1." });
        if (pageSize is < 1 or > 50)
            return BadRequest(new { error = "pageSize must be between 1 and 50." });

        var response = await chatService.GetUserChatsAsync(userId.Value, pageNumber, pageSize, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{chatId:int}/end")]
    [Authorize(Policy = "ChatsWrite")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> EndChat(int chatId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        try
        {
            var ended = await chatService.EndChatAsync(chatId, userId.Value, cancellationToken);
            if (!ended)
                return NotFound(new { error = $"Chat {chatId} not found." });

            return Ok(new { message = "Chat ended." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message });
        }
    }
}
