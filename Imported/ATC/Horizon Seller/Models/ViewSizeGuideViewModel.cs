using System.Collections.Generic;

namespace Horizon_Seller.Models
{
    public class ViewSizeGuideViewModel
    {
        public int ProductId { get; set; }
        public string ProductTitle { get; set; }
        public bool IsPhotoUpload { get; set; }
        public List<string> UploadedPhotoUrls { get; set; } = new List<string>();
        public string MeasurementUnit { get; set; }
        public string Category { get; set; }
        public string TableTitle { get; set; }
        public List<List<string>> TableData { get; set; } = new List<List<string>>();
        public string FitTips { get; set; }
        public string HowToMeasure { get; set; }
        public string AdditionalNotes { get; set; }
    }
}

