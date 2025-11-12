using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Model
{
    public class MstMember : BaseEntity
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
        public string CityAddress
        {
            get
            {
                var result = new List<string>();

                if (!string.IsNullOrWhiteSpace(City))
                    result.Add(City.Trim());

                if (!string.IsNullOrWhiteSpace(Address))
                    result.Add(Address.Trim());

                return result.Count > 0 ? string.Join(", ", result) : "N/A";
            }
        }

        public string CompanyDetails
        {
            get
            {
                var result = new List<string>();

                if (!string.IsNullOrWhiteSpace(Company))
                    result.Add(Company.Trim());

                if (!string.IsNullOrWhiteSpace(CompAddress))
                    result.Add(CompAddress.Trim());

                if (!string.IsNullOrWhiteSpace(CompCity))
                    result.Add(CompCity.Trim());

                return result.Count > 0 ? string.Join(", ", result) : "N/A";
            }
        }

        public string DisplayName
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Name) ? Name : "Unknown Contact";
            }
        }

        public bool HasMobile2
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Mobile2);
            }
        }

      
        public bool HasEmail
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Email1);
            }
        }

     
        public string PrimaryContact
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Mobile1))
                    return Mobile1;
                if (!string.IsNullOrWhiteSpace(Email1))
                    return Email1;
                return "No contact info";
            }
        }

       
        public string FormattedContactInfo
        {
            get
            {
                var info = new StringBuilder();
                info.AppendLine($"Name: {DisplayName}");

                if (!string.IsNullOrWhiteSpace(Mobile1))
                    info.AppendLine($"Mobile: {Mobile1}");

                if (HasMobile2)
                    info.AppendLine($"Alt Mobile: {Mobile2}");

                if (HasEmail)
                    info.AppendLine($"Email: {Email1}");

                if (!string.IsNullOrWhiteSpace(Company))
                    info.AppendLine($"Company: {Company}");

                if (!string.IsNullOrWhiteSpace(City))
                    info.AppendLine($"City: {City}");

                return info.ToString().Trim();
            }
        }
    }
}