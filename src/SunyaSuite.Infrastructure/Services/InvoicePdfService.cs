using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Infrastructure.Services;

public class InvoicePdfService : IInvoicePdfService
{
    public InvoicePdfService()
    {
    }

    public async Task<byte[]> GeneratePdfAsync(InvoiceDetailDto invoice, CopyType copyType = CopyType.Original, DateDisplayPreference preference = DateDisplayPreference.Gregorian, CancellationToken ct = default)
    {
        return await Task.Run(() => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Inter"));

                page.Header().Element(c => ComposeCopyWatermark(c, copyType));
                page.Content().Element(c => ComposeContent(c, invoice));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf(), ct);
    }

    private static void ComposeCopyWatermark(IContainer container, CopyType copyType)
    {
        var (label, color) = copyType switch
        {
            CopyType.Original => ("ORIGINAL", Colors.Blue.Darken3),
            CopyType.Duplicate => ("DUPLICATE", Colors.Orange.Darken2),
            CopyType.Triplicate => ("TRIPLICATE", Colors.Red.Darken2),
            _ => ("", Colors.Black)
        };

        container.AlignRight().Text(label).FontSize(11).Bold().FontColor(color);
    }

    private void ComposeContent(IContainer container, InvoiceDetailDto invoice)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeHeader(c, invoice));
            col.Item().PaddingVertical(10).Element(c => ComposeSellerBuyer(c, invoice));
            col.Item().PaddingVertical(4).Element(c => ComposeProjectInfo(c, invoice));
            col.Item().Element(c => ComposeItemsTable(c, invoice));
            col.Item().PaddingTop(10).AlignRight().Element(c => ComposeTotals(c, invoice));
            col.Item().PaddingTop(8).Element(c => ComposeAmountInWords(c, invoice));
            col.Item().PaddingTop(20).Element(c => ComposeSignature(c));
        });
    }

    private void ComposeHeader(IContainer container, InvoiceDetailDto invoice)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                if (invoice.BillType == BillType.VatBill)
                {
                    col.Item().Text("TAX INVOICE / कर बीजक").FontSize(20).Bold().FontColor(Colors.Black);
                }
                else
                {
                    col.Item().Text("BILL / बिल").FontSize(20).Bold().FontColor(Colors.Black);
                }

                col.Item().PaddingTop(4).Text($"Invoice No: {invoice.InvoiceNumber}").FontSize(10);
                col.Item().Text($"Date (BS): {invoice.DateBS}").FontSize(10);
                col.Item().Text($"Date (AD): {invoice.IssueDate:yyyy-MM-dd}").FontSize(10);
                col.Item().Text($"Fiscal Year: {invoice.FiscalYear}").FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            if (!string.IsNullOrEmpty(invoice.SellerLogoBase64))
            {
                var base64Data = invoice.SellerLogoBase64.Contains(',')
                    ? invoice.SellerLogoBase64.Split(',')[1]
                    : invoice.SellerLogoBase64;
                var imageBytes = Convert.FromBase64String(base64Data);
                row.ConstantItem(70).Image(imageBytes).FitArea();
            }

            row.ConstantItem(180).Column(col =>
            {
                col.Item().AlignRight().Text(invoice.CompanyName).FontSize(12).Bold();
                col.Item().AlignRight().Text(invoice.CompanyAddress).FontSize(9);
                col.Item().AlignRight().Text($"PAN/VAT: {invoice.CompanyPan}").FontSize(9);
                if (!string.IsNullOrEmpty(invoice.CompanyPhone))
                    col.Item().AlignRight().Text($"Phone: {invoice.CompanyPhone}").FontSize(9);
            });
        });
    }

    private void ComposeSellerBuyer(IContainer container, InvoiceDetailDto invoice)
    {
        container.Background(Colors.Grey.Lighten4).Padding(8).Column(col =>
        {
            col.Item().Text("Buyer:").Bold().FontSize(10);
            col.Item().PaddingTop(2).Text(invoice.ClientName).FontSize(10);
            if (!string.IsNullOrEmpty(invoice.BuyerAddress))
                col.Item().Text(invoice.BuyerAddress).FontSize(9);
            if (!string.IsNullOrEmpty(invoice.BuyerPan))
                col.Item().Text($"PAN: {invoice.BuyerPan}").FontSize(9);
        });
    }

    private static void ComposeProjectInfo(IContainer container, InvoiceDetailDto invoice)
    {
        if (string.IsNullOrEmpty(invoice.ProjectName) && string.IsNullOrEmpty(invoice.ProjectRemark))
            return;

        container.Background(Colors.Grey.Lighten5).Padding(6).Column(col =>
        {
            if (!string.IsNullOrEmpty(invoice.ProjectName))
            {
                col.Item().Row(row =>
                {
                    row.AutoItem().Text("Project: ").Bold().FontSize(9);
                    row.RelativeItem().Text(invoice.ProjectName).FontSize(9);
                });
            }
            if (!string.IsNullOrEmpty(invoice.ProjectRemark))
            {
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.AutoItem().Text("Remark: ").Bold().FontSize(9);
                    row.RelativeItem().Text(invoice.ProjectRemark).FontSize(9);
                });
            }
        });
    }

    private void ComposeItemsTable(IContainer container, InvoiceDetailDto invoice)
    {
        var hasHsCode = invoice.Items.Any(i => !string.IsNullOrEmpty(i.HsCode));
        var hasProject = invoice.Items.Any(i => !string.IsNullOrEmpty(i.ProjectName));

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(3);
                if (hasHsCode)
                    columns.ConstantColumn(55);
                if (hasProject)
                    columns.RelativeColumn(1);
                columns.ConstantColumn(40);
                columns.ConstantColumn(50);
                columns.ConstantColumn(65);
                columns.ConstantColumn(70);
            });

            table.Header(header =>
            {
                void HeaderCell(string text)
                {
                    header.Cell().Background(Colors.Black).Padding(4).Text(text).FontColor(Colors.White).Bold().FontSize(8).AlignCenter();
                }

                HeaderCell("SN");
                HeaderCell("Description");
                if (hasHsCode) HeaderCell("HS Code");
                if (hasProject) HeaderCell("Project");
                HeaderCell("Qty");
                HeaderCell("Unit");
                HeaderCell("Rate");
                HeaderCell("Amount");
            });

            foreach (var item in invoice.Items)
            {
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.LineNo.ToString()).FontSize(8).AlignCenter();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.Description).FontSize(8);
                if (hasHsCode)
                    table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.HsCode ?? "").FontSize(8).AlignCenter();
                if (hasProject)
                    table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.ProjectName ?? "").FontSize(8).AlignCenter();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.Quantity.ToString("N2")).FontSize(8).AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(string.IsNullOrEmpty(item.Unit) ? "-" : item.Unit).FontSize(8).AlignCenter();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.UnitPrice.ToString("N2")).FontSize(8).AlignRight();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(item.Amount.ToString("N2")).FontSize(8).AlignRight();
            }
        });
    }

    private void ComposeTotals(IContainer container, InvoiceDetailDto invoice)
    {
        container.Width(280).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Subtotal:").Bold().FontSize(9);
                row.RelativeItem().Text(invoice.Subtotal.ToString("N2")).AlignRight().FontSize(9);
            });

            if (invoice.BillType == BillType.VatBill && !invoice.IsAbbreviated)
            {
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("VAT (13%):").FontSize(9);
                    row.RelativeItem().Text(invoice.VatAmount.ToString("N2")).AlignRight().FontSize(9);
                });
            }

            if (invoice.DiscountAmount > 0)
            {
                col.Item().PaddingTop(2).Row(row =>
                {
                    row.RelativeItem().Text("Discount:").FontSize(9);
                    row.RelativeItem().Text($"({invoice.DiscountAmount:N2})").AlignRight().FontSize(9);
                });
            }

            col.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken1);

            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Grand Total:").FontSize(12).Bold();
                row.RelativeItem().Text(invoice.Total.ToString("N2")).FontSize(12).Bold().AlignRight();
            });
        });
    }

    private static void ComposeAmountInWords(IContainer container, InvoiceDetailDto invoice)
    {
        if (string.IsNullOrEmpty(invoice.GrandTotalInWords)) return;

        container.PaddingTop(2).AlignRight().Width(350).Text(x =>
        {
            x.Span("Amount in Words: ").Bold().FontSize(8);
            x.Span(invoice.GrandTotalInWords).FontSize(8).Italic();
        });
    }

    private static void ComposeSignature(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Authorized Signature:").FontSize(9).Bold();
                col.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(100).Column(col =>
            {
                col.Item().AlignCenter().Text("[SEAL]").FontSize(9).FontColor(Colors.Grey.Darken2);
            });
        });
    }
}
