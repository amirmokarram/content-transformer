using System;

namespace SPAR.TransformerModule.ContentTransformers.Invoice
{
    internal class Invoice
    {
        [ExcelColumn(DisplayName = "Branch", Format = "0", Order = 1)]
        public int Branch { get; set; }
        [ExcelColumn(DisplayName = "Create Date", Format = "yyyy-mm-dd", Order = 2)]
        public DateTime CreateDate { get; set; }
        [ExcelColumn(DisplayName = "Code", Format = "@", Order = 3)]
        public string Code { get; set; }
        [ExcelColumn(DisplayName = "Price", Format = "0.00", Order = 4)]
        public decimal Price { get; set; }
        [ExcelColumn(DisplayName = "Quantity", Format = "0", Order = 5)]
        public int Quantity { get; set; }
        [ExcelColumn(DisplayName = "Total", Format = "0.00", Order = 6)]
        public decimal Total
        {
            get
            {
                return Quantity * Price;
            }
        }

        #region Static
        public static Invoice Parse(string data)
        {
            if (data == null)
                return null;
            string[] invoiceParts = data.Split(';');
            Invoice invoice = new Invoice();
            invoice.Branch = int.Parse(invoiceParts[0]);
            invoice.CreateDate = DateTime.Parse(invoiceParts[1]);
            invoice.Code = invoiceParts[2];
            invoice.Price = int.Parse(invoiceParts[3]);
            invoice.Quantity = int.Parse(invoiceParts[5]);
            return invoice;
        }
        #endregion
    }
}
