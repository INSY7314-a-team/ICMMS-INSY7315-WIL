using System.Linq;
using ICCMS_API.Models;

namespace ICCMS_API.Helpers
{
    public static class Pricing
    {
        public static void Recalculate(Quotation quotation)
        {
            // Calculate line totals for each item
            foreach (var item in quotation.Items)
            {
                item.LineTotal = item.Quantity * item.UnitPrice;
            }

            // Calculate subtotal
            quotation.Subtotal = quotation.Items.Sum(item => item.LineTotal);

            // Apply markup to subtotal first
            var subtotalWithMarkup = quotation.Subtotal * quotation.MarkupRate;

            // Calculate tax total on the marked-up subtotal
            quotation.TaxTotal = quotation.Items.Sum(item => (item.LineTotal * quotation.MarkupRate) * item.TaxRate);

            // Calculate grand total
            quotation.GrandTotal = subtotalWithMarkup + quotation.TaxTotal;

            // Sync legacy Total field
            quotation.Total = quotation.GrandTotal;

            // Update timestamp
            quotation.UpdatedAt = DateTime.UtcNow;
        }

        public static void Recalculate(Invoice invoice)
        {
            // Calculate line totals for each item
            foreach (var item in invoice.Items)
            {
                item.LineTotal = item.Quantity * item.UnitPrice;
            }

            // Calculate subtotal
            invoice.Subtotal = invoice.Items.Sum(item => item.LineTotal);

            // Calculate subtotal with markup
            invoice.SubtotalWithMarkup = invoice.Items.Sum(item => item.LineTotal * item.Markup);

            // Calculate tax total with markup
            invoice.TaxTotalWithMarkup = invoice.Items.Sum(item =>
                item.LineTotal * item.TaxRate * item.Markup
            );

            // Calculate tax total
            invoice.TaxTotal = invoice.Items.Sum(item => item.LineTotal * item.TaxRate);

            // Calculate total amount
            invoice.TotalAmount = subtotalWithMarkup + invoice.TaxTotal;

            // Calculate total amount with markup
            invoice.TotalAmountWithMarkup = invoice.SubtotalWithMarkup + invoice.TaxTotalWithMarkup;

            // Sync legacy fields
            invoice.Amount = invoice.Subtotal;
            invoice.TaxAmount = invoice.TaxTotal;
            invoice.AmountWithMarkup = invoice.SubtotalWithMarkup;
            invoice.TaxAmountWithMarkup = invoice.TaxTotalWithMarkup;
            invoice.TotalAmountWithMarkup = invoice.TotalAmountWithMarkup;

            // Update timestamp
            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }
}
