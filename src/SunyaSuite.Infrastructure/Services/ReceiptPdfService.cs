using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Infrastructure.Services;

public class ReceiptPdfService : IReceiptPdfService
{
    public async Task<byte[]> GeneratePdfAsync(MoneyReceiptDetailDto receipt, CopyType copyType = CopyType.Original, DateDisplayPreference preference = DateDisplayPreference.Gregorian, CancellationToken ct = default)
    {
        return await Task.Run(() => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Inter"));

                page.Header().Element(c => ComposeCopyWatermark(c, copyType));
                page.Content().Element(c => ComposeContent(c, receipt));
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

    private void ComposeContent(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Column(col =>
        {
            col.Item().Element(c => ComposeHeader(c, receipt));
            col.Item().PaddingVertical(8).Element(c => ComposePayerInfo(c, receipt));
            col.Item().PaddingTop(6).Element(c => ComposeAmountSection(c, receipt));
            col.Item().PaddingTop(6).Element(c => ComposePaymentInfo(c, receipt));
            col.Item().PaddingTop(6).Element(c => ComposeAllocationsTable(c, receipt));
            col.Item().PaddingTop(8).AlignRight().Element(c => ComposeAllocationsTotal(c, receipt));
            col.Item().PaddingTop(24).Element(c => ComposeSignature(c, receipt));
        });
    }

    private void ComposeHeader(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("MONEY RECEIPT / रसिद").FontSize(20).Bold().FontColor(Colors.Black);
                col.Item().PaddingTop(4).Text($"Receipt No: {receipt.ReceiptNumber}").FontSize(10);
                col.Item().Text($"Date (AD): {receipt.DateAD:yyyy-MM-dd}").FontSize(10);
                col.Item().Text($"Date (BS): {receipt.DateBS}").FontSize(10);
                col.Item().Text($"Fiscal Year: {receipt.FiscalYear}").FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            if (!string.IsNullOrEmpty(receipt.SellerLogoBase64))
            {
                var base64Data = receipt.SellerLogoBase64.Contains(',')
                    ? receipt.SellerLogoBase64.Split(',')[1]
                    : receipt.SellerLogoBase64;
                var imageBytes = Convert.FromBase64String(base64Data);
                row.ConstantItem(70).Image(imageBytes).FitArea();
            }
        });
    }

    private static void ComposePayerInfo(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Background(Colors.Grey.Lighten4).Padding(8).Column(col =>
        {
            col.Item().Text("Received From:").Bold().FontSize(10);
            col.Item().PaddingTop(2).Text(receipt.ReceivedFromName).FontSize(10);
            if (!string.IsNullOrEmpty(receipt.ReceivedFromAddress))
                col.Item().Text(receipt.ReceivedFromAddress).FontSize(9);
            if (!string.IsNullOrEmpty(receipt.ReceivedFromPan))
                col.Item().Text($"PAN: {receipt.ReceivedFromPan}").FontSize(9);
        });
    }

    private static void ComposeAmountSection(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Text("Amount Received:").Bold().FontSize(11);
                row.RelativeItem().Text($"Rs. {receipt.AmountReceived:N2}").AlignRight().FontSize(11).Bold();
            });

            if (!string.IsNullOrEmpty(receipt.AmountInWords))
            {
                col.Item().PaddingTop(2).Text(x =>
                {
                    x.Span("In Words: ").Bold().FontSize(8);
                    x.Span(receipt.AmountInWords).FontSize(8).Italic();
                });
            }
        });
    }

    private static void ComposePaymentInfo(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Payment Method: {receipt.PaymentMethod}").FontSize(9);
            if (!string.IsNullOrEmpty(receipt.ReferenceNo))
                row.RelativeItem().Text($"Reference: {receipt.ReferenceNo}").AlignRight().FontSize(9);
        });
    }

    private static void ComposeAllocationsTable(IContainer container, MoneyReceiptDetailDto receipt)
    {
        if (receipt.Allocations.Count == 0) return;

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);
                columns.RelativeColumn(3);
                columns.ConstantColumn(100);
            });

            table.Header(header =>
            {
                void HeaderCell(string text)
                {
                    header.Cell().Background(Colors.Black).Padding(4).Text(text).FontColor(Colors.White).Bold().FontSize(8).AlignCenter();
                }

                HeaderCell("SN");
                HeaderCell("Invoice No");
                HeaderCell("Allocated Amount");
            });

            foreach (var (allocation, index) in receipt.Allocations.Select((a, i) => (a, i + 1)))
            {
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(index.ToString()).FontSize(8).AlignCenter();
                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(allocation.InvoiceNumber).FontSize(8);

                table.Cell().PaddingVertical(3).PaddingHorizontal(4).Text(allocation.AllocatedAmount.ToString("N2")).FontSize(8).AlignRight();
            }
        });
    }

    private static void ComposeAllocationsTotal(IContainer container, MoneyReceiptDetailDto receipt)
    {
        if (receipt.Allocations.Count == 0) return;

        container.Width(200).Row(row =>
        {
            row.RelativeItem().Text("Total:").Bold().FontSize(10);
            row.RelativeItem().Text(receipt.AmountReceived.ToString("N2")).AlignRight().Bold().FontSize(10);
        });
    }

    private static void ComposeSignature(IContainer container, MoneyReceiptDetailDto receipt)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Authorized Signature:").FontSize(9).Bold();
                col.Item().PaddingTop(25).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(120).Column(col =>
            {
                col.Item().AlignRight().Text("Received By:").FontSize(9).Bold();
                col.Item().PaddingTop(2).AlignRight().Text(receipt.ReceivedBy).FontSize(9);
                col.Item().PaddingTop(16).AlignRight().LineHorizontal(1).LineColor(Colors.Grey.Darken2);
            });
        });
    }
}
