using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Connection
{
    public class AppSettings
    {
        //public const string BaseApiUrl = "https://localhost:7055/api/";
        // On Android emulator
        //public const string BaseApiUrl = "http://172.23.176.1:5151//api/";
        //online
      public const string BaseApiUrl = "http://195.250.31.98:2030/api/";

    }

    public static class Endpoints
    {
        public const string Login = "MstMember/login";
        public const string SearchMember = "MstMember";
        public const string CategaryDrp = "MstCategary/DropDown";
        public const string EditMember = "MstMember_Edit/UpsertMember_Edit";
        public const string GetMemberDataByMemberId = "MstMember_Edit/GetMember_EditByMemId";

    }
}
