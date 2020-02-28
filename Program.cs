using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TicketInfoSpider
{
    internal class Program
    {
        private static readonly CookieContainer CookieContainer = new CookieContainer();

        private static readonly InvoiceGetterHttpClient MainClient = new InvoiceGetterHttpClient(new SocketsHttpHandler
            {UseCookies = true, CookieContainer = CookieContainer});

        private static void Main()
        {
            Logger("Trying to open csv file...");
            var dataTable = CsvLoader.Csv2DataTable("test.csv");
            Logger("csv has been loaded into memory.");
            var successfulRequest = dataTable.Rows.Count;
            var completeNumber = 0;
            Console.WriteLine($"Rows:{successfulRequest}");
            Logger("Trying to get valid code, please wait...");

            var validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
            if (SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result))
                Logger("Valid code has been save as ValidCode.png");
            Logger("Open ValidCode.png and enter the valid code below");

            var validCode = GetValidCode();
            var start = DateTime.UtcNow; // Start time

            foreach (DataRow row in dataTable.Rows)
            {
                completeNumber++;
                var ticketDataCollection =
                    TicketDataRequestAsync(row[0].ToString(), validCode, row[1].ToString()).Result;
                if (ticketDataCollection == null || ticketDataCollection.rtnCode == "-1")
                {
                    Logger($"FAILED : InvoiceId={row[0]} [{completeNumber}/{dataTable.Rows.Count}]");
                    successfulRequest -= 1;
                    continue;
                }

                foreach (var ticket in ticketDataCollection.rtnData)
                    PdfDownLoadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}");
                Logger($"SUCCESS: InvoiceId={row[0]} [{completeNumber}/{dataTable.Rows.Count}]");
            }

            var finish = DateTime.UtcNow; // Finish time
            Logger("Task finished");
            Console.WriteLine(
                $"Total Rows:{dataTable.Rows.Count}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double) successfulRequest / (double) dataTable.Rows.Count:P}\tTotal time:{finish - start}");
        }

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
                Logger("You entered an illegal valid code, try again");
                validCode = Console.ReadLine();
            }

            return validCode;
        }

        public static void Logger(string s)
        {
            Console.WriteLine($"{DateTime.UtcNow}: {s}");
        }

        public static async void PdfDownLoadAsync(string url, string fileName)
        {
            try
            {
                var data = await MainClient.GetByteArrayAsync(url);
                var fileStream = new FileStream($".\\pdf\\{fileName}.pdf", FileMode.Create);
                var bufferedStream = new BufferedStream(fileStream);
                bufferedStream.Write(data);
                bufferedStream.Flush();
                bufferedStream.Close();
                fileStream.Close();
            }
            catch (Exception)
            {
                Logger("DownLoad Failed.");
            }
        }

        public static async Task<TicketDataCollection> TicketDataRequestAsync(string ticketId, string validCode,
            string price)
        {
            try
            {
                var responseMessage = await MainClient.SendAsync(new InvoiceDataRequestMessage(ticketId, validCode,
                    price));
                var jsonData = responseMessage.Content.ReadAsStringAsync().Result;
                var ticketDataCollection =
                    JsonConvert.DeserializeObject<TicketDataCollection>(jsonData);
                return ticketDataCollection;
            }
            catch (Exception)
            {
                Logger("Request Failed");
                return null;
            }
        }
    }
}