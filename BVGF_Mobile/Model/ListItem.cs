using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BVGF.Model
{
    public class ListItem
    {
        public bool IsAd { get; set; }
    }
    public class MemberItem : ListItem
    {
        public MstMember Member { get; set; }

        public MemberItem(MstMember member)
        {
            IsAd = false;
            Member = member;
        }
    }
    public class AdItem : ListItem
    {
        public AdsEntity Ad { get; set; }

        public AdItem(AdsEntity ad)
        {
            IsAd = true;
            Ad = ad;
        }
    }

}
