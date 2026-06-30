using System;
using System.Collections.Generic;

namespace NextHorizon.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Phone { get; set; }
        public string UserType { get; set; } // SuperAdmin, Admin, Finance Officer, Support Agent, Customer
        public string Status { get; set; } // active, inactive, suspended
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        
        // Navigation property
        public StaffInfo StaffInfo { get; set; }
    }

    public class StaffInfo
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Username { get; set; }
        public string Permissions { get; set; } // JSON string
        public int? AddedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsActive { get; set; }
        
        // Navigation properties
        public User User { get; set; }
        public StaffInfo AddedByStaff { get; set; }
        public ICollection<StaffInfo> Subordinates { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }

    public class AuthenticatedUser
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public string AccessLevel { get; set; }
        public string Permissions { get; set; }
        public DateTime LastActive { get; set; }
        
        // Helper property
        public bool IsSuperAdmin => UserType == "SuperAdmin";
        public bool IsAdmin => UserType == "Admin" || UserType == "SuperAdmin";
        public bool IsFinanceOfficer => UserType == "Finance Officer";
        public bool IsSupportAgent => UserType == "Support Agent";
        public bool IsQaUser => UserType == "QA" || UserType == "QA Analyst" || UserType == "QA Head" || UserType == "QA Reviewer";
    }
}
