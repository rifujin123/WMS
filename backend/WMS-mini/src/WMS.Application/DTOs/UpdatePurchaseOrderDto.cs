using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMS.Application.DTOs
{
    public class UpdatePurchaseOrderDto
    {
        public Guid? VendorId { get; set; }
        public string? VendorName { get; set; }
        public decimal CreditOffsetAmount { get; set; }
        public List<UpdatePurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new();
    }
}
