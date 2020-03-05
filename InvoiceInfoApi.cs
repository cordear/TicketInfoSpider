using System;
using System.IO;
using System.Linq;
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
        public string Url { set; get; }
        public string TicketId { set; get; }
    }

    public delegate void InvoiceDataRequestFailedEventHandler(InvoiceInvoiceDataRequestFailedEventArgs e);

    public class InvoiceInvoiceDataRequestFailedEventArgs : EventArgs
    {
        public string TicketId { set; get; }
        public string Massage { set; get; }
    }

    public class PdfDownloadSuccessEventArgs : EventArgs
    {
        public string TicketId { set; get; }
    }

    public delegate void PdfDownloadSuccessEventHandler(PdfDownloadSuccessEventArgs e);

    public class InvoiceGetterHttpClient : HttpClient
    {
        public InvoiceGetterHttpClient(SocketsHttpHandler socketsHttpHandler) : base(socketsHttpHandler)
        {
            DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
            DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7");
            DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            OnPdfDownLoadSuccess += MassageHandler.Logger;
            OnInvoiceInvoiceDataRequestFailed += MassageHandler.Logger;
            OnPdfDownloadFailed += MassageHandler.Logger;
        }

        public event PdfDownloadFailedEventHandler OnPdfDownloadFailed;
        public event InvoiceDataRequestFailedEventHandler OnInvoiceInvoiceDataRequestFailed;
        public event PdfDownloadSuccessEventHandler OnPdfDownLoadSuccess;

        public async void PdfDownloadAsync(string url, string fileName, string ticketId)
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
                OnPdfDownLoadSuccess?.Invoke(new PdfDownloadSuccessEventArgs {TicketId = ticketId});
            }
            catch (Exception)
            {
                OnPdfDownloadFailed?.Invoke(new PdfDownloadFailedEventArgs
                    {Url = url, Massage = "Download failed", TicketId = ticketId});
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
                if (ticketDataCollection.rtnCode == "-1")
                    OnInvoiceInvoiceDataRequestFailed?.Invoke(new InvoiceInvoiceDataRequestFailedEventArgs
                        {TicketId = ticketId, Massage = "Invoice doesn't exist."});

                return ticketDataCollection;
            }
            catch (Exception)
            {
                OnInvoiceInvoiceDataRequestFailed?.Invoke(new InvoiceInvoiceDataRequestFailedEventArgs
                    {TicketId = ticketId, Massage = "Request Failed"});
                return null;
            }
        }
    }

    internal class MassageHandler
    {
        public static void Logger(InvoiceInvoiceDataRequestFailedEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow}: FAILED : InvoiceId={e.TicketId} {e.Massage}");
        }

        public static void Logger(PdfDownloadFailedEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow}: FAILED : InvoiceId={e.TicketId} Uri={e.Url} {e.Massage}");
        }

        public static void Logger(PdfDownloadSuccessEventArgs e)
        {
            Console.WriteLine($"{DateTime.UtcNow}: SUCCESS: InvoiceId={e.TicketId}");
        }

        public static void Logger(string s)
        {
            Console.WriteLine($"{DateTime.UtcNow}: {s}");
        }
    }

    internal class ValidCodeGetter
    {
        public static bool SaveValidCodePng(byte[] bytes)
        {
            if (bytes.Length == 0) return false;
            var fileStream = new FileStream("VaildCode.png", FileMode.Create);
            fileStream.Write(bytes);
            fileStream.Close();
            return true;
        }

        public static string GetValidCode()
        {
            var validCode = Console.ReadLine();
            while (validCode == null || validCode.Length != 5 || !validCode.All(char.IsLetterOrDigit))
            {
                MassageHandler.Logger("You entered an illegal valid code, try again");
                validCode = Console.ReadLine();
            }

            return validCode;
        }
    }
}