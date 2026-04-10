using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models;
using System.Data;

namespace NextHorizon.Controllers
{
    [Route("api/support")]
    [ApiController]
    public class SupportConversationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SupportConversationController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/support/start - Start a new conversation
        [HttpPost("start")]
        public async Task<IActionResult> StartConversation(
            [FromQuery] int sellerId,
            [FromQuery] string category,
            [FromQuery] string question
        )
        {
            if (sellerId <= 0 || string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(question))
            {
                return BadRequest(new { error = "Missing required fields" });
            }

            // Create FAQ row for this concern
            var faq = new SupportFAQ
            {
                Category = category,
                Question = question,
                Status = "Waiting",
                UserType = "Seller",
                CreatedAt = DateTime.Now
            };

            _context.SupportFAQs.Add(faq);
            await _context.SaveChangesAsync();

            // Create conversation for this support request
            var conversation = new SupportConversation
            {
                SellerId = sellerId,
                Status = "Waiting",
                CreatedAt = DateTime.Now
            };

            _context.SupportConversations.Add(conversation);
            await _context.SaveChangesAsync();

            // Save first message in SupportMessages against the FAQ record
            var message = new SupportMessage
            {
                SupportFAQId = faq.Id,
                SenderId = sellerId,
                SenderRole = "Seller",
                MessageText = question,
                CreatedAt = DateTime.Now
            };

            _context.SupportMessages.Add(message);
            await _context.SaveChangesAsync();

            await AddAgentWaitingResponseAsync(faq.Id);

            return Ok(new
            {
                id = conversation.Id,
                supportFaqId = faq.Id,
                reused = false
            });
        }

