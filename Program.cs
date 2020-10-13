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
        { UseCookies = true, CookieContainer = CookieContainer });

        private static void Main()
        {
            MassageHandler.Logger("Trying to open csv file...");
            List<InvoiceInfo> records;
            StreamReader reader = null;
            try
            {
                reader = new StreamReader("test.csv");
            }
            catch (Exception)
            {
                Console.WriteLine("Can not open test.csv. Press any key to exit.");
                Console.Read();
                Environment.Exit(-1);
            }
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Configuration.HasHeaderRecord = false;
                records = csv.GetRecords<InvoiceInfo>().ToList();
            }

            MassageHandler.Logger("csv has been loaded into memory.");
            var successfulRequest = records.Count;
            var totalCount = records.Count;
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
                new Process { StartInfo = new ProcessStartInfo("ValidCode.png") { UseShellExecute = true } }.Start();
                validCode = ValidCodeGetter.GetValidCode();
            }

            if (Directory.Exists(@".\pdf"))
            {
                MassageHandler.Logger("It seems there already has a pdf directory. Do you want to delete it?");
                MassageHandler.Logger("[Y]es or [N]o");
                var choice = Console.ReadKey();
                while (char.ToUpper(choice.KeyChar) != 'Y' && char.ToUpper(choice.KeyChar) != 'N')
                {
                    MassageHandler.Logger("You entered a wrong key. Please try again.");
                    MassageHandler.Logger("[Y]es or [N]o");
                    choice = Console.ReadKey();
                }
                if (char.ToUpper(choice.KeyChar) == 'Y')
                {
                    Directory.Delete(@".\pdf", true);
                    MassageHandler.Logger("pdf directory has been deleted.");
                }
                else if (char.ToUpper(choice.KeyChar) == 'N')
                {
                    MassageHandler.Logger("Keep pdf directory.");
                }

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
                var ticketType = ticket.bz[4..10];
                if (!invoiceTypeCollection.Contains(ticketType))
                {
                    MassageHandler.Logger($"Folder {ticketType} not exist, create now.");
                    Directory.CreateDirectory(@$".\pdf\{ticketType}");
                    invoiceTypeCollection.Add(ticketType);
                }

                MainClient.PdfDownloadAsync(ticket.pdfurl, $"{ticket.fpdm}_{ticket.fphm}", data.id,
                    ticketType);

                MassageHandler.Logger($"{currentStatus}");
            }

            MassageHandler.Logger("Now ReDownloading.");
            MainClient.ReDownload();
            var finish = DateTime.UtcNow; // Finish time
            MassageHandler.Logger("Task finished");
            MassageHandler.Logger(
                $"Total Rows:{totalCount}\tSuccessful Request:{successfulRequest}\tSuccessful rate:{(double)successfulRequest / (double)totalCount:P}\tTotal time:{finish - start}");
            Console.WriteLine("Press any key to exit.");
            Console.ReadLine();
        }
    }
}