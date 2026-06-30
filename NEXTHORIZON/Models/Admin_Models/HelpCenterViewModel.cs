using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    // ════════════════════════════════════════════════
    //  OLD TICKET SYSTEM (V1 - BACKWARD COMPATIBILITY)
    // ════════════════════════════════════════════════
    public class HelpCenterStatsViewModel
    {
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int PendingTickets { get; set; }
        public int UrgentTickets { get; set; }
        public int ResolvedToday { get; set; }
        public string AvgResponse { get; set; } = "0h";
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string TicketNo { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public string Status { get; set; } = "open";
        public string Category { get; set; } = "orders";
        public string DateLabel { get; set; } = string.Empty;
        public string Role { get; set; } = "Consumer";
        public List<TicketMessage> Messages { get; set; } = new();
    }

    public class TicketMessage
    {
        public string Sender { get; set; } = "user";
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public string Body { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
    }

    public class HelpCenterViewModel
    {
        public HelpCenterStatsViewModel Stats { get; set; } = new();
        public List<SupportTicket> Tickets { get; set; } = new();
    }

    public class AdminReplyRequest
    {
        public int TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TicketActionRequest
    {
        public int TicketId { get; set; }
    }


    // ════════════════════════════════════════════════
    //  FAQ SYSTEM (SHARED BETWEEN V1 & V2)
    // ════════════════════════════════════════════════
    public class FaqItem
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string UserType { get; set; } = "Consumer";
    }

    public class AddFaqRequest
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string UserType { get; set; } = "Consumer";
    }

    public class UpdateFaqRequest
    {
        public int Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string UserType { get; set; } = "Consumer";
    }

    public class DeleteFaqRequest
    {
        public int Id { get; set; }
    }

    public class AddCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class DeleteCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
    }


    // ════════════════════════════════════════════════
    //  NEW QUEUE SYSTEM (V2 - LIVE CHAT / REAL-TIME)
    // ════════════════════════════════════════════════
    public class QueueDashboardStatsViewModel
    {
        public int InQueue { get; set; }
        public int ActiveSessions { get; set; }
        public string AvgWaitTime { get; set; } = "0:00";
        public int ResolvedToday { get; set; }
        public int AgentsOnline { get; set; }
        public double AbandonRate { get; set; }
    }

    public class QueueSession
    {
        public int Id { get; set; }
        public string SessionNo { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string Role { get; set; } = "Consumer";
        public string Initials { get; set; } = string.Empty;
        public string AvatarColor { get; set; } = "#1a1a1a";
        public int WaitSeconds { get; set; }
        public int QueuePosition { get; set; }
        public string Status { get; set; } = "waiting";
        public string Category { get; set; } = "orders";
        public string? AssignedTo { get; set; }
        public List<QueueMessage> Messages { get; set; } = new();
    }

    public class QueueMessage
    {
        public string Sender { get; set; } = "user";
        public string Body { get; set; } = string.Empty;
        public string TimeLabel { get; set; } = string.Empty;
    }

    public class AgentSlot
    {
        public int ConversationId { get; set; }
        public string? ClientName { get; set; }
        public string? Category { get; set; }
        public int SlotNumber { get; set; }
    }

    public class AgentStatusViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Status { get; set; } = "online";
        public int ActiveSessions { get; set; }
        public int MaxSessions { get; set; } = 3;
        public List<AgentSlot> Slots { get; set; } = new();
    }

    public class HelpCenterV2ViewModel
    {
        public QueueDashboardStatsViewModel Stats { get; set; } = new();
        public List<QueueSession> Sessions { get; set; } = new();
        public List<AgentStatusViewModel> Agents { get; set; } = new();
        public List<FaqItem> Faqs { get; set; } = new();
        public List<string> Categories { get; set; } = new();
    }

    public class AssignSessionRequest
    {
        public int SessionId { get; set; }
        public int AgentId { get; set; }
        public string AgentName { get; set; } = string.Empty;
    }

    public class QueueSendRequest
    {
        public int SessionId { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsNote { get; set; } = false;
    }

    public class QueueSessionActionRequest
    {
        public int SessionId { get; set; }
    }
}