using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ClosedXML.Excel;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace WebsiteKeywordCollector
{
    class Program
    {
        private static List<ScrapedItem> _allScrapedData = new List<ScrapedItem>();
        private static string _mainUrl;
        private static string _username;
        private static string _password;

        static void Main(string[] args)
        {
            Console.WriteLine("Website Keyword Collector");
            Console.WriteLine("=========================");

            // Get website URL from user
            Console.Write("Enter website URL to scrape: ");
            _mainUrl = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(_mainUrl))
            {
                Console.WriteLine("URL cannot be empty. Exiting...");
                return;
            }

            // Initialize ChromeDriver with options
            var options = new ChromeOptions();
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            // Set download directory
            string downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            options.AddUserProfilePreference("download.default_directory", downloadDir);
            options.AddUserProfilePreference("download.prompt_for_download", false);

            using (IWebDriver driver = new ChromeDriver(options))
            {
                try
                {
                    // Navigate to the website
                    driver.Navigate().GoToUrl(_mainUrl);
                    Console.WriteLine($"Scraping content from: {_mainUrl}");

                    // Check if login is required
                    if (IsLoginPage(driver))
                    {
                        Console.WriteLine("Login page detected.");
                        Console.Write("Enter username: ");
                        _username = Console.ReadLine();
                        Console.Write("Enter password: ");
                        _password = Console.ReadLine();

                        if (PerformLogin(driver))
                        {
                            Console.WriteLine("Login successful!");
                        }
                        else
                        {
                            Console.WriteLine("Login failed. Exiting...");
                            return;
                        }
                    }

                    // Collect data from current page
                    CollectPageData(driver);

                    // Find all navigation links
                    var navLinks = FindNavigationLinks(driver);
                    Console.WriteLine($"Found {navLinks.Count} navigation links to explore.");

                    // Visit each navigation link
                    foreach (var link in navLinks)
                    {
                        try
                        {
                            Console.WriteLine($"Navigating to: {link.Text} ({link.GetAttribute("href")})");
                            link.Click();
                            WaitForPageLoad(driver);

                            // Collect data from new page
                            CollectPageData(driver);

                            // Go back to main page
                            driver.Navigate().Back();
                            WaitForPageLoad(driver);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error navigating to {link.Text}: {ex.Message}");
                        }
                    }

                    // Perform logout if logged in
                    if (!string.IsNullOrEmpty(_username))
                    {
                        PerformLogout(driver);
                    }

                    // Filter out empty texts and duplicates
                    var filteredData = _allScrapedData
                        .Where(item => !string.IsNullOrWhiteSpace(item.Text))
                        .GroupBy(item => item.Text)
                        .Select(group => group.First())
                        .ToList();

                    Console.WriteLine($"Collected {filteredData.Count} unique text items from all pages.");

                    // Export to files in Downloads folder
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string excelFileName = Path.Combine(downloadDir, $"KeywordsExport_{timestamp}.xlsx");
                    string jsonFileName = Path.Combine(downloadDir, $"KeywordsExport_{timestamp}.json");

                    ExportToExcel(filteredData, excelFileName);
                    ExportToJson(filteredData, jsonFileName);

                    Console.WriteLine($"\nExport completed to your Downloads folder:");
                    Console.WriteLine($"- Excel file: {excelFileName}");
                    Console.WriteLine($"- JSON file: {jsonFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("Stack Trace: " + ex.StackTrace);
                }
                finally
                {
                    driver.Quit();
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static bool IsLoginPage(IWebDriver driver)
        {
            try
            {
                // Check for common login page indicators
                //return driver.FindElements(By.Id("username")).Count > 0 ||
                //      driver.FindElements(By.Id("email")).Count > 0 ||
                //      driver.FindElements(By.Id("login")).Count > 0 ||
                //      driver.FindElements(By.Id("password")).Count > 0;
                return driver.FindElements(By.Name("username")).Count > 0 ||
                       driver.FindElements(By.Name("Email")).Count > 0 ||
                       driver.FindElements(By.Name("login")).Count > 0 ||
                       driver.FindElements(By.Name("Password")).Count > 0;
            }
            catch
            {
                return false;
            }
        }

        //private static bool PerformLogin(IWebDriver driver)
        //{
        //    try
        //    {
        //        //var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(50));
        //        var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(5));

        //        // Try to find username field by various common names
        //        IWebElement usernameField = wait.Until(d =>
        //            //d.FindElement(By.Id("username")) ??
        //            //d.FindElement(By.Id("email")) ??
        //            //d.FindElement(By.Id("login")) ??
        //            //d.FindElement(By.Name("username")) ??
        //            d.FindElement(By.Name("Email")));

        //        // Try to find password field
        //        //IWebElement passwordField = driver.FindElement(By.Id("password")) ??
        //        //                          driver.FindElement(By.Name("password"));
        //        IWebElement passwordField = driver.FindElement(By.Id("Password")) ??
        //                                  driver.FindElement(By.Name("Password"));

        //        // Try to find submit button
        //        //IWebElement submitButton = driver.FindElement(By.XPath("//input[@type='submit' or @type='button']")) ??
        //        IWebElement submitButton = driver.FindElement(By.XPath("//button[@type='submit' or @type='button']")) ??
        //                                  driver.FindElement(By.TagName("button"));

        //        //usernameField.SendKeys(_username);
        //        //if (usernameField.Enabled)
        //        //{
        //        //    //wait.Until(d => usernameField.Displayed && usernameField.Enabled);
        //        //    usernameField.Click();
        //        //    usernameField.Clear();
        //        //    usernameField.SendKeys(_username);
        //        //}
        //        //else
        //        //{
        //        //    // Try clicking first to bring focus
        //        //    usernameField.Click();
        //        //    usernameField.SendKeys(_username);
        //        //}
        //        // passwordField.SendKeys(_password);
        //        //if (passwordField.Enabled)
        //        //{
        //        //    passwordField.SendKeys(_username);
        //        //}
        //        //else
        //        //{
        //        //    // Try clicking first to bring focus
        //        //    passwordField.Click();
        //        //    passwordField.SendKeys(_password);
        //        //}

        //        wait.Until(d => usernameField.Displayed && usernameField.Enabled);
        //        usernameField.Click();
        //        usernameField.Clear();
        //        usernameField.SendKeys(_username);

        //        // Wait for and interact with password
        //        wait.Until(d => passwordField.Displayed && passwordField.Enabled);
        //        passwordField.Click();
        //        passwordField.Clear();
        //        passwordField.SendKeys(_password);
        //        submitButton.Click();

        //        WaitForPageLoad(driver);

        //        // Verify login was successful by checking for logout button or absence of login fields
        //        return driver.FindElements(By.Id("username")).Count == 0 ||
        //               driver.FindElements(By.LinkText("Logout")).Count > 0 ||
        //               driver.FindElements(By.PartialLinkText("Sign Out")).Count > 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Login error: {ex.Message}");
        //        return false;
        //    }
        //}
        private static bool PerformLogin(IWebDriver driver)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                // First ensure page is loaded
                wait.Until(d => ((IJavaScriptExecutor)d)
                    .ExecuteScript("return document.readyState").Equals("complete"));

                // Try multiple common username field locators
                IWebElement usernameField = FindElementWithRetry(wait, new List<By>
        {
            By.Name("Email"),
            By.Id("Email"),
            By.Name("Username"),
            By.Id("Username"),
            By.Name("User"),
            By.CssSelector("input[type='email']"),
            By.XPath("//input[contains(@name, 'mail') or contains(@id, 'mail')]")
        });

                // Try multiple common password field locators
                IWebElement passwordField = FindElementWithRetry(wait, new List<By>
        {
            By.Id("Password"),
            By.Name("Password"),
            By.CssSelector("input[type='password']"),
            By.XPath("//input[@type='password']")
        });

                // Try multiple submit button locators
                IWebElement submitButton = FindElementWithRetry(wait, new List<By>
        {
            By.XPath("//button[@type='submit' or @type='button']"),
            By.CssSelector("button[type='submit'], input[type='submit']"),
            By.Id("LoginButton"),
            By.Name("LoginButton"),
            By.XPath("//*[contains(text(), 'Sign In') or contains(text(), 'Log In')]")
        });

                // Interact with elements
                usernameField.Click();
                Thread.Sleep(500);
                usernameField.Clear();
                usernameField.SendKeys(_username);

                passwordField.Click();
                passwordField.Clear();
                passwordField.SendKeys(_password);

                submitButton.Click();

                // Wait for login to complete
                WaitForPageLoad(driver);

                // Verify login success with multiple indicators
                return CheckLoginSuccess(driver);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                // Take screenshot for debugging
                //TakeScreenshot(driver, "login_failure");
                return false;
            }
        }

        // Helper method to try multiple locators
        private static IWebElement FindElementWithRetry(WebDriverWait wait, List<By> locators)
        {
            foreach (var locator in locators)
            {
                try
                {
                    return wait.Until(d =>
                    {
                        try
                        {
                            var element = d.FindElement(locator);
                            return (element.Displayed && element.Enabled) ? element : null;
                        }
                        catch (NoSuchElementException)
                        {
                            return null;
                        }
                    });
                }
                catch (WebDriverTimeoutException)
                {
                    continue; // Try next locator
                }
            }
            throw new NoSuchElementException($"None of the locators worked: {string.Join(", ", locators)}");
        }

        // Helper method to check login success
        private static bool CheckLoginSuccess(IWebDriver driver)
        {
            try
            {
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                return wait.Until(d =>
                {
                    return d.FindElements(By.Id("username")).Count == 0 ||
                           d.FindElements(By.LinkText("Logout")).Count > 0 ||
                           d.FindElements(By.PartialLinkText("Sign Out")).Count > 0 ||
                           d.FindElements(By.CssSelector(".user-profile")).Count > 0 ||
                           !d.Url.Contains("login");
                });
            }
            catch
            {
                return false;
            }
        }

        // Helper method to wait for page load
        private static void WaitForPageLoad(IWebDriver driver)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(d => ((IJavaScriptExecutor)d)
                .ExecuteScript("return document.readyState").Equals("complete"));
        }

        public static string TakeScreenshot(IWebDriver driver, string screenshotName)
        {
            try
            {
                // 1. Setup paths
                string solutionDirectory = GetSolutionDirectory();
                string screenshotDirectory = Path.Combine(solutionDirectory, "TestResults", "Screenshots");
                Directory.CreateDirectory(screenshotDirectory);

                // 2. Clean up screenshot name
                screenshotName = CleanFileName(screenshotName);

                // 3. Take screenshot
                ITakesScreenshot screenshotDriver = (ITakesScreenshot)driver;
                Screenshot screenshot = screenshotDriver.GetScreenshot();

                // 4. Save with timestamp and browser info
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string browserName = driver.GetType().Name;
                string screenshotPath = Path.Combine(
                    screenshotDirectory,
                    $"{timestamp}_{browserName}_{screenshotName}.png");

                //screenshot.SaveAsFile(screenshotPath, ScreenshotImageFormat.Png);
                screenshot.SaveAsFile(screenshotPath);

                // 5. Return path for reporting
                return screenshotPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to take screenshot: {ex.Message}");
                return string.Empty;
            }
        }

        private static string GetSolutionDirectory()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        private static string CleanFileName(string fileName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
        }
        private static void PerformLogout(IWebDriver driver)
        {
            try
            {
                var logoutButton = driver.FindElement(By.LinkText("Logout")) ??
                                   driver.FindElement(By.PartialLinkText("Sign Out")) ??
                                   driver.FindElement(By.XPath("//a[contains(@href,'logout')]"));

                if (logoutButton != null)
                {
                    logoutButton.Click();
                    WaitForPageLoad(driver);
                    Console.WriteLine("Logged out successfully.");
                }
            }
            catch
            {
                Console.WriteLine("Could not find logout button.");
            }
        }

        private static List<IWebElement> FindNavigationLinks(IWebDriver driver)
        {
            try
            {
                // Find all navigation links (menu items)
                var navElements = driver.FindElements(By.XPath(
                    "//nav//a | " +
                    "//*[contains(@class,'menu')]//a | " +
                    "//*[contains(@class,'navigation')]//a | " +
                    "//ul[contains(@class,'nav')]//a | " +
                    "//div[contains(@class,'navbar')]//a"));

                return navElements
                    .Where(link => !string.IsNullOrEmpty(link.GetAttribute("href")))
                    .Where(link => link.GetAttribute("href").StartsWith(_mainUrl)) // Only internal links
                    .DistinctBy(link => link.GetAttribute("href")) // Remove duplicates
                    .ToList();
            }
            catch
            {
                return new List<IWebElement>();
            }
        }

        private static void CollectPageData(IWebDriver driver)
        {
            try
            {
                string currentUrl = driver.Url;
                Console.WriteLine($"Collecting data from: {currentUrl}");

                var scrapedData = new List<ScrapedItem>();

                // Get all label texts
                var labels = driver.FindElements(By.TagName("label"));
                scrapedData.AddRange(labels.Select(l => new ScrapedItem
                {
                    ElementType = "Label",
                    Text = l.Text,
                    XPath = GetElementXPath(l),
                    PageUrl = currentUrl
                }));

                // Get all table headers
                var tableHeaders = driver.FindElements(By.XPath("//th"));
                scrapedData.AddRange(tableHeaders.Select(th => new ScrapedItem
                {
                    ElementType = "Table Header",
                    Text = th.Text,
                    XPath = GetElementXPath(th),
                    PageUrl = currentUrl
                }));

                // Get all heading elements (h1-h6)
                for (int i = 1; i <= 6; i++)
                {
                    var headers = driver.FindElements(By.TagName($"h{i}"));
                    scrapedData.AddRange(headers.Select(h => new ScrapedItem
                    {
                        ElementType = $"Heading H{i}",
                        Text = h.Text,
                        XPath = GetElementXPath(h),
                        PageUrl = currentUrl
                    }));
                }

                // Get all list items in sidebars
                var sidebarLists = driver.FindElements(By.XPath("//aside//li | //*[contains(@class,'sidebar')]//li"));
                scrapedData.AddRange(sidebarLists.Select(li => new ScrapedItem
                {
                    ElementType = "List Item",
                    Text = li.Text,
                    XPath = GetElementXPath(li),
                    PageUrl = currentUrl
                }));

                // Get paragraph texts
                var paragraphs = driver.FindElements(By.TagName("p"));
                scrapedData.AddRange(paragraphs.Where(p => !string.IsNullOrWhiteSpace(p.Text))
                    .Select(p => new ScrapedItem
                    {
                        ElementType = "Paragraph",
                        Text = p.Text,
                        XPath = GetElementXPath(p),
                        PageUrl = currentUrl
                    }));

                // Add to global collection
                _allScrapedData.AddRange(scrapedData);
                Console.WriteLine($"Collected {scrapedData.Count} items from this page.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error collecting page data: {ex.Message}");
            }
        }

        //private static void WaitForPageLoad(IWebDriver driver, int timeoutInSeconds = 10)
        //{
        //    try
        //    {
        //        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(timeoutInSeconds));
        //        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        //        Thread.Sleep(1000); // Additional small wait
        //    }
        //    catch
        //    {
        //        // Continue even if timeout occurs
        //    }
        //}

        private static string GetElementXPath(IWebElement element)
        {
            try
            {
                return ((IJavaScriptExecutor)((IWrapsDriver)element).WrappedDriver)
                    .ExecuteScript(
                        "function getPathTo(element) {" +
                        "    if (element === document.body) return element.tagName;" +
                        "    if (element.id !== '') return '//' + element.tagName + '[@id=\"' + element.id + '\"]';" +
                        "    if (element === document.documentElement) return element.tagName;" +
                        "" +
                        "    var ix = 0;" +
                        "    var siblings = element.parentNode.childNodes;" +
                        "    for (var i = 0; i < siblings.length; i++) {" +
                        "        var sibling = siblings[i];" +
                        "        if (sibling === element) return getPathTo(element.parentNode) + '/' + element.tagName + '[' + (ix + 1) + ']';" +
                        "        if (sibling.nodeType === 1 && sibling.tagName === element.tagName) ix++;" +
                        "    }" +
                        "}" +
                        "return getPathTo(arguments[0]);", element) as string;
            }
            catch
            {
                return "unknown";
            }
        }

        private static void ExportToExcel(List<ScrapedItem> data, string fileName)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Keywords");

                // Add headers
                worksheet.Cell(1, 1).Value = "Element Type";
                worksheet.Cell(1, 2).Value = "Text Content";
                worksheet.Cell(1, 3).Value = "XPath";
                worksheet.Cell(1, 4).Value = "Page URL";

                // Add data
                for (int i = 0; i < data.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = data[i].ElementType;
                    worksheet.Cell(i + 2, 2).Value = data[i].Text;
                    worksheet.Cell(i + 2, 3).Value = data[i].XPath;
                    worksheet.Cell(i + 2, 4).Value = data[i].PageUrl;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Save file
                workbook.SaveAs(fileName);
            }
        }

        private static void ExportToJson(List<ScrapedItem> data, string fileName)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(fileName, json);
        }
    }

    // Data model for scraped items
    public class ScrapedItem
    {
        public string? ElementType { get; set; }
        public string? Text { get; set; }
        public string? XPath { get; set; }
        public string? PageUrl { get; set; }
    }
}