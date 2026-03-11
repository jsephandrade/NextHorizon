using System.Collections.Generic;
using NextHorizon.Models.Dtos;

namespace NextHorizon.Models
{
    public class ProfileViewVM
    {
        public ProfileHeaderRow Header { get; set; } = new ProfileHeaderRow();
        public ProfileTodayStatsRow Today { get; set; } = new ProfileTodayStatsRow();
        public List<ProfileRecentUploadRow> RecentUploads { get; set; } = new List<ProfileRecentUploadRow>();
    }
}