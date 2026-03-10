using Microsoft.AspNetCore.Mvc;
using NextHorizon.Services;
using NextHorizon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using NextHorizon.ViewModels;

namespace NextHorizon.Controllers
{
    public class FitnessOverviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<FitnessOverviewController> _logger;

        public FitnessOverviewController(ApplicationDbContext db, ILogger<FitnessOverviewController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        private IActionResult CheckAuthentication()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }
            return null;
        }

        [HttpGet]
        public async Task<IActionResult> DailyFitnessOverview(DateTime? date)
        {
            var authResult = CheckAuthentication();
            if (authResult != null) return authResult;

            var userId = GetCurrentUserId().Value;
            var selectedDate = date ?? DateTime.Today;

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@SelectedDate", selectedDate)
                };

                // Execute stored procedure and get multiple result sets
                using (var command = _db.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "dbo.sp_Fitness_DailyOverview";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    await _db.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var viewModel = new DailyFitnessViewModel
                        {
                            SelectedDate = selectedDate,
                            HourlyData = new List<DailyFitnessDataRow>(),
                            Totals = new DailyTotalsRow()
                        };

                        // Read hourly data
                        while (await reader.ReadAsync())
                        {
                            viewModel.HourlyData.Add(new DailyFitnessDataRow
                            {
                                HourOfDay = reader.GetInt32(0),
                                TotalDistance = reader.GetDecimal(1),
                                TotalTimeSec = reader.GetInt32(2),
                                TotalSteps = reader.GetInt32(3),
                                ActivityCount = reader.GetInt32(4)
                            });
                        }

                        // Read totals
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            viewModel.Totals.TotalDistance = reader.GetDecimal(0);
                            viewModel.Totals.TotalTimeSec = reader.GetInt32(1);
                            viewModel.Totals.TotalSteps = reader.GetInt32(2);
                            viewModel.Totals.TotalActivities = reader.GetInt32(3);
                        }

                        return View(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading daily fitness data for user {UserId}", userId);
                return View(new DailyFitnessViewModel 
                { 
                    SelectedDate = selectedDate,
                    HourlyData = new List<DailyFitnessDataRow>(),
                    Totals = new DailyTotalsRow()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> WeeklyFitnessOverview(DateTime? date)
        {
            var authResult = CheckAuthentication();
            if (authResult != null) return authResult;

            var userId = GetCurrentUserId().Value;
            var selectedDate = date ?? DateTime.Today;

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@SelectedDate", selectedDate)
                };

                using (var command = _db.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "dbo.sp_Fitness_WeeklyOverview";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    await _db.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Calculate week start and end
                        var weekStart = selectedDate.AddDays(-(int)selectedDate.DayOfWeek + 1);
                        var weekEnd = weekStart.AddDays(6);

                        var viewModel = new WeeklyFitnessViewModel
                        {
                            SelectedDate = selectedDate,
                            WeekStart = weekStart,
                            WeekEnd = weekEnd,
                            WeeklyData = new List<WeeklyFitnessDataRow>(),
                            Averages = new WeeklyAveragesRow()
                        };

                        // Read daily data
                        while (await reader.ReadAsync())
                        {
                            viewModel.WeeklyData.Add(new WeeklyFitnessDataRow
                            {
                                DayOfWeek = reader.GetInt32(0),
                                DayName = reader.GetString(1),
                                TotalDistance = reader.GetDecimal(2),
                                TotalTimeSec = reader.GetInt32(3),
                                TotalSteps = reader.GetInt32(4),
                                ActivityCount = reader.GetInt32(5)
                            });
                        }

                        // Read averages
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            viewModel.Averages.AvgDailyDistance = reader.GetDecimal(0);
                            viewModel.Averages.AvgDailyTime = reader.GetDecimal(1);
                            viewModel.Averages.AvgDailySteps = reader.GetDecimal(2);
                            viewModel.Averages.WeeklyTotalDistance = reader.GetDecimal(3);
                            viewModel.Averages.WeeklyTotalTime = reader.GetDecimal(4);
                            viewModel.Averages.WeeklyTotalSteps = reader.GetDecimal(5);
                        }

                        return View(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading weekly fitness data for user {UserId}", userId);
                return View(new WeeklyFitnessViewModel 
                { 
                    SelectedDate = selectedDate,
                    WeeklyData = new List<WeeklyFitnessDataRow>(),
                    Averages = new WeeklyAveragesRow()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> MonthlyFitnessOverview(DateTime? date)
        {
            var authResult = CheckAuthentication();
            if (authResult != null) return authResult;

            var userId = GetCurrentUserId().Value;
            var selectedDate = date ?? DateTime.Today;

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@SelectedDate", selectedDate)
                };

                using (var command = _db.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "dbo.sp_Fitness_MonthlyOverview";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    await _db.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var viewModel = new MonthlyFitnessViewModel
                        {
                            SelectedDate = selectedDate,
                            DaysInMonth = DateTime.DaysInMonth(selectedDate.Year, selectedDate.Month),
                            MonthlyData = new List<MonthlyFitnessDataRow>(),
                            Averages = new MonthlyAveragesRow()
                        };

                        // Read daily data
                        while (await reader.ReadAsync())
                        {
                            viewModel.MonthlyData.Add(new MonthlyFitnessDataRow
                            {
                                DayOfMonth = reader.GetInt32(0),
                                TotalDistance = reader.GetDecimal(1),
                                TotalTimeSec = reader.GetInt32(2),
                                TotalSteps = reader.GetInt32(3),
                                ActivityCount = reader.GetInt32(4)
                            });
                        }

                        // Read averages
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            viewModel.Averages.AvgDailyDistance = reader.GetDecimal(0);
                            viewModel.Averages.AvgDailyTime = reader.GetDecimal(1);
                            viewModel.Averages.AvgDailySteps = reader.GetDecimal(2);
                            viewModel.Averages.MonthlyTotalDistance = reader.GetDecimal(3);
                            viewModel.Averages.MonthlyTotalTime = reader.GetDecimal(4);
                            viewModel.Averages.MonthlyTotalSteps = reader.GetDecimal(5);
                        }

                        return View(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading monthly fitness data for user {UserId}", userId);
                return View(new MonthlyFitnessViewModel 
                { 
                    SelectedDate = selectedDate,
                    MonthlyData = new List<MonthlyFitnessDataRow>(),
                    Averages = new MonthlyAveragesRow()
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> YearlyFitnessOverview(int? year)
        {
            var authResult = CheckAuthentication();
            if (authResult != null) return authResult;

            var userId = GetCurrentUserId().Value;
            var selectedYear = year ?? DateTime.Today.Year;
            var selectedDate = new DateTime(selectedYear, 1, 1);

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@SelectedDate", selectedDate)
                };

                using (var command = _db.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = "dbo.sp_Fitness_YearlyOverview";
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddRange(parameters);

                    await _db.Database.OpenConnectionAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var viewModel = new YearlyFitnessViewModel
                        {
                            SelectedDate = selectedDate,
                            SelectedYear = selectedYear,
                            YearlyData = new List<YearlyFitnessDataRow>(),
                            Averages = new YearlyAveragesRow()
                        };

                        // Read monthly data
                        while (await reader.ReadAsync())
                        {
                            viewModel.YearlyData.Add(new YearlyFitnessDataRow
                            {
                                MonthNumber = reader.GetInt32(0),
                                MonthName = reader.GetString(1),
                                TotalDistance = reader.GetDecimal(2),
                                TotalTimeSec = reader.GetInt32(3),
                                TotalSteps = reader.GetInt32(4),
                                ActivityCount = reader.GetInt32(5)
                            });
                        }

                        // Read averages
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            viewModel.Averages.AvgMonthlyDistance = reader.GetDecimal(0);
                            viewModel.Averages.AvgMonthlyTime = reader.GetDecimal(1);
                            viewModel.Averages.AvgMonthlySteps = reader.GetDecimal(2);
                            viewModel.Averages.YearlyTotalDistance = reader.GetDecimal(3);
                            viewModel.Averages.YearlyTotalTime = reader.GetDecimal(4);
                            viewModel.Averages.YearlyTotalSteps = reader.GetDecimal(5);
                        }

                        return View(viewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading yearly fitness data for user {UserId}", userId);
                return View(new YearlyFitnessViewModel 
                { 
                    SelectedYear = selectedYear,
                    SelectedDate = selectedDate,
                    YearlyData = new List<YearlyFitnessDataRow>(),
                    Averages = new YearlyAveragesRow()
                });
            }
        }
    }
}