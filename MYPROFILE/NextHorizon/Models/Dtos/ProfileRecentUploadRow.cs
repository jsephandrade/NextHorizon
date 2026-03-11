using System;

namespace NextHorizon.Models.Dtos
{
    public class ProfileRecentUploadRow
    {
        public int UserId { get; set; }
        public string Title { get; set; } = "";
        public string ActivityName { get; set; } = "";
        public DateTime? ActivityDate { get; set; }
        public string? ProofUrl { get; set; }
        public decimal? DistanceKm { get; set; }
        public int? MovingTimeSec { get; set; }
        public int? AvgPaceSecPerKm { get; set; }
    }
}