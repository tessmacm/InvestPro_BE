using System;

namespace IMS.Core.Entities
{
    public class SystemReport
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "PDF";
        public string Size { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string TargetInvestorIds { get; set; } = "all";
    }
}
