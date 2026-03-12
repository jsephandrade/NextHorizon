using System.Security.Claims;
using MemberTracker.Data.Messaging;
using MemberTracker.Models;
using MemberTracker.Models.Messaging;
using MemberTracker.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MemberTracker.Controllers;

[ApiController]
[Authorize]
[Route("api/messages")]
public sealed class MessagesController : ControllerBase
{
    private const int DefaultPageNumber = 1;
    private const int DefaultConversationPageSize = 20;
    private const int DefaultMessagePageSize = 50;
    private const int MaxPageSize = 100;

    private readonly IMessagingRepository _messagingRepository;
    private readonly IOrderConversationResolver _orderConversationResolver;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public MessagesController(
        IMessagingRepository messagingRepository,
        IOrderConversationResolver orderConversationResolver,
        IWebHostEnvironment webHostEnvironment)
    {
        _messagingRepository = messagingRepository;
        _orderConversationResolver = orderConversationResolver;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpPost("conversations")]
    [EnableRateLimiting("conversation-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<ConversationDto>> CreateOrGetConversation([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var contextType = ParseContextType(request.ContextType);
        if (!contextType.HasValue)
        {
            return BadRequest("ContextType must be either 'general' or 'order'.");
        }

        MessageConversationSummary summary;

        if (contextType.Value == ConversationContextType.General)
        {
            var sellerUserId = request.SellerUserId?.Trim();
            if (string.IsNullOrWhiteSpace(sellerUserId) || !int.TryParse(sellerUserId, out var sellerUserInt) || sellerUserInt <= 0)
            {
                return BadRequest("SellerUserId must be a positive integer when ContextType is general.");
            }

            if (userId.Value == sellerUserInt)
            {
                return BadRequest("Buyer and seller must be different users.");
            }

            summary = await _messagingRepository.CreateOrGetGeneralAsync(userId.Value, sellerUserInt, cancellationToken);
        }
        else
        {
            if (!request.OrderId.HasValue || request.OrderId.Value <= 0)
            {
                return BadRequest("OrderId is required when ContextType is order.");
            }

            var orderContext = await _orderConversationResolver.ResolveAsync(request.OrderId.Value, userId.Value, cancellationToken);
            if (orderContext is null)
            {
                return NotFound("Order not found.");
            }

            if (!orderContext.CanRequestUserAccess)
            {
                return Forbid();
            }

            summary = await _messagingRepository.CreateOrGetOrderAsync(
                orderContext.OrderId,
                orderContext.BuyerUserId,
                orderContext.SellerUserId,
                cancellationToken);
        }

        return Ok(ToConversationDto(summary));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ConversationDto>>> ListConversations([FromQuery] ConversationListQuery query, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var page = Math.Max(query.Page ?? DefaultPageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultConversationPageSize, 1, MaxPageSize);

        var paged = await _messagingRepository.ListByUserAsync(userId.Value, page, pageSize, cancellationToken);

        return Ok(new PagedResult<ConversationDto>
        {
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Select(ToConversationDto).ToList(),
        });
    }

    [HttpGet("conversations/{conversationId:int}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(int conversationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var conversation = await _messagingRepository.GetConversationAsync(conversationId, userId.Value, cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(ToConversationDto(conversation));
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [EnableRateLimiting("message-send")]
    [ConditionalValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, [FromForm] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
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
            if (!UploadValidationRules.HaveAllowedExtension(request.Attachment) ||
                !UploadValidationRules.HaveAllowedContentType(request.Attachment) ||
                !UploadValidationRules.HaveMatchingExtensionAndContentType(request.Attachment) ||
                !UploadValidationRules.HaveMatchingSignature(request.Attachment) ||
                !UploadValidationRules.HaveValidImageStructure(request.Attachment) ||
                request.Attachment.Length <= 0 ||
                request.Attachment.Length > UploadValidationRules.MaxProofSizeBytes)
            {
                return BadRequest("Attachment must be a valid image (jpg, jpeg, png, webp) and 5MB or smaller.");
            }

            savedAttachment = await SaveAttachmentAsync(request.Attachment, cancellationToken);
        }

        try
        {
            var message = await _messagingRepository.SendMessageAsync(
                conversationId,
                userId.Value,
                body,
                savedAttachment?.AttachmentUrl,
                cancellationToken);

            if (message is null)
            {
                if (savedAttachment is not null)
                {
                    DeleteFileIfExists(savedAttachment.Value.AbsolutePath);
                }

                return NotFound();
            }

            return Ok(ToMessageDto(message));
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
        [FromQuery] MessageListQuery query,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultMessagePageSize, 1, MaxPageSize);
        var messages = await _messagingRepository.ListMessagesAsync(
            conversationId,
            userId.Value,
            query.Before,
            pageSize,
            cancellationToken);

        if (messages is null)
        {
            return NotFound();
        }

        return Ok(messages.Select(ToMessageDto).ToList());
    }

    [HttpPost("conversations/{conversationId:int}/read")]
    [EnableRateLimiting("message-read")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int conversationId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var updated = await _messagingRepository.MarkReadAsync(conversationId, userId.Value, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("messages/{messageId:long}")]
    [EnableRateLimiting("message-send")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(long messageId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var deleted = await _messagingRepository.SoftDeleteMessageAsync(messageId, userId.Value, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<(string AbsolutePath, string AttachmentUrl)> SaveAttachmentAsync(IFormFile attachment, CancellationToken cancellationToken)
    {
        var uploadsDirectory = GetAttachmentUploadsDirectory();
        Directory.CreateDirectory(uploadsDirectory);

        var extension = Path.GetExtension(attachment.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(uploadsDirectory, fileName);

        await using (var stream = System.IO.File.Create(absolutePath))
        {
            await attachment.CopyToAsync(stream, cancellationToken);
        }

        return (absolutePath, $"/uploads/message-attachments/{fileName}");
    }

    private string GetAttachmentUploadsDirectory()
    {
        var webRoot = _webHostEnvironment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            webRoot = Path.Combine(_webHostEnvironment.ContentRootPath, "wwwroot");
        }

        return Path.Combine(webRoot, "uploads", "message-attachments");
    }

    private static void DeleteFileIfExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }

    private static ConversationContextType? ParseContextType(string contextType)
    {
        if (string.Equals(contextType?.Trim(), "general", StringComparison.OrdinalIgnoreCase))
        {
            return ConversationContextType.General;
        }

        if (string.Equals(contextType?.Trim(), "order", StringComparison.OrdinalIgnoreCase))
        {
            return ConversationContextType.Order;
        }

        return null;
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
            SentAt = item.SentAt,
            IsDeleted = item.IsDeleted,
        };
}
