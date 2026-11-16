using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebProject.Models;

namespace WebProject.Services
{
    public class AuditPdfService
    {
        public byte[] GeneratePdf(
            List<Audit> records,
            string timeframe)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // PDF header
                    page.Header().Column(column =>
                    {
                        column.Item().Text("Asset & Licence Tracker")
                            .FontSize(18)
                            .Bold();

                        column.Item().Text("Audit Trail Report")
                            .FontSize(14);

                        column.Item().Text($"Timeframe: {timeframe}");

                        column.Item().Text(
                            $"Total Records: {records.Count}");

                        column.Item().PaddingVertical(10)
                            .LineHorizontal(1);
                    });

                    // Audit records
                    page.Content().PaddingVertical(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(65);
                            columns.RelativeColumn(5);
                            columns.ConstantColumn(125);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell)
                                .Text("Audit ID");

                            header.Cell().Element(HeaderCell)
                                .Text("Audit Message");

                            header.Cell().Element(HeaderCell)
                                .Text("Date / Time");
                        });

                        foreach (var audit in records)
                        {
                            table.Cell().Element(DataCell)
                                .Text(audit.AuditId.ToString());

                            table.Cell().Element(DataCell)
                                .Text(audit.AuditMessage);

                            table.Cell().Element(DataCell)
                                .Text(audit.AuditDateTime);
                        }
                    });

                    // Page numbers
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }

        private static IContainer HeaderCell(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten2)
                .BorderBottom(1)
                .Padding(7)
                .DefaultTextStyle(x => x.Bold());
        }

        private static IContainer DataCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(7);
        }
    }
}
