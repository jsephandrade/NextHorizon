using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    public class DailyFitnessDataRow
    {
        public int HourOfDay { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalTimeSec { get; set; }
        public int TotalSteps { get; set; }
        public int ActivityCount { get; set; }
    }

    public class DailyTotalsRow
    {
        public decimal TotalDistance { get; set; }
        public int TotalTimeSec { get; set; }
        public int TotalSteps { get; set; }
        public int TotalActivities { get; set; }
    }

    public class WeeklyFitnessDataRow
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalTimeSec { get; set; }
        public int TotalSteps { get; set; }
        public int ActivityCount { get; set; }
    }

    public class WeeklyAveragesRow
    {
        public decimal AvgDailyDistance { get; set; }
        public decimal AvgDailyTime { get; set; }
        public decimal AvgDailySteps { get; set; }
        public decimal WeeklyTotalDistance { get; set; }
        public decimal WeeklyTotalTime { get; set; }
        public decimal WeeklyTotalSteps { get; set; }
    }

    public class MonthlyFitnessDataRow
    {
        public int DayOfMonth { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalTimeSec { get; set; }
        public int TotalSteps { get; set; }
        public int ActivityCount { get; set; }
    }

    public class MonthlyAveragesRow
    {
        public decimal AvgDailyDistance { get; set; }
        public decimal AvgDailyTime { get; set; }
        public decimal AvgDailySteps { get; set; }
        public decimal MonthlyTotalDistance { get; set; }
        public decimal MonthlyTotalTime { get; set; }
        public decimal MonthlyTotalSteps { get; set; }
    }

    public class YearlyFitnessDataRow
    {
        public int MonthNumber { get; set; }
        public string MonthName { get; set; }
        public decimal TotalDistance { get; set; }
        public int TotalTimeSec { get; set; }
        public int TotalSteps { get; set; }
        public int ActivityCount { get; set; }
    }

    public class YearlyAveragesRow
    {
        public decimal AvgMonthlyDistance { get; set; }
        public decimal AvgMonthlyTime { get; set; }
        public decimal AvgMonthlySteps { get; set; }
        public decimal YearlyTotalDistance { get; set; }
        public decimal YearlyTotalTime { get; set; }
        public decimal YearlyTotalSteps { get; set; }
    }

    // MemberUpload entity for the database table
    [Table("MemberUploads")]
    public class MemberUpload
    {
        [Key]
        [Column("UploadId")]
        public int UploadId { get; set; }

        [Column("UserId")]
        public int UserId { get; set; }

        [Column("Title")]
        public string Title { get; set; }

        [Column("ActivityName")]
        public string ActivityName { get; set; }

        [Column("ActivityDate")]
        public DateTime ActivityDate { get; set; }

        [Column("ProofUrl")]
        public string ProofUrl { get; set; }

        [Column("DistanceKm")]
        public decimal DistanceKm { get; set; }

        [Column("MovingTimeSec")]
        public int MovingTimeSec { get; set; }

        [Column("AvgPaceSecPerKm")]
        public int AvgPaceSecPerKm { get; set; }

        [Column("CreatedAt")]
        public DateTime? CreatedAt { get; set; }

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [Column("Steps")]
        public int Steps { get; set; }
    }
}