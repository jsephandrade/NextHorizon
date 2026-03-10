using System.ComponentModel.DataAnnotations;
using NextHorizon.Models;

namespace NextHorizon.ViewModels
{
    public class DailyFitnessViewModel
    {
        public List<DailyFitnessDataRow> HourlyData { get; set; } = new();
        public DailyTotalsRow Totals { get; set; } = new();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        
        // Helper properties for the view
        public string FormattedTotalDistance => Totals?.TotalDistance.ToString("0.0") + " KM";
        public string FormattedTotalTime => Totals != null ? 
            TimeSpan.FromSeconds(Totals.TotalTimeSec).ToString(@"hh\:mm\:ss") : "00:00:00";
        public string FormattedTotalSteps => Totals?.TotalSteps.ToString("N0") ?? "0";
    }

    public class WeeklyFitnessViewModel
    {
        public List<WeeklyFitnessDataRow> WeeklyData { get; set; } = new();
        public WeeklyAveragesRow Averages { get; set; } = new();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }

        // Helper properties
        public string FormattedAvgDistance => Averages?.AvgDailyDistance.ToString("0.0") + " KM";
        public string FormattedAvgTime => Averages != null ? 
            TimeSpan.FromSeconds((double)Averages.AvgDailyTime).ToString(@"hh\:mm\:ss") : "00:00:00";
        public string FormattedAvgSteps => ((int)(Averages?.AvgDailySteps ?? 0)).ToString("N0");
        public string FormattedTotalDistance => Averages?.WeeklyTotalDistance.ToString("0.0") + " KM";
        
        // Get value for a specific day (for chart bars)
        public decimal GetDistanceForDay(int dayOfWeek)
        {
            return WeeklyData.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)?.TotalDistance ?? 0;
        }
        
        public int GetTimeForDay(int dayOfWeek)
        {
            return WeeklyData.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)?.TotalTimeSec ?? 0;
        }
        
        public int GetStepsForDay(int dayOfWeek)
        {
            return WeeklyData.FirstOrDefault(d => d.DayOfWeek == dayOfWeek)?.TotalSteps ?? 0;
        }
    }

    public class MonthlyFitnessViewModel
    {
        public List<MonthlyFitnessDataRow> MonthlyData { get; set; } = new();
        public MonthlyAveragesRow Averages { get; set; } = new();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int DaysInMonth { get; set; }

        // Helper properties
        public string FormattedAvgDistance => Averages?.AvgDailyDistance.ToString("0.0") + " KM";
        public string FormattedAvgTime => Averages != null ? 
            TimeSpan.FromSeconds((double)Averages.AvgDailyTime).ToString(@"hh\:mm\:ss") : "00:00:00";
        public string FormattedAvgSteps => ((int)(Averages?.AvgDailySteps ?? 0)).ToString("N0");
        public string FormattedTotalDistance => Averages?.MonthlyTotalDistance.ToString("0.0") + " KM";
        
        // Get value for a specific day
        public decimal GetDistanceForDay(int day)
        {
            return MonthlyData.FirstOrDefault(d => d.DayOfMonth == day)?.TotalDistance ?? 0;
        }
    }

    public class YearlyFitnessViewModel
    {
        public List<YearlyFitnessDataRow> YearlyData { get; set; } = new();
        public YearlyAveragesRow Averages { get; set; } = new();
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public int SelectedYear { get; set; }

        // Helper properties
        public string FormattedAvgDistance => Averages?.AvgMonthlyDistance.ToString("0.0") + " KM";
        public string FormattedAvgTime => Averages != null ? 
            TimeSpan.FromSeconds((double)Averages.AvgMonthlyTime).ToString(@"hh\:mm\:ss") : "00:00:00";
        public string FormattedAvgSteps => ((int)(Averages?.AvgMonthlySteps ?? 0)).ToString("N0");
        public string FormattedTotalDistance => Averages?.YearlyTotalDistance.ToString("0.0") + " KM";
        
        // Get value for a specific month (1-12)
        public decimal GetDistanceForMonth(int month)
        {
            return YearlyData.FirstOrDefault(d => d.MonthNumber == month)?.TotalDistance ?? 0;
        }
        
        public string GetMonthName(int month)
        {
            return YearlyData.FirstOrDefault(d => d.MonthNumber == month)?.MonthName ?? 
                   new DateTime(2000, month, 1).ToString("MMM");
        }
    }
}