        // POST: /api/support/message - Save a message in a conversation
        [HttpPost("message")]
        public async Task<IActionResult> SaveMessage([FromBody] SaveMessageRequest request)
        {
            try
            {
                if (request.ConversationId <= 0)
                {
                    return BadRequest(new { error = "Conversation id is required" });
                }

                if (request.SupportFAQId <= 0)
                {
                    return BadRequest(new { error = "Support FAQ id is required" });
                }

                if (string.IsNullOrWhiteSpace(request.MessageText))
                {
                    return BadRequest(new { error = "Message text is required" });
                }

                var conversation = await _context.SupportConversations.FindAsync(request.ConversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }

                if (string.Equals(conversation.Status, "Resolved", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Cannot send a message to a resolved conversation" });
                }

                var supportFaq = await _context.SupportFAQs.FindAsync(request.SupportFAQId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "Support FAQ not found" });
                }

                if (string.Equals(request.SenderRole, "Seller", StringComparison.OrdinalIgnoreCase))
                {
                    supportFaq.Question = request.MessageText;
                    _context.SupportFAQs.Update(supportFaq);
                }

                var message = new SupportMessage
                {
                    SupportFAQId = request.SupportFAQId,
                    SenderId = request.SenderId,
                    SenderRole = request.SenderRole,
                    MessageText = request.MessageText,
                    CreatedAt = DateTime.Now
                };

                _context.SupportMessages.Add(message);
                await _context.SaveChangesAsync();

                if (string.Equals(request.SenderRole, "Seller", StringComparison.OrdinalIgnoreCase) && !conversation.AgentId.HasValue && !supportFaq.AgentId.HasValue)
                {
                    await AddAgentWaitingResponseAsync(request.SupportFAQId);
                }

                return Ok(new { id = message.Id, message = "Message saved" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task AddAgentWaitingResponseAsync(int supportFaqId)
        {
            var supportFaq = await _context.SupportFAQs.FindAsync(supportFaqId);
            if (supportFaq == null || supportFaq.AgentId.HasValue)
            {
                return;
            }

            var alreadyHasWaitingMessage = await _context.SupportMessages
                .AnyAsync(m => m.SupportFAQId == supportFaqId
                               && m.SenderRole == "Agent"
                               && m.MessageText == "An agent will assist you shortly.");

            if (alreadyHasWaitingMessage)
            {
                return;
            }

            _context.SupportMessages.Add(new SupportMessage
            {
                SupportFAQId = supportFaqId,
                SenderId = 0,
                SenderRole = "Agent",
                MessageText = "An agent will assist you shortly.",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task AddAgentAvailableIndicatorAsync(int supportFaqId, int? agentId)
        {
            var supportFaq = await _context.SupportFAQs.FindAsync(supportFaqId);
            if (supportFaq == null)
            {
                return;
            }

            string? agentName = null;
            if (agentId.HasValue)
            {
                agentName = await GetAgentNameAsync(agentId.Value);
            }

            string indicatorText = !string.IsNullOrWhiteSpace(agentName)
                ? $"{agentName}: Enter the Conversation"
                : "Agent: Enter the Conversation";

            var alreadyHasIndicator = await _context.SupportMessages
                .AnyAsync(m => m.SupportFAQId == supportFaqId
                               && m.SenderRole == "Agent"
                               && m.MessageText.Contains("Enter the Conversation"));

            if (alreadyHasIndicator)
            {
                return;
            }

            _context.SupportMessages.Add(new SupportMessage
            {
                SupportFAQId = supportFaqId,
                SenderId = agentId ?? 0,
                SenderRole = "Agent",
                MessageText = indicatorText,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task AddInactivityNoticeAsync(int supportFaqId)
        {
            const string inactivityNotice = "The conversation will now be closed due to inactivity. Have a great day!";

            var alreadyHasNotice = await _context.SupportMessages
                .AnyAsync(m => m.SupportFAQId == supportFaqId
                               && m.SenderRole == "Agent"
                               && m.MessageText == inactivityNotice);

            if (alreadyHasNotice)
            {
                return;
            }

            _context.SupportMessages.Add(new SupportMessage
            {
                SupportFAQId = supportFaqId,
                SenderId = 0,
                SenderRole = "Agent",
                MessageText = inactivityNotice,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private async Task<string?> GetAgentNameAsync(int agentKey)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP 1 AgentName FROM Agents WHERE UserID = @key OR AgentID = @key";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@key";
            parameter.Value = agentKey;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync();
            return result as string;
        }

        private async Task UpdateAgentChatStatusAsync(int conversationId, string chatStatus)
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            command.CommandText = "UPDATE Agents SET ChatStatus = @status WHERE ConversationID = @conversationId";

            var statusParam = command.CreateParameter();
            statusParam.ParameterName = "@status";
            statusParam.Value = chatStatus;
            command.Parameters.Add(statusParam);

            var conversationParam = command.CreateParameter();
            conversationParam.ParameterName = "@conversationId";
            conversationParam.Value = conversationId;
            command.Parameters.Add(conversationParam);

            await command.ExecuteNonQueryAsync();
        }

        private async Task<bool> ResolveSellerInactivityAsync(int faqId)
        {
            var supportFaq = await _context.SupportFAQs.FindAsync(faqId);
            if (supportFaq == null)
            {
                return false;
            }

            if (string.Equals(supportFaq.Status, "Resolved", StringComparison.OrdinalIgnoreCase)
                || string.Equals(supportFaq.Status, "Closed", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var lastSellerMessage = await _context.SupportMessages
                .Where(m => m.SupportFAQId == faqId && m.SenderRole == "Seller")
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastSellerMessage == null)
            {
                return false;
            }

            if (lastSellerMessage.CreatedAt.AddMinutes(5) > DateTime.Now)
            {
                return false;
            }

            supportFaq.Status = "Resolved";
            supportFaq.EndTime = DateTime.Now;
            _context.SupportFAQs.Update(supportFaq);

            const string inactivityNotice = "The conversation will now be closed due to inactivity. Have a great day!";
            var alreadyHasNotice = await _context.SupportMessages
                .AnyAsync(m => m.SupportFAQId == faqId
                               && m.SenderRole == "Agent"
                               && m.MessageText == inactivityNotice);

            if (!alreadyHasNotice)
            {
                _context.SupportMessages.Add(new SupportMessage
                {
                    SupportFAQId = faqId,
                    SenderId = 0,
                    SenderRole = "Agent",
                    MessageText = inactivityNotice,
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            // Update agent chat status after inactivity resolution
            if (supportFaq.AgentId.HasValue)
            {
                var conversation = await _context.SupportConversations
                    .FirstOrDefaultAsync(c => c.AgentId == supportFaq.AgentId && c.Status != "Closed");
                
                if (conversation != null)
                {
                    await UpdateAgentChatStatusAsync(conversation.Id, "Resolved");
                }
            }

            return true;
        }

        // GET: /api/support/faq/{faqId}/messages - Get all messages for a support FAQ
        [HttpGet("faq/{faqId}/messages")]
        public async Task<IActionResult> GetFaqMessages(int faqId)
        {
            try
            {
                var supportFaq = await _context.SupportFAQs.FindAsync(faqId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "SupportFAQ not found" });
                }

                await ResolveSellerInactivityAsync(faqId);
                supportFaq = await _context.SupportFAQs.FindAsync(faqId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "SupportFAQ not found" });
                }

                var messages = await _context.SupportMessages
                    .Where(m => m.SupportFAQId == faqId)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync();

                string? agentName = null;
                if (supportFaq.AgentId.HasValue)
                {
                    agentName = await GetAgentNameAsync(supportFaq.AgentId.Value);
                }

                return Ok(new { agentName, status = supportFaq.Status, messages });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: /api/support/{conversationId}/state - Get conversation status
        [HttpGet("{conversationId}/state")]
        public async Task<IActionResult> GetConversationState(int conversationId)
        {
            try
            {
                var conversation = await _context.SupportConversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }

                return Ok(new { status = conversation.Status, agentId = conversation.AgentId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: /api/support/{conversationId}/close - Close a conversation
        [HttpPut("{conversationId}/close")]
        public async Task<IActionResult> CloseConversation(int conversationId)
        {
            try
            {
                var conversation = await _context.SupportConversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }

                conversation.Status = "Closed";
                _context.SupportConversations.Update(conversation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Conversation closed" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: /api/support/{conversationId}/status?status=Resolved&supportFaqId=123 - Update conversation status
        [HttpPut("{conversationId}/status")]
        public async Task<IActionResult> UpdateConversationStatus(int conversationId, [FromQuery] string status, [FromQuery] int? supportFaqId = null, [FromQuery] bool inactivity = false)
        {
            try
            {
                var conversation = await _context.SupportConversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    return NotFound(new { error = "Conversation not found" });
                }

                if (string.IsNullOrWhiteSpace(status))
                {
                    return BadRequest(new { error = "Status is required" });
                }

                var allowed = new[] { "Active", "Waiting", "Resolved", "Closed" };
                if (!allowed.Contains(status, StringComparer.OrdinalIgnoreCase))
                {
                    return BadRequest(new { error = "Invalid status value" });
                }

                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && !conversation.AgentId.HasValue)
                {
                    return BadRequest(new { error = "Cannot set Active until an agent is assigned" });
                }

                conversation.Status = status;
                _context.SupportConversations.Update(conversation);

                if (supportFaqId.HasValue)
                {
                    var supportFaq = await _context.SupportFAQs.FindAsync(supportFaqId.Value);
                    if (supportFaq != null)
                    {
                        supportFaq.Status = status;
                        _context.SupportFAQs.Update(supportFaq);
                    }
                }

                await _context.SaveChangesAsync();

                await UpdateAgentChatStatusAsync(conversationId, status);

                if (string.Equals(status, "Resolved", StringComparison.OrdinalIgnoreCase)
                    && inactivity
                    && supportFaqId.HasValue)
                {
                    await AddInactivityNoticeAsync(supportFaqId.Value);
                }

                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) && supportFaqId.HasValue)
                {
                    await AddAgentAvailableIndicatorAsync(supportFaqId.Value, conversation.AgentId);
                }

                return Ok(new { message = "Conversation status updated", status = conversation.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST: /api/support/faq - Save conversation part into SupportFAQs table
        [HttpPost("faq")]
        public async Task<IActionResult> SaveSupportFaq([FromBody] SaveSupportFaqRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest(new { error = "Question is required" });
                }

                var supportFaq = new SupportFAQ
                {
                    Category = request.Category ?? string.Empty,
                    Question = request.Question,
                    Status = request.Status ?? "Waiting",
                    DurationMinutes = 0,
                    UserType = request.UserType ?? "Seller",
                    AgentId = request.AgentId,
                    CreatedAt = DateTime.Now
                };

                _context.SupportFAQs.Add(supportFaq);
                await _context.SaveChangesAsync();

                return Ok(new { id = supportFaq.Id, message = "SupportFAQ saved" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        // PUT: /api/support/faq/{faqId} - Update SupportFAQ status and metadata
        [HttpPut("faq/{faqId}")]
        public async Task<IActionResult> UpdateSupportFaq(int faqId, [FromBody] UpdateSupportFaqRequest request)
        {
            try
            {
                var supportFaq = await _context.SupportFAQs.FindAsync(faqId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "SupportFAQ not found" });
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    var allowed = new[] { "Waiting", "Active", "Resolved", "Closed" };
                    if (!allowed.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
                    {
                        return BadRequest(new { error = "Invalid status value" });
                    }

                    supportFaq.Status = request.Status;
                }

                _context.SupportFAQs.Update(supportFaq);
                await _context.SaveChangesAsync();

                // Update agent chat status if FAQ status changed and agent is assigned
                if (!string.IsNullOrWhiteSpace(request.Status) && supportFaq.AgentId.HasValue)
                {
                    var conversation = await _context.SupportConversations
                        .FirstOrDefaultAsync(c => c.AgentId == supportFaq.AgentId && c.Status != "Closed");
                    
                    if (conversation != null)
                    {
                        await UpdateAgentChatStatusAsync(conversation.Id, request.Status);
                    }
                }

                return Ok(new { message = "SupportFAQ updated", status = supportFaq.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: /api/support/faq/{faqId}/start - Mark conversation start time
        [HttpPut("faq/{faqId}/start")]
        public async Task<IActionResult> MarkConversationStart(int faqId)
        {
            try
            {
                var supportFaq = await _context.SupportFAQs.FindAsync(faqId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "SupportFAQ not found" });
                }

                supportFaq.StartTime = DateTime.Now;
                _context.SupportFAQs.Update(supportFaq);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Conversation start time recorded" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // PUT: /api/support/faq/{faqId}/end - Mark conversation end time
        [HttpPut("faq/{faqId}/end")]
        public async Task<IActionResult> MarkConversationEnd(int faqId)
        {
            try
            {
                var supportFaq = await _context.SupportFAQs.FindAsync(faqId);
                if (supportFaq == null)
                {
                    return NotFound(new { error = "SupportFAQ not found" });
                }

                supportFaq.EndTime = DateTime.Now;
                _context.SupportFAQs.Update(supportFaq);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Conversation end time recorded" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // Request model for saving messages
    public class SaveMessageRequest
    {
        public int ConversationId { get; set; }
        public int SupportFAQId { get; set; }
        public int SenderId { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string MessageText { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    // Request model for support faq save
    public class SaveSupportFaqRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public int? AgentId { get; set; }
    }

    // Request model for support faq update
    public class UpdateSupportFaqRequest
    {
        public string Status { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}
