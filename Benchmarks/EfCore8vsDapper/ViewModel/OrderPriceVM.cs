using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfCore8vsDapper.ViewModel
{
    public record OrderPriceVM
    {
        public int SalesOrderID { get; set; }
        public int TotalOrderQty { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalDiscount { get; set; }
    }
}
