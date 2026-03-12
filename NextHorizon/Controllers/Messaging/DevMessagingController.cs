using MemberTracker.Data.Messaging;
using MemberTracker.Models;
using MemberTracker.Models.Messaging;
using MemberTracker.Models.Messaging.Dev;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberTracker.Controllers;

[ApiController]
[Authorize]
[Route("api/dev/messages")]
public sealed class DevMessagingController : ControllerBase
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private readonly IMessagingRepository _messagingRepository;
    private readonly IOrderConversationResolver _orderConversationResolver;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public DevMessagingController(
        IMessagingRepository messagingRepository,
        IOrderConversationResolver orderConversationResolver,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration)
    {
        _messagingRepository = messagingRepository;
        _orderConversationResolver = orderConversationResolver;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
    }

    [HttpPost("conversations/general")]
    public async Task<ActionResult<ConversationDto>> CreateOrGetGeneral([FromBody] DevGeneralConversationRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(request.ActorUserId, out var actorUserId) ||
            !TryParseUserId(request.BuyerUserId, out var buyerUserId) ||
            !TryParseUserId(request.SellerUserId, out var sellerUserId))
        {
            return BadRequest("ActorUserId, BuyerUserId, and SellerUserId must be positive integers.");
        }

        if (buyerUserId == sellerUserId)
        {
            return BadRequest("BuyerUserId and SellerUserId must be different.");
        }

        if (!IsParticipant(actorUserId, buyerUserId, sellerUserId))
        {
            return BadRequest("ActorUserId must be either BuyerUserId or SellerUserId.");
        }

        var conversation = await _messagingRepository.CreateOrGetGeneralAsync(buyerUserId, sellerUserId, cancellationToken);
        var actorView = await _messagingRepository.GetConversationAsync(conversation.ConversationId, actorUserId, cancellationToken);

        return actorView is null ? NotFound() : Ok(ToConversationDto(actorView));
    }

    [HttpPost("conversations/order")]
    public async Task<ActionResult<ConversationDto>> CreateOrGetOrder([FromBody] DevOrderConversationRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(request.ActorUserId, out var actorUserId) || request.OrderId <= 0)
        {
            return BadRequest("ActorUserId must be a positive integer and OrderId must be greater than zero.");
        }

        int buyerUserId;
        int sellerUserId;

        if (TryParseUserId(request.BuyerUserId, out var explicitBuyerUserId) &&
            TryParseUserId(request.SellerUserId, out var explicitSellerUserId))
        {
            buyerUserId = explicitBuyerUserId;
            sellerUserId = explicitSellerUserId;

            if (!IsParticipant(actorUserId, buyerUserId, sellerUserId))
            {
                return Forbid();
            }
        }
        else
        {
            var resolved = await _orderConversationResolver.ResolveAsync(request.OrderId, actorUserId, cancellationToken);
            if (resolved is null)
            {
                return NotFound("Order not found.");
            }

            if (!resolved.CanRequestUserAccess)
            {
                return Forbid();
            }

            buyerUserId = resolved.BuyerUserId;
            sellerUserId = resolved.SellerUserId;
        }

        var conversation = await _messagingRepository.CreateOrGetOrderAsync(request.OrderId, buyerUserId, sellerUserId, cancellationToken);
        var actorView = await _messagingRepository.GetConversationAsync(conversation.ConversationId, actorUserId, cancellationToken);

        return actorView is null ? NotFound() : Ok(ToConversationDto(actorView));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ConversationDto>>> ListConversations(
        [FromQuery] string actorUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(actorUserId, out var actorUserGuid))
        {
            return BadRequest("actorUserId must be a positive integer.");
        }

        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var paged = await _messagingRepository.ListByUserAsync(actorUserGuid, normalizedPage, normalizedPageSize, cancellationToken);

        return Ok(new PagedResult<ConversationDto>
        {
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Select(ToConversationDto).ToList(),
        });
    }

    [HttpGet("conversations/{conversationId:int}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(int conversationId, [FromQuery] string actorUserId, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(actorUserId, out var actorUserGuid))
        {
            return BadRequest("actorUserId must be a positive integer.");
        }

        var conversation = await _messagingRepository.GetConversationAsync(conversationId, actorUserGuid, cancellationToken);
        return conversation is null ? NotFound() : Ok(ToConversationDto(conversation));
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, [FromForm] DevSendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(request.ActorUserId, out var actorUserId))
        {
            return BadRequest("ActorUserId must be a positive integer.");
        }

        var body = request.Body?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(body) && request.Attachment is null)
        {
            return BadRequest("Either Body or Attachment is required.");
        }

        if (body.Length > 2000)
        {
            return BadRequest("Body must be 2000 characters or less.");
        }

        (string AbsolutePath, string AttachmentUrl)? savedAttachment = null;
        if (request.Attachment is not null)
        {
            if (!UploadValidationRules.BeValidMessageAttachment(request.Attachment))
            {
                return BadRequest("Attachment must be a valid image or video (jpg, jpeg, png, webp, mp4, webm, mov) and 5MB or smaller.");
            }

            savedAttachment = await SaveAttachmentAsync(request.Attachment, cancellationToken);
        }

        try
        {
            var item = await _messagingRepository.SendMessageAsync(conversationId, actorUserId, body, savedAttachment?.AttachmentUrl, cancellationToken);
            if (item is null)
            {
                if (savedAttachment is not null)
                {
                    DeleteFileIfExists(savedAttachment.Value.AbsolutePath);
                }

                return NotFound();
            }

            return Ok(ToMessageDto(item));
        }
        catch
        {
            if (savedAttachment is not null)
            {
                DeleteFileIfExists(savedAttachment.Value.AbsolutePath);
            }

            throw;
        }
    }

    [HttpGet("conversations/{conversationId:int}/messages")]
    public async Task<ActionResult<IReadOnlyList<MessageDto>>> ListMessages(
        int conversationId,
        [FromQuery] string actorUserId,
        [FromQuery] DateTime? before,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(actorUserId, out var actorUserGuid))
        {
            return BadRequest("actorUserId must be a positive integer.");
        }

        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var messages = await _messagingRepository.ListMessagesAsync(conversationId, actorUserGuid, before, normalizedPageSize, cancellationToken);

        return messages is null
            ? NotFound()
            : Ok(messages.Select(ToMessageDto).ToList());
    }

    [HttpPost("conversations/{conversationId:int}/read")]
    public async Task<IActionResult> MarkRead(int conversationId, [FromBody] DevActorRequest request, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(request.ActorUserId, out var actorUserId))
        {
            return BadRequest("ActorUserId must be a positive integer.");
        }

        var updated = await _messagingRepository.MarkReadAsync(conversationId, actorUserId, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("messages/{messageId:long}")]
    public async Task<IActionResult> DeleteMessage(long messageId, [FromQuery] string actorUserId, CancellationToken cancellationToken)
    {
        if (!IsDevEnabled())
        {
            return NotFound();
        }

        if (!TryParseUserId(actorUserId, out var actorUserGuid))
        {
            return BadRequest("actorUserId must be a positive integer.");
        }

        var deleted = await _messagingRepository.SoftDeleteMessageAsync(messageId, actorUserGuid, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private bool IsDevEnabled()
        => _webHostEnvironment.IsDevelopment() && _configuration.GetValue("Features:EnableDevMessaging", false);

    private static bool TryParseUserId(string? userId, out int value)
        => int.TryParse(userId?.Trim(), out value) && value > 0;

    private static bool IsParticipant(int actorUserId, int buyerUserId, int sellerUserId)
        => actorUserId == buyerUserId || actorUserId == sellerUserId;

    private async Task<(string AbsolutePath, string AttachmentUrl)> SaveAttachmentAsync(IFormFile attachment, CancellationToken cancellationToken)
    {
        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        }

        var uploadsDirectory = Path.Combine(webRoot, "uploads", "message-attachments");
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await attachment.CopyToAsync(stream, cancellationToken);
        }

        return (filePath, $"/uploads/message-attachments/{fileName}");
    }

    private static void DeleteFileIfExists(string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(filePath) && System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private static ConversationDto ToConversationDto(MessageConversationSummary summary)
        => new()
        {
            ConversationId = summary.ConversationId,
            BuyerUserId = summary.BuyerUserId.ToString(),
            SellerUserId = summary.SellerUserId.ToString(),
            ContextType = summary.ContextType == ConversationContextType.Order ? "order" : "general",
            OrderId = summary.OrderId,
            LastMessageAt = summary.LastMessageAt,
            BuyerLastReadAt = summary.BuyerLastReadAt,
            SellerLastReadAt = summary.SellerLastReadAt,
            LastMessagePreview = summary.LastMessagePreview,
            UnreadCount = summary.UnreadCount,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
        };

    private static MessageDto ToMessageDto(MessageItem item)
        => new()
        {
            MessageId = item.MessageId,
            ConversationId = item.ConversationId,
            SenderUserId = item.SenderUserId.ToString(),
            Body = item.Body,
            AttachmentUrl = item.AttachmentUrl,
            IsDeleted = item.IsDeleted,
            SentAt = item.SentAt,
        };
}
