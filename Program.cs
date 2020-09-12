using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
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

            var validCode = "";
            while (validCode == "" || !ValidCodeGetter.IsValidCodeCorrect(dataTable.Rows[0][0].ToString(), validCode,
                dataTable.Rows[0][1].ToString(), MainClient))
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

                var ticket = ticketDataCollection.rtnData[0];
                if (!invoiceTypeCollection.Contains(ticket.bz[4..10]))
                {
                    MassageHandler.Logger($"Folder {ticket.bz[4..10]} not exist, create now.");
                    Directory.CreateDirectory(@$".\pdf\{ticket.bz[4..10]}");
                    invoiceTypeCollection.Add(ticket.bz[4..10]);
                }

                MainClient.PdfDownloadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}", row[0].ToString(),
                    ticket.bz[4..10]);

                MassageHandler.Logger($"{currentStatus}");
            }

            MassageHandler.Logger("Now ReDownloading.");
            MainClient.ReDownload();
            var finish = DateTime.UtcNow; // Finish time
            MassageHandler.Logger("Task finished");
            MassageHandler.Logger(
                $"Total Rows:{dataTable.Rows.Count}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double) successfulRequest / (double) dataTable.Rows.Count:P}\tTotal time:{finish - start}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}