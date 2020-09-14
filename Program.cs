using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using CsvHelper;

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
            List<InvoiceInfo> records;
            using (var reader = new StreamReader("test.csv"))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                records = csv.GetRecords<InvoiceInfo>().ToList();
            }

            MassageHandler.Logger("csv has been loaded into memory.");
            var successfulRequest = records.Count + 1;
            var totalCount = records.Count + 1;
            var completeNumber = 0;
            Console.WriteLine($"Rows:{successfulRequest}");
            MassageHandler.Logger("Trying to get valid code, please wait...");

            var validCode = "";
            while (validCode == "" || !ValidCodeGetter.IsValidCodeCorrect(records[0].id, validCode,
                records[0].price, MainClient))
            {
                if (validCode != "") MassageHandler.Logger("It seems you entered a wrong valid code, now try again.");
                var validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
                MassageHandler.Logger(
                    ValidCodeGetter.SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result)
                        ? "Valid code has been save as ValidCode.png. Enter the valid code below."
                        : "Can't get the valid code!");
                new Process {StartInfo = new ProcessStartInfo("ValidCode.png") {UseShellExecute = true}}.Start();
                validCode = ValidCodeGetter.GetValidCode();
            }

            var start = DateTime.UtcNow; // Start time
            var invoiceTypeCollection = new List<string>();
            foreach (var data in records)
            {
                completeNumber++;
                var currentStatus = $"[{completeNumber}/{totalCount}]";
                var ticketDataCollection =
                    MainClient.InvoiceDataRequestAsync(data.id, validCode, data.price).Result;
                if (ticketDataCollection == null || ticketDataCollection.rtnCode == "-1")
                {
                    successfulRequest -= 1;
                    continue;
                }

                var ticket = ticketDataCollection.rtnData[0];
                if (!invoiceTypeCollection.Contains(ticket.bz[4..10]))
                {
                    MassageHandler.Logger($"Folder {ticket.bz[4..10]} not exist, create now.");
                    Directory.CreateDirectory(@$".\pdf\{ticket.bz[4..10]}");
                    invoiceTypeCollection.Add(ticket.bz[4..10]);
                }

                MainClient.PdfDownloadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}", data.id,
                    ticket.bz[4..10]);

                MassageHandler.Logger($"{currentStatus}");
            }

            MassageHandler.Logger("Now ReDownloading.");
            MainClient.ReDownload();
            var finish = DateTime.UtcNow; // Finish time
            MassageHandler.Logger("Task finished");
            MassageHandler.Logger(
                $"Total Rows:{totalCount}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double) successfulRequest / (double) dataTable.Rows.Count:P}\tTotal time:{finish - start}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}