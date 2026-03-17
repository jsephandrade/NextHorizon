using NextHorizon.Data;
using NextHorizon.Data.Messaging;
using NextHorizon.Models;
using NextHorizon.Messaging.Models;
using NextHorizon.Modules.MemberTracker.Security;
using NextHorizon.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
    private readonly ApplicationDbContext _dbContext;

    public MessagesController(
        IMessagingRepository messagingRepository,
        IOrderConversationResolver orderConversationResolver,
        IWebHostEnvironment webHostEnvironment,
        IAuthenticatedUserContextService authenticatedUserContextService,
        ApplicationDbContext dbContext)
    {
        _messagingRepository = messagingRepository;
        _orderConversationResolver = orderConversationResolver;
        _webHostEnvironment = webHostEnvironment;
        _authenticatedUserContextService = authenticatedUserContextService;
        _dbContext = dbContext;
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

            var actor = ToMessageActor(currentUser, ConversationActorScope.Consumer);
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

        var actorView = await _messagingRepository.GetConversationAsync(summary.ConversationId, ToMessageActor(currentUser, ConversationActorScope.Consumer), cancellationToken);
        return actorView is null
            ? NotFound()
            : Ok(await ToConversationDtoAsync(actorView, currentUser, ConversationActorScope.Consumer, cancellationToken));
    }

    [HttpGet("conversations/resolve")]
    public async Task<ActionResult<ConversationDto>> ResolveConversation(
        [FromQuery] ResolveConversationQuery query,
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

        var contextType = ParseContextType(query.ContextType);
        if (!contextType.HasValue)
        {
            return BadRequest("ContextType must be either 'general' or 'order'.");
        }

        int? sellerUserInt = null;
        if (!string.IsNullOrWhiteSpace(query.SellerUserId))
        {
            if (!int.TryParse(query.SellerUserId.Trim(), out var parsedSellerUserId) || parsedSellerUserId <= 0)
            {
                return BadRequest("SellerUserId must be a positive integer when provided.");
            }

            sellerUserInt = parsedSellerUserId;
        }

        if (contextType.Value == ConversationContextType.General && !sellerUserInt.HasValue)
        {
            return BadRequest("SellerUserId is required when ContextType is general.");
        }

        if (contextType.Value == ConversationContextType.Order && (!query.OrderId.HasValue || query.OrderId.Value <= 0))
        {
            return BadRequest("OrderId is required when ContextType is order.");
        }

        var conversation = await _messagingRepository.FindConversationAsync(
            ToMessageActor(currentUser, ConversationActorScope.Consumer),
            contextType.Value,
            sellerUserInt,
            query.OrderId,
            cancellationToken);

        if (conversation is null)
        {
            return NoContent();
        }

        return Ok(await ToConversationDtoAsync(conversation, currentUser, ConversationActorScope.Consumer, cancellationToken));
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<PagedResult<ConversationDto>>> ListConversations([FromQuery] ConversationListQuery query, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.HasMessagingRole)
        {
            return Forbid();
        }

        var scope = ParseConversationActorScope(query.Role);
        if (!scope.HasValue)
        {
            return BadRequest("Role must be either 'seller' or 'consumer' when provided.");
        }

        if (scope.Value == ConversationActorScope.Seller && !currentUser.SellerId.HasValue)
        {
            return Forbid();
        }

        if (scope.Value == ConversationActorScope.Consumer && !currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var page = Math.Max(query.Page ?? DefaultPageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize ?? DefaultConversationPageSize, 1, MaxPageSize);

        var paged = await _messagingRepository.ListByActorAsync(ToMessageActor(currentUser, scope.Value), scope.Value, page, pageSize, cancellationToken);
        var items = await ToConversationDtosAsync(paged.Items, currentUser, scope.Value, cancellationToken);

        return Ok(new PagedResult<ConversationDto>
        {
            PageNumber = paged.PageNumber,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = items,
        });
    }

    [HttpGet("conversations/{conversationId:int}")]
    public async Task<ActionResult<ConversationDto>> GetConversation(int conversationId, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.HasMessagingRole)
        {
            return Forbid();
        }

        var scope = ParseConversationActorScope(role);
        if (!scope.HasValue)
        {
            return BadRequest("Role must be either 'seller' or 'consumer' when provided.");
        }

        if (scope.Value == ConversationActorScope.Seller && !currentUser.SellerId.HasValue)
        {
            return Forbid();
        }

        if (scope.Value == ConversationActorScope.Consumer && !currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var conversation = await _messagingRepository.GetConversationAsync(conversationId, ToMessageActor(currentUser, scope.Value), cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(await ToConversationDtoAsync(conversation, currentUser, scope.Value, cancellationToken));
    }

    [HttpPost("conversations/{conversationId:int}/messages")]
    [EnableRateLimiting("message-send")]
    [ConditionalValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<MessageDto>> SendMessage(int conversationId, [FromForm] SendMessageRequest request, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.HasMessagingRole)
        {
            return Forbid();
        }

        var scope = ParseConversationActorScope(role);
        if (!scope.HasValue)
        {
            return BadRequest("Role must be either 'seller' or 'consumer' when provided.");
        }

        if (scope.Value == ConversationActorScope.Seller && !currentUser.SellerId.HasValue)
        {
            return Forbid();
        }

        if (scope.Value == ConversationActorScope.Consumer && !currentUser.ConsumerId.HasValue)
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
                ToMessageActor(currentUser, scope.Value),
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

        if (!currentUser.HasMessagingRole)
        {
            return Forbid();
        }

        var pageSize = Math.Clamp(query.PageSize ?? DefaultMessagePageSize, 1, MaxPageSize);
        var scope = ParseConversationActorScope(query.Role);
        if (!scope.HasValue)
        {
            return BadRequest("Role must be either 'seller' or 'consumer' when provided.");
        }

        if (scope.Value == ConversationActorScope.Seller && !currentUser.SellerId.HasValue)
        {
            return Forbid();
        }

        if (scope.Value == ConversationActorScope.Consumer && !currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var messages = await _messagingRepository.ListMessagesAsync(
            conversationId,
            ToMessageActor(currentUser, scope.Value),
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
    public async Task<IActionResult> MarkRead(int conversationId, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        if (!currentUser.HasMessagingRole)
        {
            return Forbid();
        }

        var scope = ParseConversationActorScope(role);
        if (!scope.HasValue)
        {
            return BadRequest("Role must be either 'seller' or 'consumer' when provided.");
        }

        if (scope.Value == ConversationActorScope.Seller && !currentUser.SellerId.HasValue)
        {
            return Forbid();
        }

        if (scope.Value == ConversationActorScope.Consumer && !currentUser.ConsumerId.HasValue)
        {
            return Forbid();
        }

        var updated = await _messagingRepository.MarkReadAsync(conversationId, ToMessageActor(currentUser, scope.Value), cancellationToken);
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

        if (!currentUser.HasMessagingRole)
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

    private static ConversationActorScope? ParseConversationActorScope(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return ConversationActorScope.Any;
        }

        if (string.Equals(role.Trim(), "seller", StringComparison.OrdinalIgnoreCase))
        {
            return ConversationActorScope.Seller;
        }

        if (string.Equals(role.Trim(), "consumer", StringComparison.OrdinalIgnoreCase))
        {
            return ConversationActorScope.Consumer;
        }

        return null;
    }

    private static MessageActorContext ToMessageActor(AuthenticatedUserContext currentUser)
        => ToMessageActor(currentUser, ConversationActorScope.Any);

    private static MessageActorContext ToMessageActor(AuthenticatedUserContext currentUser, ConversationActorScope scope)
        => new(ResolveEffectiveMessagingUserId(currentUser, scope), currentUser.ConsumerId, currentUser.SellerId);

    private static int ResolveEffectiveMessagingUserId(AuthenticatedUserContext currentUser, ConversationActorScope scope)
    {
        return scope switch
        {
            ConversationActorScope.Consumer when currentUser.ConsumerAccountUserId.HasValue => currentUser.ConsumerAccountUserId.Value,
            ConversationActorScope.Seller when currentUser.SellerAccountUserId.HasValue => currentUser.SellerAccountUserId.Value,
            _ => currentUser.UserId,
        };
    }

    private async Task<List<ConversationDto>> ToConversationDtosAsync(
        IEnumerable<MessageConversationSummary> summaries,
        AuthenticatedUserContext currentUser,
        ConversationActorScope scope,
        CancellationToken cancellationToken)
    {
        var summaryList = summaries.ToList();
        if (summaryList.Count == 0)
        {
            return [];
        }

        var buyerIds = summaryList
            .Select(summary => summary.BuyerUserId)
            .Distinct()
            .ToList();

        var consumerDisplayNames = await _dbContext.Set<ConsumerRef>()
            .AsNoTracking()
            .Where(consumer => buyerIds.Contains(consumer.ConsumerId))
            .ToDictionaryAsync(consumer => consumer.ConsumerId, BuildConsumerDisplayName, cancellationToken);

        return summaryList
            .Select(summary => ToConversationDto(summary, currentUser, scope, consumerDisplayNames))
            .ToList();
    }

    private async Task<ConversationDto> ToConversationDtoAsync(
        MessageConversationSummary summary,
        AuthenticatedUserContext currentUser,
        ConversationActorScope scope,
        CancellationToken cancellationToken)
    {
        var consumerDisplayNames = await _dbContext.Set<ConsumerRef>()
            .AsNoTracking()
            .Where(consumer => consumer.ConsumerId == summary.BuyerUserId)
            .ToDictionaryAsync(consumer => consumer.ConsumerId, BuildConsumerDisplayName, cancellationToken);

        return ToConversationDto(summary, currentUser, scope, consumerDisplayNames);
    }

    private static ConversationDto ToConversationDto(
        MessageConversationSummary summary,
        AuthenticatedUserContext currentUser,
        ConversationActorScope scope,
        IReadOnlyDictionary<int, string> consumerDisplayNames)
    {
        var viewerIsSeller = scope == ConversationActorScope.Seller
            || (scope == ConversationActorScope.Any
                && currentUser.SellerId.HasValue
                && currentUser.SellerId.Value == summary.SellerUserId
                && !currentUser.ConsumerId.HasValue);
        var contextLabel = summary.ContextType == ConversationContextType.Order
            ? $"Order #{summary.OrderId}"
            : "General inquiry";

        string displayName;
        string displaySubtitle;
        string avatarUrl;
        string counterpartyRole;
        string counterpartyId;

        if (viewerIsSeller)
        {
            displayName = consumerDisplayNames.TryGetValue(summary.BuyerUserId, out var consumerName) &&
                !string.IsNullOrWhiteSpace(consumerName)
                ? consumerName
                : "Consumer";
            displaySubtitle = contextLabel;
            avatarUrl = string.Empty;
            counterpartyRole = "consumer";
            counterpartyId = summary.BuyerUserId.ToString();
        }
        else
        {
            var seller = ProductData.Sellers.FirstOrDefault(item => item.Id == summary.SellerUserId);
            displayName = seller?.ShopName ?? "Seller";
            displaySubtitle = contextLabel;
            avatarUrl = seller?.Avatar ?? string.Empty;
            counterpartyRole = "seller";
            counterpartyId = summary.SellerUserId.ToString();
        }

        return new()
        {
            ConversationId = summary.ConversationId,
            CurrentUserId = ResolveEffectiveMessagingUserId(currentUser, scope).ToString(),
            BuyerUserId = summary.BuyerUserId.ToString(),
            SellerUserId = summary.SellerUserId.ToString(),
            DisplayName = displayName,
            DisplaySubtitle = displaySubtitle,
            AvatarUrl = avatarUrl,
            ContextLabel = contextLabel,
            CounterpartyRole = counterpartyRole,
            CounterpartyId = counterpartyId,
            CanReply = currentUser.HasMessagingRole,
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
    }

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

    private static string BuildConsumerDisplayName(ConsumerRef consumer)
    {
        var fullName = string.Join(
            " ",
            new[] { consumer.FirstName, consumer.MiddleName, consumer.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return consumer.Username?.Trim() ?? string.Empty;
    }
}

