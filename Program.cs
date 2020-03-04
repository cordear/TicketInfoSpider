using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace TicketInfoSpider
{
    internal class Program
    {
        private static readonly CookieContainer CookieContainer = new CookieContainer();

        private static readonly InvoiceGetterHttpClient MainClient = new InvoiceGetterHttpClient(new SocketsHttpHandler
            {UseCookies = true, CookieContainer = CookieContainer});

        private static void Main()
        {
            MainClient.OnPdfDownLoadSuccess += MassageHandler.Logger;
            MainClient.OnInvoiceInvoiceDataRequestFailed += MassageHandler.Logger;
            MainClient.OnPdfDownloadFailed += MassageHandler.Logger;

            Logger("Trying to open csv file...");
            var dataTable = CsvLoader.Csv2DataTable("test.csv");
            Logger("csv has been loaded into memory.");
            var successfulRequest = dataTable.Rows.Count;
            var completeNumber = 0;
            Console.WriteLine($"Rows:{successfulRequest}");
            Logger("Trying to get valid code, please wait...");

            var validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
            Logger(SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result)
                ? "Valid code has been save as ValidCode.png. Open ValidCode.png and enter the valid code below."
                : "Can't get the valid code!");

            var validCode = GetValidCode();
            var start = DateTime.UtcNow; // Start time

            foreach (DataRow row in dataTable.Rows)
            {
                completeNumber++;
                var currentStatus = $"[{completeNumber}/{dataTable.Rows.Count}]";
                var ticketDataCollection =
                    MainClient.InvoiceDataRequestAsync(row[0].ToString(), validCode, row[1].ToString()).Result;
                if (ticketDataCollection == null || ticketDataCollection.rtnCode == "-1")
                {
                    successfulRequest -= 1;
                    continue;
                }

                foreach (var ticket in ticketDataCollection.rtnData)
                    MainClient.PdfDownloadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}", row[0].ToString());

                Console.WriteLine($"\t\t\t{currentStatus}");
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
    }
}