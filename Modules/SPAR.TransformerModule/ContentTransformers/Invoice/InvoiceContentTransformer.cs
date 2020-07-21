using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ContentTransformer.Common.Services.ContentTransformer;

namespace SPAR.TransformerModule.ContentTransformers.Invoice
{
    internal class InvoiceContentTransformer : IContentTransformer
    {
        public TransformInfo Transform(IEnumerable<IContentStoreModel> contents)
        {
            ConcurrentBag<Invoice> invoices = new ConcurrentBag<Invoice>();
            Parallel.ForEach(contents, content =>
            {
                using (MemoryStream contentStream = new MemoryStream(content.Load()))
                {
                    using (StreamReader streamReader = new StreamReader(contentStream))
                    {
                        string stringContent = streamReader.ReadToEnd();
                        invoices.Add(Invoice.Parse(stringContent));
                    }
                }
            });
            
            MemoryStream transformStream = new MemoryStream();
            using (InvoiceExcelTemplate template = new InvoiceExcelTemplate())
            {
                template.ApplyData(invoices);
                template.Save(transformStream);
            }
            transformStream.Position = 0;
            return new TransformInfo
            {
                Name = $"SPAR{DateTime.Now:yyyyMMddhhmmss}",
                Extension = ".xlsx",
                MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                TransformStream = transformStream
            };
        }
    }
}
