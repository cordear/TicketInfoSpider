using System;
using System.Collections.Generic;
using System.Data;
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

            var validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
            MassageHandler.Logger(
                ValidCodeGetter.SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result)
                    ? "Valid code has been save as ValidCode.png. Open ValidCode.png and enter the valid code below."
                    : "Can't get the valid code!");

            var validCode = ValidCodeGetter.GetValidCode();
            while (!ValidCodeGetter.IsValidCodeCorrect(dataTable.Rows[0][0].ToString(), validCode,
                dataTable.Rows[0][1].ToString(), MainClient))
            {
                MassageHandler.Logger("It seems you entered a wrong valid code, now try again.");
                validCodeResponse = MainClient.GetAsync(InvoiceInfoApi.ValidateCode).Result;
                MassageHandler.Logger(
                    ValidCodeGetter.SaveValidCodePng(validCodeResponse.Content.ReadAsByteArrayAsync().Result)
                        ? "Valid code has been save as ValidCode.png. Open ValidCode.png and enter the valid code below."
                        : "Can't get the valid code!");
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

                if (!invoiceTypeCollection.Contains(ticketDataCollection.rtnData[0].bz[4..10]))
                {
                    MassageHandler.Logger($"Folder {ticketDataCollection.rtnData[0].bz[4..10]} not exist, create now.");
                    Directory.CreateDirectory(@$".\pdf\{ticketDataCollection.rtnData[0].bz[4..10]}");
                    invoiceTypeCollection.Add(ticketDataCollection.rtnData[0].bz[4..10]);
                }

                foreach (var ticket in ticketDataCollection.rtnData)
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
        }
    }
}