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
    public partial class Order
    {
        public long OrderId { get; set; }
        [Required]
        public string ApplicationUserID { get; set; }
        public int Tokens { get; set; }
        public decimal Price { get; set; }
        public int Status { get; set; }
        public System.DateTime Time { get; set; }
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}