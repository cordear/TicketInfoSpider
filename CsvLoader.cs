using CsvHelper.Configuration.Attributes;

namespace TicketInfoSpider
{
    internal class InvoiceInfo
    {
        [Index(0)] public string id { set; get; }

        [Index(1)] public string price { set; get; }
    }
}