using NextHorizon.Data;
using NextHorizon.Data.Messaging;
using NextHorizon.Models;
using NextHorizon.Messaging.Models;
using NextHorizon.Modules.MemberTracker.Security;
using NextHorizon.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextHorizon.Security;

namespace NextHorizon.Controllers;

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
    private readonly IAuthenticatedUserContextService _authenticatedUserContextService;

    public MessagesController(
        IMessagingRepository messagingRepository,
        IOrderConversationResolver orderConversationResolver,
        IWebHostEnvironment webHostEnvironment,
        IAuthenticatedUserContextService authenticatedUserContextService)
    {
        _messagingRepository = messagingRepository;
        _orderConversationResolver = orderConversationResolver;
        _webHostEnvironment = webHostEnvironment;
        _authenticatedUserContextService = authenticatedUserContextService;
    }

    [HttpPost("conversations")]
    [EnableRateLimiting("conversation-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<ConversationDto>> CreateOrGetConversation([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
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

            if (!ProductData.HasSeller(sellerUserInt))
            {
                return NotFound("Seller not found.");
            }

            if (currentUser.SellerId.HasValue && currentUser.SellerId.Value == sellerUserInt)
            {
                return BadRequest("Buyer and seller must be different accounts.");
            }

            summary = await _messagingRepository.CreateOrGetGeneralAsync(currentUser.ConsumerId.Value, sellerUserInt, cancellationToken);
        }
        else
        {
            if (!request.OrderId.HasValue || request.OrderId.Value <= 0)
            {
                return BadRequest("OrderId is required when ContextType is order.");
            }

            var actor = ToStorefrontActor(currentUser);
            var orderContext = await _orderConversationResolver.ResolveAsync(request.OrderId.Value, actor, cancellationToken);
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
                orderContext.BuyerConsumerId,
                orderContext.SellerId,
                cancellationToken);
        }

        var actorView = await _messagingRepository.GetConversationAsync(summary.ConversationId, ToStorefrontActor(currentUser), cancellationToken);
        return actorView is null ? NotFound() : Ok(ToConversationDto(actorView, currentUser.UserId));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ConversationDto>>> ListConversations([FromQuery] ConversationListQuery query, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var page = Math.Max(query.Page ?? DefaultPageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultConversationPageSize, 1, MaxPageSize);

        var paged = await _messagingRepository.ListByActorAsync(ToStorefrontActor(currentUser), page, pageSize, cancellationToken);

        return Ok(new PagedResult<ConversationDto>
        {
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = paged.Items.Select(summary => ToConversationDto(summary, currentUser.UserId)).ToList(),
        });
    }

    [HttpGet("conversations/{conversationId:int}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(int conversationId, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var conversation = await _messagingRepository.GetConversationAsync(conversationId, ToStorefrontActor(currentUser), cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(ToConversationDto(conversation, currentUser.UserId));
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [EnableRateLimiting("message-send")]
    [ConditionalValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, [FromForm] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
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
            var message = await _messagingRepository.SendMessageAsync(
                conversationId,
                ToStorefrontActor(currentUser),
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
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultMessagePageSize, 1, MaxPageSize);
        var messages = await _messagingRepository.ListMessagesAsync(
            conversationId,
            ToStorefrontActor(currentUser),
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
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var updated = await _messagingRepository.MarkReadAsync(conversationId, ToStorefrontActor(currentUser), cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("messages/{messageId:long}")]
    [EnableRateLimiting("message-send")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMessage(long messageId, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var deleted = await _messagingRepository.SoftDeleteMessageAsync(messageId, currentUser.UserId, cancellationToken);
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

    private static MessageActorContext ToStorefrontActor(AuthenticatedUserContext currentUser)
        => new(currentUser.UserId, currentUser.ConsumerId, null);

    private static ConversationDto ToConversationDto(MessageConversationSummary summary, int currentUserId)
        => new()
        {
            ConversationId = summary.ConversationId,
            CurrentUserId = currentUserId.ToString(),
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

