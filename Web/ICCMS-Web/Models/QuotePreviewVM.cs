namespace ICCMS_Web.Models;

public class QuotePreviewVM {
  public string ProjectId { get; set; } = "";
  public string Title { get; set; } = "";
  public double MarkupPercent { get; set; } = 10;
  public double TaxPercent { get; set; } = 15;

  // client (from clients.json)
  public string? ClientId { get; set; }
  public string ClientName { get; set; } = "";
  public string? ClientOrg { get; set; }
  public string? ClientEmail { get; set; }
  public string? ClientPhone { get; set; }
  public string? ClientAddress { get; set; }

  public List<QuoteItem> Items { get; set; } = new();
}
