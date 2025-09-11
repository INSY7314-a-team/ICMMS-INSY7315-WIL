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

            // Calculate tax total
            quotation.TaxTotal = quotation.Items.Sum(item => item.LineTotal * item.TaxRate);

            // Calculate grand total
            quotation.GrandTotal = quotation.Subtotal + quotation.TaxTotal;

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

            // Calculate tax total
            invoice.TaxTotal = invoice.Items.Sum(item => item.LineTotal * item.TaxRate);

            // Calculate total amount
            invoice.TotalAmount = invoice.Subtotal + invoice.TaxTotal;

            // Sync legacy fields
            invoice.Amount = invoice.Subtotal;
            invoice.TaxAmount = invoice.TaxTotal;

            // Update timestamp
            invoice.UpdatedAt = DateTime.UtcNow;
        }
    }
}
