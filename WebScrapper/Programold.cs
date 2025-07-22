//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
////using OfficeOpenXml;
//using OpenQA.Selenium;
//using OpenQA.Selenium.Chrome;
//using Newtonsoft.Json;
//using ClosedXML.Excel;

//namespace WebsiteKeywordCollector
//{
//    class Programold
//    {
//        static void Main(string[] args)
//        {
//            Console.WriteLine("Website Keyword Collector");
//            Console.WriteLine("=========================");

//            // Get website URL from user
//            Console.Write("Enter website URL to scrape: ");
//            string url = Console.ReadLine();

//            if (string.IsNullOrWhiteSpace(url))
//            {
//                Console.WriteLine("URL cannot be empty. Exiting...");
//                return;
//            }

//            // Initialize ChromeDriver (make sure Chrome is installed)
//            IWebDriver driver = new ChromeDriver();

//            try
//            {
//                // Navigate to the website
//                driver.Navigate().GoToUrl(url);
//                Console.WriteLine($"Scraping content from: {url}");

//                // Collect data from various elements
//                var scrapedData = new List<ScrapedItem>();

//                // 1. Get all label texts
//                var labels = driver.FindElements(By.TagName("label"));
//                scrapedData.AddRange(labels.Select(l => new ScrapedItem
//                {
//                    ElementType = "Label",
//                    Text = l.Text,
//                    XPath = GetElementXPath(l)
//                }));

//                // 2. Get all table headers
//                var tableHeaders = driver.FindElements(By.XPath("//th"));
//                scrapedData.AddRange(tableHeaders.Select(th => new ScrapedItem
//                {
//                    ElementType = "Table Header",
//                    Text = th.Text,
//                    XPath = GetElementXPath(th)
//                }));

//                // 3. Get all heading elements (h1-h6)
//                for (int i = 1; i <= 6; i++)
//                {
//                    var headers = driver.FindElements(By.TagName($"h{i}"));
//                    scrapedData.AddRange(headers.Select(h => new ScrapedItem
//                    {
//                        ElementType = $"Heading H{i}",
//                        Text = h.Text,
//                        XPath = GetElementXPath(h)
//                    }));
//                }

//                // 4. Get all list items in sidebars (ul/ol in aside or with class containing "sidebar")
//                var sidebarLists = driver.FindElements(By.XPath("//aside//li | //*[contains(@class,'sidebar')]//li"));
//                scrapedData.AddRange(sidebarLists.Select(li => new ScrapedItem
//                {
//                    ElementType = "List Item",
//                    Text = li.Text,
//                    XPath = GetElementXPath(li)
//                }));

//                // 5. Get paragraph texts
//                var paragraphs = driver.FindElements(By.TagName("p"));
//                scrapedData.AddRange(paragraphs.Where(p => !string.IsNullOrWhiteSpace(p.Text))
//                    .Select(p => new ScrapedItem
//                    {
//                        ElementType = "Paragraph",
//                        Text = p.Text,
//                        XPath = GetElementXPath(p)
//                    }));

//                // Filter out empty texts and duplicates
//                var filteredData = scrapedData
//                    .Where(item => !string.IsNullOrWhiteSpace(item.Text))
//                    .GroupBy(item => item.Text)
//                    .Select(group => group.First())
//                    .ToList();

//                Console.WriteLine($"Collected {filteredData.Count} unique text items.");

//                // Export to files
//                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//                string excelFileName = $"KeywordsExport_{timestamp}.xlsx";
//                string jsonFileName = $"KeywordsExport_{timestamp}.json";

//                ExportToExcel(filteredData, excelFileName);
//                ExportToJson(filteredData, jsonFileName);

//                Console.WriteLine($"\nExport completed:");
//                Console.WriteLine($"- Excel file: {Path.GetFullPath(excelFileName)}");
//                Console.WriteLine($"- JSON file: {Path.GetFullPath(jsonFileName)}");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"An error occurred: {ex.Message}");
//            }
//            finally
//            {
//                driver.Quit();
//            }

//            Console.WriteLine("\nPress any key to exit...");
//            Console.ReadKey();
//        }

//        // Helper method to get XPath of an element
//        private static string GetElementXPath(IWebElement element)
//        {
//            return ((IJavaScriptExecutor)((IWrapsDriver)element).WrappedDriver)
//                .ExecuteScript(
//                    "function getPathTo(element) {" +
//                    "    if (element === document.body) return element.tagName;" +
//                    "    if (element.id !== '') return '//' + element.tagName + '[@id=\"' + element.id + '\"]';" +
//                    "    if (element === document.documentElement) return element.tagName;" +
//                    "" +
//                    "    var ix = 0;" +
//                    "    var siblings = element.parentNode.childNodes;" +
//                    "    for (var i = 0; i < siblings.length; i++) {" +
//                    "        var sibling = siblings[i];" +
//                    "        if (sibling === element) return getPathTo(element.parentNode) + '/' + element.tagName + '[' + (ix + 1) + ']';" +
//                    "        if (sibling.nodeType === 1 && sibling.tagName === element.tagName) ix++;" +
//                    "    }" +
//                    "}" +
//                    "return getPathTo(arguments[0]);", element) as string;
//        }

//        // Export to Excel

//        private static void ExportToExcel(List<ScrapedItem> data, string fileName)
//        {
//            using (var workbook = new XLWorkbook())
//            {
//                var worksheet = workbook.Worksheets.Add("Keywords");

//                // Add headers
//                worksheet.Cell(1, 1).Value = "Element Type";
//                worksheet.Cell(1, 2).Value = "Text Content";
//                worksheet.Cell(1, 3).Value = "XPath";

//                // Add data
//                for (int i = 0; i < data.Count; i++)
//                {
//                    worksheet.Cell(i + 2, 1).Value = data[i].ElementType;
//                    worksheet.Cell(i + 2, 2).Value = data[i].Text;
//                    worksheet.Cell(i + 2, 3).Value = data[i].XPath;
//                }

//                // Auto-fit columns
//                worksheet.Columns().AdjustToContents();

//                // Save file
//                workbook.SaveAs(fileName);
//            }
//        }
       

//        // Export to JSON
//        private static void ExportToJson(List<ScrapedItem> data, string fileName)
//        {
//            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
//            File.WriteAllText(fileName, json);
//        }
//    }

//    // Data model for scraped items
//    private class ScrapedItem
//    {
//        public string ElementType { get; set; }
//        public string Text { get; set; }
//        public string XPath { get; set; }
//    }
//}