using System;
using System.Collections.Generic;

namespace NextHorizon.Models.Admin_Models
{
    public class ChallengeViewModel
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
        public string Prizes { get; set; } = string.Empty;
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BannerBase64 { get; set; } = string.Empty;
        public string BannerImageName { get; set; } = string.Empty;
        public string BannerImageContentType { get; set; } = string.Empty;
        public int TotalParticipants { get; set; }
        public int TotalCompleted { get; set; }
        public decimal CompletionRate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public byte[] BannerImageBinary { get; set; } = Array.Empty<byte>();
        
        // Display properties
        public string StatusBadgeClass => Status switch
        {
            "Live" => "bg-success",
            "Upcoming" => "bg-warning",
            "Completed" => "bg-secondary",
            "Cancelled" => "bg-danger",
            _ => "bg-secondary"
        };
        
        public string DateRange => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    }

    public class ChallengeDetailsViewModel
    {
        public ChallengeViewModel Challenge { get; set; } = new();
        public List<ParticipantLeaderboard> Leaderboard { get; set; } = new();
        public decimal AvgDistanceKm { get; set; }
        public decimal AvgTimeMinutes { get; set; }
    }

    public class ParticipantLeaderboard
    {    
        public int Rank { get; set; }  
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public int ConsumerId { get; set; }
        public string AthleteName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalDistanceKm { get; set; }
        public int TotalActivities { get; set; }
        public int TotalTimeSeconds { get; set; }
        public string TotalTimeFormatted { get; set; } = string.Empty;
        public decimal AveragePace { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? StoredRank { get; set; }  // The rank stored in challenge_participants table
        public string AvatarUrl { get; set; } = string.Empty;
        public decimal ChallengeGoalKm { get; set; }
        public decimal ProgressPercent { get; set; }
    }

    public class ActivityLogViewModel
    {
        public int ActivityId { get; set; }
        public int ParticipantId { get; set; }        // Add this - needed for tracking
         public int ChallengeId { get; set; }
        public DateTime ActivityDate { get; set; }
        public decimal DistanceKm { get; set; }
        public int DurationSeconds { get; set; }
        public string DurationFormatted => TimeSpan.FromSeconds(DurationSeconds).ToString(@"hh\:mm\:ss");
        public decimal AveragePace { get; set; }
        public string AveragePaceFormatted => $"{AveragePace:F2} min/km";
        public string ActivityType { get; set; } = string.Empty;
        public bool IsVerified { get; set; }
        public int? VerifiedBy { get; set; }
        public string VerifiedByName { get; set; } = string.Empty;
        public DateTime? VerifiedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ChallengeTitle { get; set; } = string.Empty;
        public string ImageProofBase64 { get; set; } = string.Empty;  
        public byte[] ImageProof { get; set; } = Array.Empty<byte>();
    }

    public class CreateChallengeRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
        public string Prizes { get; set; } = string.Empty;
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string BannerBase64 { get; set; } = string.Empty;
        public string BannerImageName { get; set; } = string.Empty;
        public string BannerImageContentType { get; set; } = string.Empty;
        public List<PrizeData> PrizesData { get; set; } = new(); // Add this
    }

    public class UpdateChallengeRequest
    {
        public int ChallengeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rules { get; set; } = string.Empty;
        public string Prizes { get; set; } = string.Empty;
        public decimal GoalKm { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string BannerBase64 { get; set; } = string.Empty;
        public string BannerImageName { get; set; } = string.Empty;
        public string BannerImageContentType { get; set; } = string.Empty;
        public List<PrizeData> PrizesData { get; set; } = new(); 
    }

    public class PrizeData
    {
        public int Tier { get; set; }
        public string TierName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PrizeTypeId { get; set; }
        public decimal? CashAmount { get; set; }
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; } = string.Empty;
        public string RewardName { get; set; } = string.Empty;
        public decimal? RewardValue { get; set; }
        public int Quantity { get; set; }
    }

    public class AddActivityRequest
    {
        public int ChallengeId { get; set; }
        public DateTime ActivityDate { get; set; }
        public decimal DistanceKm { get; set; }
        public int DurationSeconds { get; set; }
        public string ActivityType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public bool AutoVerify { get; set; }
    }

    public class VerifyActivityRequest
    {
        public int ActivityId { get; set; }
    }

    public class ChallengeStatistics
    {
        public int TotalAthletes { get; set; }
        public int ActiveChallenges { get; set; }
        public decimal AvgDistance { get; set; }
        public decimal TotalTimeHours { get; set; }
    }

    public class PrizeType
    {
        public int PrizeTypeId { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string TypeCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class ChallengePrize
    {
        public int PrizeId { get; set; }
        public int ChallengeId { get; set; }
        public int PrizeTypeId { get; set; }
        public int Tier { get; set; }
        public string TierName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Cash
        public decimal? CashAmount { get; set; }
        
        // Voucher
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; } = string.Empty;
        
        // Reward
        public string RewardName { get; set; } = string.Empty;
        public decimal? RewardValue { get; set; }
        public int? RewardQuantity { get; set; }
        
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class PrizeWinner
    {
        public int WinnerId { get; set; }
        public int PrizeId { get; set; }
        public int ParticipantId { get; set; }
        public int ChallengeId { get; set; }
        public int RankPosition { get; set; }
        
        public string ClaimStatus { get; set; } = string.Empty;
        public DateTime? ClaimDate { get; set; }
        public DateTime? ClaimDeadline { get; set; }
        public string ClaimNotes { get; set; } = string.Empty;
        
        public string ClaimCode { get; set; } = string.Empty;
        public DateTime? ClaimCodeGeneratedAt { get; set; }
        
        // Cash
        public string BankName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public DateTime? TransferDate { get; set; }
        
        // Voucher
        public string VoucherCode { get; set; } = string.Empty;
        public DateTime? VoucherExpiry { get; set; }
        public DateTime? VoucherSentDate { get; set; }
        public DateTime? VoucherRedemptionDate { get; set; }
        public bool VoucherUsed { get; set; }
        
        // Physical Reward
        public string ShippingAddress { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        
        // Digital Reward
        public string DigitalCode { get; set; } = string.Empty;
        public DateTime? DigitalCodeSentDate { get; set; }
        public bool DigitalCodeUsed { get; set; }
        
        public int? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
        public string AdminNotes { get; set; } = string.Empty;

        public string claimNotes { get; set; } = string.Empty;
    }

    // Request Models
    public class CreatePrizeRequest
    {
        public int ChallengeId { get; set; }
        public int PrizeTypeId { get; set; }
        public int Tier { get; set; }
        public string TierName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? CashAmount { get; set; }
        public decimal? VoucherDiscountPercent { get; set; }
        public decimal? VoucherDiscountFixed { get; set; }
        public decimal? VoucherMinimumPurchase { get; set; }
        public string VoucherType { get; set; } = string.Empty;
        public string RewardName { get; set; } = string.Empty;
        public decimal? RewardValue { get; set; }
        public int? RewardQuantity { get; set; }
        public int Quantity { get; set; }
    }

    public class ClaimPrizeRequest
    {
        public string ClaimCode { get; set; } = string.Empty;
        public string ClaimDetails { get; set; } = string.Empty; // JSON string
    }

    public class ProcessPrizeRequest
    {
        public int WinnerId { get; set; }
        public string Action { get; set; } = string.Empty; // approve, reject
        public string RejectionReason { get; set; } = string.Empty;
        public string AdminNotes { get; set; } = string.Empty;
        
        // For approved prizes
        public string VoucherCode { get; set; } = string.Empty;
        public DateTime? VoucherExpiry { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string DigitalCode { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
    }
    public class GlobalLeaderboardEntry
    {
        public int GlobalRank { get; set; }
        public int ParticipantId { get; set; }
        public int UserId { get; set; }
        public int ConsumerId { get; set; }
        public int ChallengeId { get; set; }
        public string ChallengeTitle { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public decimal ChallengeGoalKm { get; set; }
        public string AthleteName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal TotalDistanceKm { get; set; }
        public int TotalActivities { get; set; }
        public int TotalTimeSeconds { get; set; }
        public string TotalTimeFormatted { get; set; } = string.Empty;
        public decimal AveragePace { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? ChallengeRank { get; set; }
        public decimal ProgressPercent { get; set; }
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class UpdateParticipantStatusRequest
    {
        public int ParticipantId { get; set; }
        public int ChallengeId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class ClaimPrizeWithProofRequest
    {
        public string ClaimCode { get; set; } = string.Empty;
        public int WinnerId { get; set; }
        public string ProofImage { get; set; } = string.Empty;
        public string ProofFileName { get; set; } = string.Empty;
        public string ProofContentType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }
}
