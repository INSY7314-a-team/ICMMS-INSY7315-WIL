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
            var subtotalWithMarkup = quotation.Subtotal * (1 + quotation.MarkupRate);
            quotation.TaxTotal = quotation.Items.Sum(item =>
                (item.LineTotal * (1 + quotation.MarkupRate)) * item.TaxRate
            );
            quotation.GrandTotal = subtotalWithMarkup + quotation.TaxTotal;

            // Sync legacy Total field
            quotation.Total = quotation.GrandTotal;

            // Update timestamp
            quotation.UpdatedAt = DateTime.UtcNow;
        }

        public static void Recalculate(Invoice invoice)
        {
            // Calculate line totals
            foreach (var item in invoice.Items)
            {
                item.LineTotal = item.Quantity * item.UnitPrice;
            }

            // Base subtotal
            invoice.Subtotal = invoice.Items.Sum(i => i.LineTotal);

            // Apply markup correctly (1 + rate)
            invoice.SubtotalWithMarkup = invoice.Subtotal * (1 + invoice.MarkupRate);

            // Tax calculations
            invoice.TaxTotal = invoice.Items.Sum(i => (i.LineTotal * (1 + invoice.MarkupRate)) * i.TaxRate);
            invoice.TaxTotalWithMarkup = invoice.TaxTotal;

            // Totals
            invoice.TotalAmount = invoice.SubtotalWithMarkup + invoice.TaxTotal;
            invoice.Amount = invoice.Subtotal;
            invoice.TaxAmount = invoice.TaxTotal;

            // Legacy sync
            invoice.UpdatedAt = DateTime.UtcNow;
        }

    }
}
