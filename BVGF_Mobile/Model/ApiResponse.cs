using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Model
{
    public class ApiResponse<T>
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }
    public class MemberResponseData
    {
        public List<MstMember> Members { get; set; }
        public int TotalCount { get; set; }
    }
    public class categaryResponseData
    {
        public List<mstCategary> categaries { get; set; }
    }
    public class AdsResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<AdsEntity> Data { get; set; }
    }
}
