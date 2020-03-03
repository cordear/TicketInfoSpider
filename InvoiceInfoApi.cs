using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TicketInfoSpider
{
    public static class InvoiceInfoApi
    {
        public static Uri ValidateCode =>
            new Uri("http://www.baiwang.com/cloudapi/cterminal/api/service/fpcyvalidateCode");

        public static Uri AcquireTicketData =>
            new Uri("http://www.baiwang.com/cloudapi/cterminal/api/service/queryfpbybd");
    }

    public class InvoiceDataRequestMessage : HttpRequestMessage
    {
        public InvoiceDataRequestMessage(string ticketId, string validCode, string price)
        {
            RequestUri = InvoiceInfoApi.AcquireTicketData;
            Method = HttpMethod.Post;
            Content = new StringContent(
                $"bdbh={ticketId}&danHao=01&yzm={validCode}&cxlx=1&sfzjlx=01&zjhm=&fpje={price}&jesq=1", Encoding.UTF8,
                "application/x-www-form-urlencoded");
            Headers.Add("Origin", "http://www.baiwang.com");
            Headers.Add("Referer", "http://www.baiwang.com/cloudpages/cterminal-res/cterminal/fpcy.html");
            Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
            Headers.Add("X-Requested-With", "XMLHttpRequest");
            Headers.Add("Proxy-Connection", "keep-alive");
        }
    }

    public delegate void PdfDownloadFailedEventHandler(PdfDownloadFailedEventArgs e);

    public class PdfDownloadFailedEventArgs : EventArgs
    {
        public string Massage { set; get; }
        public Uri Url { set; get; }
    }

    public delegate void InvoiceInvoiceDataRequestFailedEventHandler(InvoiceInvoiceDataRequestFailedEventArgs e);

    public class InvoiceInvoiceDataRequestFailedEventArgs : EventArgs
    {
        public string TicketId { set; get; }
        public string Massage { set; get; }
    }

    public class InvoiceGetterHttpClient : HttpClient
    {
        public InvoiceGetterHttpClient(SocketsHttpHandler socketsHttpHandler) : base(socketsHttpHandler)
        {
            DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
            DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7");
            DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        }

        public event PdfDownloadFailedEventHandler PdfDownloadFailed;
        public event InvoiceInvoiceDataRequestFailedEventHandler InvoiceInvoiceDataRequestFailed;

        public async void PdfDownloadAsync(string url, string fileName)
        {
            try
            {
                var data = await GetByteArrayAsync(url);
                var fileStream = new FileStream($".\\pdf\\{fileName}.pdf", FileMode.Create);
                var bufferedStream = new BufferedStream(fileStream);
                bufferedStream.Write(data);
                bufferedStream.Flush();
                bufferedStream.Close();
                fileStream.Close();
            }
            catch (Exception)
            {
                if (PdfDownloadFailed != null)
                {
                    var e = new PdfDownloadFailedEventArgs {Url = new Uri(url), Massage = "Download failed"};
                    PdfDownloadFailed.Invoke(e);
                }
            }
        }

        public async Task<TicketDataCollection> InvoiceDataRequestAsync(string ticketId, string validCode,
            string price)
        {
            try
            {
                var responseMessage = await SendAsync(new InvoiceDataRequestMessage(ticketId, validCode,
                    price));
                var jsonData = responseMessage.Content.ReadAsStringAsync().Result;
                var ticketDataCollection =
                    JsonConvert.DeserializeObject<TicketDataCollection>(jsonData);
                return ticketDataCollection;
            }
            catch (Exception)
            {
                if (InvoiceInvoiceDataRequestFailed != null)
                {
                    var e = new InvoiceInvoiceDataRequestFailedEventArgs
                        {TicketId = ticketId, Massage = "Request Failed"};
                    this.InvoiceInvoiceDataRequestFailed.Invoke(e);
                }

                return null;
            }
        }
    }
}