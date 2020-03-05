using System;
using System.Data;
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
            MassageHandler.Logger("Trying to open csv file...");
            var dataTable = CsvLoader.Csv2DataTable("test.csv");
            MassageHandler.Logger("csv has been loaded into memory.");
            var successfulRequest = dataTable.Rows.Count;
            var completeNumber = 0;
            Console.WriteLine($"Rows:{successfulRequest}");
            MassageHandler.Logger("Trying to get valid code, please wait...");

            var validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
            MassageHandler.Logger(
                ValidCodeGetter.SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result)
                    ? "Valid code has been save as ValidCode.png. Open ValidCode.png and enter the valid code below."
                    : "Can't get the valid code!");

            var validCode = ValidCodeGetter.GetValidCode();
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
            MassageHandler.Logger("Task finished");
            Console.WriteLine(
                $"Total Rows:{dataTable.Rows.Count}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double) successfulRequest / (double) dataTable.Rows.Count:P}\tTotal time:{finish - start}");
        }
    }
}