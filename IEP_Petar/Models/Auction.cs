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
    using System.Web;
    public partial class Auction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Auction()
        {
            this.Bids = new HashSet<Bid>();
        }
        
        public const int DRAFT = 1;
        public const int READY = 2;
        public const int OPEN = 3;
        public const int SOLD = 4;
        public const int EXPIRED = 5;
        public const int DELETED = 6;   //Za potrebe logickog, ali ne i fizickog brisanja iz baze

        public long AuctionId { get; set; }
        public string ProductName { get; set; }
        public long Duration { get; set; }
        public int StartPrice { get; set; }
        public int CurrentPrice { get; set; }
        public string ApplicationUserID { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
        public System.DateTime CreationTime { get; set; }
        public System.DateTime OpeningTime { get; set; }
        public System.DateTime ClosingTime { get; set; }
        public int State { get; set; }
        public byte[] Photo { get; set; }

        [NotMapped]
        public HttpPostedFileBase ImageToUpload { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Bid> Bids { get; set; }
    }
}