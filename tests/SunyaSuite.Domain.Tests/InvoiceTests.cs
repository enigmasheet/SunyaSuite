using FluentAssertions;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using Xunit;

namespace SunyaSuite.Domain.Tests;

public class InvoiceTests
{
    private Invoice CreateInvoice(BillType billType = BillType.PanBill, bool isAbbreviated = false)
    {
        return new Invoice
        {
            BillType = billType,
            IsAbbreviated = isAbbreviated,
            TaxRate = 13m,
            Items =
            [
                new() { Quantity = 1, UnitPrice = 1000m },
                new() { Quantity = 1, UnitPrice = 500m },
            ],
        };
    }

    [Fact]
    public void Recalculate_PanBill_SumsItemsNoVat()
    {
        var invoice = CreateInvoice(BillType.PanBill);
        invoice.DiscountAmount = 50m;

        invoice.Recalculate();

        invoice.Subtotal.Should().Be(1500m);
        invoice.VatAmount.Should().Be(0m);
        invoice.Total.Should().Be(1450m);
    }

    [Fact]
    public void Recalculate_VatBill_AppliesVat()
    {
        var invoice = CreateInvoice(BillType.VatBill);
        invoice.DiscountAmount = 0m;

        invoice.Recalculate();

        invoice.Subtotal.Should().Be(1500m);
        invoice.VatAmount.Should().Be(195m); // 13% of 1500
        invoice.Total.Should().Be(1695m);
    }

    [Fact]
    public void Recalculate_VatBillAbbreviated_NoVat()
    {
        var invoice = CreateInvoice(BillType.VatBill, isAbbreviated: true);
        invoice.DiscountAmount = 0m;

        invoice.Recalculate();

        invoice.Subtotal.Should().Be(1500m);
        invoice.VatAmount.Should().Be(0m);
        invoice.Total.Should().Be(1500m);
    }

    [Fact]
    public void RecordPayment_IncreasesAmountPaid()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();

        invoice.RecordPayment(500m);

        invoice.AmountPaid.Should().Be(500m);
    }

    [Fact]
    public void RecordPayment_NegativeAmount_Throws()
    {
        var invoice = CreateInvoice();
        var act = () => invoice.RecordPayment(-100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RecordPayment_ZeroAmount_Throws()
    {
        var invoice = CreateInvoice();
        var act = () => invoice.RecordPayment(0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ReversePayment_DecreasesAmountPaid()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();
        invoice.RecordPayment(1000m);

        invoice.ReversePayment(400m);

        invoice.AmountPaid.Should().Be(600m);
    }

    [Fact]
    public void ReversePayment_NegativeAmount_Throws()
    {
        var invoice = CreateInvoice();
        var act = () => invoice.ReversePayment(-100m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void IsFullyPaid_WhenAmountPaidEqualsTotal_ReturnsTrue()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();

        invoice.RecordPayment(invoice.Total);

        invoice.IsFullyPaid.Should().BeTrue();
    }

    [Fact]
    public void IsFullyPaid_WhenAmountPaidLessThanTotal_ReturnsFalse()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();

        invoice.RecordPayment(invoice.Total - 1m);

        invoice.IsFullyPaid.Should().BeFalse();
    }

    [Fact]
    public void Recalculate_EmptyItems_TotalIsZeroMinusDiscount()
    {
        var invoice = CreateInvoice();
        invoice.Items.Clear();
        invoice.DiscountAmount = 100m;

        invoice.Recalculate();

        invoice.Subtotal.Should().Be(0m);
        invoice.Total.Should().Be(-100m);
    }

    [Fact]
    public void RecordPayment_OverPayment_Throws()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();

        var act = () => invoice.RecordPayment(invoice.Total + 1m);

        act.Should().Throw<InvalidOperationException>().WithMessage("*exceed invoice total*");
    }

    [Fact]
    public void RecordPayment_ExactTotal_Allowed()
    {
        var invoice = CreateInvoice();
        invoice.Recalculate();

        invoice.RecordPayment(invoice.Total);

        invoice.AmountPaid.Should().Be(invoice.Total);
        invoice.IsFullyPaid.Should().BeTrue();
    }
}
