using System;
using System.Collections.Generic;
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
        public string FileName { set; get; }
        public string InvoiceType { set; get; }
    }

    public delegate void InvoiceDataRequestFailedEventHandler(InvoiceDataRequestFailedEventArgs e);

    public class InvoiceDataRequestFailedEventArgs : EventArgs
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
        private readonly List<PdfDownloadFailedEventArgs> _reTryList = new List<PdfDownloadFailedEventArgs>();

        public InvoiceGetterHttpClient(SocketsHttpHandler socketsHttpHandler) : base(socketsHttpHandler)
        {
            DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36");
            DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,ja;q=0.7");
            DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            OnPdfDownLoadSuccess += MassageHandler.Logger;
            OnInvoiceInvoiceDataRequestFailed += MassageHandler.Logger;
            OnPdfDownloadFailed += MassageHandler.Logger;
            OnPdfDownloadFailed += e => { _reTryList.Add(e); };
        }

        public void ReDownload()
        {
            MassageHandler.Logger(_reTryList.Count == 0
                ? "There are no invoice need to re-download."
                : $"there are {_reTryList.Count} invoice(s) need to re-download.");
            if (_reTryList.Count == 0) return;
            foreach (var e in _reTryList) PdfDownloadAsync(e.Url, e.FileName, e.TicketId, e.InvoiceType, true);
        }

        public event PdfDownloadFailedEventHandler OnPdfDownloadFailed;
        public event InvoiceDataRequestFailedEventHandler OnInvoiceInvoiceDataRequestFailed;
        public event PdfDownloadSuccessEventHandler OnPdfDownLoadSuccess;

        public async void PdfDownloadAsync(string url, string fileName, string ticketId, string invoiceType,
            bool isReDownload = false)
        {
            try
            {
                var data = await GetByteArrayAsync(url);
                var fileStream = new FileStream(@$".\pdf\{invoiceType}\{fileName}.pdf", FileMode.Create);
                var bufferedStream = new BufferedStream(fileStream);
                bufferedStream.Write(data);
                bufferedStream.Flush();
                bufferedStream.Close();
                fileStream.Close();
                OnPdfDownLoadSuccess?.Invoke(new PdfDownloadSuccessEventArgs {TicketId = ticketId});
            }
            catch (Exception)
            {
                var e = new PdfDownloadFailedEventArgs
                {
                    Url = url, Massage = "Download failed", TicketId = ticketId, FileName = fileName,
                    InvoiceType = invoiceType
                };
                if (isReDownload) MassageHandler.Logger(e);
                else OnPdfDownloadFailed?.Invoke(e);
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
                    OnInvoiceInvoiceDataRequestFailed?.Invoke(new InvoiceDataRequestFailedEventArgs
                        {TicketId = ticketId, Massage = "Invoice doesn't exist."});

                return ticketDataCollection;
            }
            catch (Exception)
            {
                OnInvoiceInvoiceDataRequestFailed?.Invoke(new InvoiceDataRequestFailedEventArgs
                    {TicketId = ticketId, Massage = "Request Failed"});
                return null;
            }
        }

    }

    internal static class MassageHandler
    {
        public static void Logger(InvoiceDataRequestFailedEventArgs e)
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

    internal static class ValidCodeGetter
    {
        public static bool SaveValidCodePng(byte[] bytes)
        {
            if (bytes.Length == 0) return false;
            var fileStream = new FileStream("ValidCode.png", FileMode.Create);
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

        public static bool IsValidCodeCorrect(string ticketId, string validCode, string price,
            InvoiceGetterHttpClient client)
        {
            var testCase = client.InvoiceDataRequestAsync(ticketId, validCode, price).Result;
            if (testCase.rtnMsg == "发票查验失败，图形验证码不正确！") return false;
            return true;
        }
    }
}