using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace IEP_Petar.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public partial class Bid
    {
        public long BidId { get; set; }

        [Required]
        public string ApplicationUserID { get; set; }
        [Required]
        public long AuctionId { get; set; }
        public System.DateTime Time { get; set; }
        public int Amount { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public virtual Auction Auction { get; set; }
    }
}