using System;
using System.Collections.Generic;

namespace NextHorizon.Models
{
    public class FinanceViewModel
    {
        public string SellerName { get; set; } = string.Empty;
        public DateTime CurrentDate { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal PendingPayout { get; set; }
        public decimal TotalWithdrawn { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal ThisMonthRevenue { get; set; }
        public List<FinanceTransactionViewModel> Transactions { get; set; } = new();
    }

    public class FinanceTransactionViewModel
    {
        public string ReferenceId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

}
