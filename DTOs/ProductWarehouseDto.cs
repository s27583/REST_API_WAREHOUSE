using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REST_API_WAREHOUSE.DTOs
{
    public class ProductWarehouseDto
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int OrderId { get; set; }
        public double Price { get; set; }
    }
}