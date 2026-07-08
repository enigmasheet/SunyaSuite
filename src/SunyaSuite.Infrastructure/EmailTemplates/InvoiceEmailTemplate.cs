namespace SunyaSuite.Infrastructure.EmailTemplates;

public static class InvoiceEmailTemplate
{
    public static string BuildInvoiceNotification(string clientName, string invoiceNumber, decimal amount, DateTime issueDate)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /></head>
        <body style="font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
                            <tr>
                                <td style="background-color: #1565c0; padding: 24px 32px;">
                                    <h1 style="color: #ffffff; margin: 0; font-size: 22px;">Sunya<span style="color: #90caf9;">Suite</span></h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 32px;">
                                    <h2 style="color: #1565c0; margin: 0 0 16px 0;">Invoice Attached</h2>
                                    <p style="color: #333333; font-size: 15px; line-height: 1.6;">Dear {clientName},</p>
                                    <p style="color: #333333; font-size: 15px; line-height: 1.6;">
                                        Please find attached invoice <strong>{invoiceNumber}</strong> for 
                                        <strong style="color: #1565c0;">{amount:N2}</strong> issued on 
                                        <strong>{issueDate:MMMM dd, yyyy}</strong>.
                                    </p>
                                    <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
                                    <p style="color: #777777; font-size: 13px; line-height: 1.5;">
                                        SunyaSuite — Client Management &amp; Billing<br />
                                        If you have any questions, please reply to this email.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }

    public static string BuildOverdueNotification(string clientName, string invoiceNumber, decimal amount, DateOnly dueDate)
    {
        return $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8" /></head>
        <body style="font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background-color: #f5f5f5; padding: 40px 20px;">
                <tr>
                    <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08);">
                            <tr>
                                <td style="background-color: #2e7d32; padding: 24px 32px;">
                                    <h1 style="color: #ffffff; margin: 0; font-size: 22px;">Sunya<span style="color: #a5d6a7;">Suite</span></h1>
                                </td>
                            </tr>
                            <tr>
                                <td style="padding: 32px;">
                                    <h2 style="color: #d32f2f; margin: 0 0 16px 0;">Payment Overdue</h2>
                                    <p style="color: #333333; font-size: 15px; line-height: 1.6;">Dear {clientName},</p>
                                    <p style="color: #333333; font-size: 15px; line-height: 1.6;">
                                        This is a reminder that invoice <strong>{invoiceNumber}</strong> for 
                                        <strong style="color: #2e7d32;">{amount:C}</strong> was due on 
                                        <strong>{dueDate:MMMM dd, yyyy}</strong> and is now overdue.
                                    </p>
                                    <p style="color: #333333; font-size: 15px; line-height: 1.6;">
                                        Please remit payment at your earliest convenience to avoid any service interruption.
                                    </p>
                                    <table cellpadding="0" cellspacing="0" style="margin: 24px 0;">
                                        <tr>
                                            <td align="center" style="background-color: #2e7d32; border-radius: 4px; padding: 12px 32px;">
                                                <a href="#" style="color: #ffffff; text-decoration: none; font-size: 15px; font-weight: 600;">View Invoice</a>
                                            </td>
                                        </tr>
                                    </table>
                                    <hr style="border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;" />
                                    <p style="color: #777777; font-size: 13px; line-height: 1.5;">
                                        SunyaSuite — Client Management &amp; Billing<br />
                                        If you have any questions, please reply to this email.
                                    </p>
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>
        </body>
        </html>
        """;
    }
}
