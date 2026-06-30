using System;

namespace NextHorizon.Models
{
    public class AuditLog
    {
        public int LogId { get; set; }
        public DateTime Timestamp { get; set; }
        public int StaffId { get; set; }
        public string AdminName { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string TargetType { get; set; }
        public int? TargetId { get; set; }
        public string Status { get; set; }
        public string Details { get; set; } // JSON string
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        
        // Navigation property
        public StaffInfo Staff { get; set; }
    }

    public class AuditLogViewModel
    {
        public int LogId { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimestampFormatted => Timestamp.ToString("yyyy-MM-dd HH:mm");
        public string AdminName { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string TargetType { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
        public string IpAddress { get; set; }
    }


    public class AuditLogFilterRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string AdminName { get; set; }
        public string Action { get; set; }
        public string TargetType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}