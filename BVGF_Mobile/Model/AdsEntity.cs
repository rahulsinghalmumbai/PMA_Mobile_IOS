using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Model
{
    public class AdsEntity
    {
        public long AdId { get; set; }
        public long? ContentId { get; set; }
        public string? ContentName { get; set; }
        public string? AddTitle { get; set; }
        public string? AdImage { get; set; }
        public AdType AdType { get; set; }
        public string? TargetKeywords { get; set; }
        public string? RedirectUrl { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public long? PriorityLevel { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
    }//for ios some changes
    public enum AdType
    {
        Native = 1,
        Banner = 2,
        Video = 3,
        Intestitiol = 4
    }
}
