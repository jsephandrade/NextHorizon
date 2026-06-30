using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class AdminUserViewModel
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActive { get; set; }
        public bool IsActive { get; set; }
        public string AddedByName { get; set; }
        public string DisplayStatus => IsActive ? "Active" : "Revoked";
        public string StatusBadgeClass => IsActive ? "bg-success-subtle text-success" : "bg-danger-subtle text-danger";
    }

    public class RevokedAdminViewModel
    {
        public int StaffId { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string UserType { get; set; }
        public DateTime RevokedAt { get; set; }
        public string RevokedBy { get; set; }
        public string RevokedReason { get; set; }
    }

    public class AddAdminRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserType { get; set; }
        public bool OverrideCredentials { get; set; }
        public string OverrideUsername { get; set; }
        public string OverridePassword { get; set; }
    }

    public class UpdateAdminRequest
    {
        public int StaffId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string UserType { get; set; }
        public bool OverrideCredentials { get; set; }
        public string OverrideUsername { get; set; }
        public string OverridePassword { get; set; }
    }

    public class RevokeAdminRequest
    {
        public int StaffId { get; set; }
        public string Reason { get; set; }
    }

    public class ReinstateAdminRequest
    {
        public int StaffId { get; set; }
        public string Reason { get; set; }
    }

    public class UpdateProfileRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }


}