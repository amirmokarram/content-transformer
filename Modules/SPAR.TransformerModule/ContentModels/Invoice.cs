using System;

namespace SPAR.TransformerModule.ContentModels
{
    internal class Invoice
    {
        public int BranchCode { get; set; }
        public DateTime CreateDate { get; set; }
        public string ProductCode { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
