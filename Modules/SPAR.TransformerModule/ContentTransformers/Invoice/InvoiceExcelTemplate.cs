using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace SPAR.TransformerModule.ContentTransformers.Invoice
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class ExcelColumnAttribute : Attribute
    {
        public ExcelColumnAttribute()
        {
            Width = 14;
        }

        public string DisplayName { get; set; }
        public string Format { get; set; }
        public int Order { get; set; }
        public int Width { get; set; }
    }

    internal class InvoiceExcelTemplate : IDisposable
    {
        #region Static
        private static readonly string[] SheetNames = { "Jan+Feb", "Mar+Apr", "May+June", "July+Aug", "Sept+Oct", "Nov+Dec" };
        private static readonly List<SheetColumnInfo> SheetColumns = new List<SheetColumnInfo>();
        private static readonly Dictionary<int, string> KeyLookup = new Dictionary<int, string>
        {
            {1, SheetNames[0]},
            {2, SheetNames[0]},
            {3, SheetNames[1]},
            {4, SheetNames[1]},
            {5, SheetNames[2]},
            {6, SheetNames[2]},
            {7, SheetNames[3]},
            {8, SheetNames[3]},
            {9, SheetNames[4]},
            {10, SheetNames[4]},
            {11, SheetNames[5]},
            {12, SheetNames[5]}
        };

        static InvoiceExcelTemplate()
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            List<SheetColumnInfo> temporaryList = new List<SheetColumnInfo>();
            temporaryList.AddRange(typeof(Invoice).GetProperties().Select(x => new SheetColumnInfo(x)));
            temporaryList.Add(new SheetColumnInfo("Summaries", typeof(decimal))
            {
                Order = 7,
                Format = "0.00",
                Width = 14
            });
            SheetColumns.AddRange(temporaryList.OrderBy(x => x.Order));
        }
        #endregion

        private readonly ExcelPackage _excelPackage;
        private readonly Dictionary<string, ExcelWorksheet> _sheets;

        public InvoiceExcelTemplate()
        {
            _excelPackage = new ExcelPackage();
            
            _sheets = new Dictionary<string, ExcelWorksheet>();
            foreach (string sheetName in SheetNames)
            {
                ExcelWorksheet worksheet = _excelPackage.Workbook.Worksheets.Add(sheetName);
                worksheet.InsertColumn(1, SheetColumns.Count);
                for (int i = 0; i < SheetColumns.Count; i++)
                {
                    ExcelColumn column = worksheet.Column(i + 1);
                    column.Width = SheetColumns[i].Width;
                    column.Style.Numberformat.Format = SheetColumns[i].Format;

                    ExcelRange cell = worksheet.Cells[1, i + 1];
                    cell.Value = SheetColumns[i].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(Color.Black);
                }
                _sheets.Add(sheetName, worksheet);
            }
        }

        public void ApplyData(IEnumerable<Invoice> yearlyInvoices)
        {
            foreach (IGrouping<string, Invoice> monthGrouping in yearlyInvoices.GroupBy(x => KeyLookup[x.CreateDate.Month]))
            {
                ExcelWorksheet worksheet = _sheets[monthGrouping.Key];
                foreach (Invoice invoice in monthGrouping)
                {
                    
                }
            }
        }

        public void Save(Stream outStream)
        {
            _excelPackage.SaveAs(outStream);
        }

        #region IDisposable
        public void Dispose()
        {
            _excelPackage.Dispose();
        }
        #endregion

        #region Inner Type
        private class SheetColumnInfo
        {
            public SheetColumnInfo(string columnName, Type columnType)
            {
                ColumnName = columnName;
                ColumnType = columnType;
                Format = "General";
            }
            public SheetColumnInfo(PropertyInfo property)
            {
                ExcelColumnAttribute excelColumnAttribute = property.GetCustomAttribute<ExcelColumnAttribute>();

                ColumnName = excelColumnAttribute?.DisplayName ?? property.Name;
                ColumnType = property.PropertyType;
                Format = excelColumnAttribute?.Format ?? "General";
                Order = excelColumnAttribute?.Order ?? 0;
                Width = excelColumnAttribute?.Width ?? 14;
            }

            public string ColumnName { get; }
            public Type ColumnType { get; }
            public string Format { get; set; }
            public int Order { get; set; }
            public int Width { get; set; }
        }
        #endregion
    }
}