namespace SunyaSuite.Web.Client.Services;

public static class EmailTemplateHelper
{
    public static string BuildInvoiceEmailBody(string invoiceNumber, string clientName, string total, string issueDate) =>
        $@"<html><body style='font-family:sans-serif;'>
<h2>Invoice {invoiceNumber}</h2>
<p>Dear {clientName},</p>
<p>Please find your invoice attached.</p>
<table border='1' cellpadding='8' style='border-collapse:collapse;'>
<tr><th>Invoice #</th><th>Amount</th><th>Date</th></tr>
<tr><td>{invoiceNumber}</td><td>{total}</td><td>{issueDate}</td></tr>
</table>
<p>Thank you for your business.</p>
</body></html>";

    public static string BuildReceiptEmailBody(string receiptNumber, string receivedFrom, string amount, string date) =>
        $@"<html><body style='font-family:sans-serif;'>
<h2>Receipt {receiptNumber}</h2>
<p>Dear {receivedFrom},</p>
<p>Please find your receipt attached.</p>
<table border='1' cellpadding='8' style='border-collapse:collapse;'>
<tr><th>Receipt #</th><th>Amount</th><th>Date</th></tr>
<tr><td>{receiptNumber}</td><td>{amount}</td><td>{date}</td></tr>
</table>
<p>Thank you for your payment.</p>
</body></html>";
}
