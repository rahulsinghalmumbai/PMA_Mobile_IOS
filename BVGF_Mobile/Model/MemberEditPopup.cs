using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Model
{
    public class MemberEditPopup:BaseEntity
    {
        public long MemberID { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string Mobile1 { get; set; }
        public string? Mobile2 { get; set; }
        public string? Mobile3 { get; set; }
        public string? Telephone { get; set; }
        public string Email1 { get; set; }
        public string? Email2 { get; set; }
        public string? Email3 { get; set; }
        public string? Company { get; set; }
        public string? CompAddress { get; set; }
        public string? CompCity { get; set; }
        public DateTime? DOB { get; set; }
        public bool? IsEdit { get; set; }
        public string? CategoryName { get; set; }
        public long? CreatedBy { get; set; }
    }
}
