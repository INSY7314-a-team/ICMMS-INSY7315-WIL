using ICCMS_API.Models;
using System.Linq;

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
            quotation.TaxTotal = quotation.Items.Sum(item => (item.LineTotal * (1 + quotation.MarkupRate)) * item.TaxRate);
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

            // Apply markup to subtotal first
            var subtotalWithMarkup = invoice.Subtotal * invoice.MarkupRate;
            // Apply markup to subtotal first
            var subtotalWithMarkup = invoice.Subtotal * invoice.MarkupRate;

            // Calculate tax total on the marked-up subtotal
            invoice.TaxTotal = invoice.Items.Sum(item => (item.LineTotal * invoice.MarkupRate) * item.TaxRate);

            // Calculate total amount
            invoice.TotalAmount = subtotalWithMarkup + invoice.TaxTotal;

            // Sync legacy fields
            invoice.Amount = invoice.Subtotal;
            invoice.TaxAmount = invoice.TaxTotal;

            // Update timestamp
            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }
}