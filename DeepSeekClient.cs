using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DeepSeekAutomator
{
    public class DeepSeekClient : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        private DeepSeekClient(IWebDriver driver, WebDriverWait wait)
        {
            _driver = driver;
            _wait = wait;
        }

        public static async Task<DeepSeekClient> CreateAsync(string profile, bool headless = true)
        {
            var driver = await BrowserFactory.CreateBrowserAsync(profile, headless: headless);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));

            await Task.Run(() =>
            {
                driver.Navigate().GoToUrl("https://chat.deepseek.com/");
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(
                    "textarea[placeholder*='Message'], textarea[placeholder*='Сообщение']")));
            });

            return new DeepSeekClient(driver, wait);
        }
        public void EnableNormalMode() => ClickModelType("default", "Обычный режим");

        public void EnableExpertMode() => ClickModelType("expert", "Режим Эксперт");

        private void ClickModelType(string type, string label)
        {
            try
            {
                _driver.FindElement(By.XPath($"//div[@data-model-type='{type}']")).Click();
            }
            catch { Console.WriteLine($"[Deepseek][Error] {label} не найден\n"); }
        }

        public void EnableDeepThinking() => SetToggleButton("Глубокое мышление", "DeepThink", true);
        public void DisableDeepThinking() => SetToggleButton("Глубокое мышление", "DeepThink", false);

        public void EnableOnlineSearch() => SetToggleButton("Умный поиск", "Search", true);
        public void DisableOnlineSearch() => SetToggleButton("Умный поиск", "Search", false);

        private void SetToggleButton(string ruText, string enText, bool enable)
        {
            try
            {
                var xpath = $"//div[contains(@class, 'ds-atom-button')][contains(., '{ruText}') or contains(., '{enText}')]";
                var button = _driver.FindElement(By.XPath(xpath));
                bool isSelected = button.GetAttribute("class")!.Contains("ds-toggle-button--selected");

                if (enable && !isSelected || !enable && isSelected)
                {
                    button.Click();
                }
            }
            catch { Console.WriteLine($"[Deepseek][Error] Кнопка '{ruText}' не найдена\n"); }
        }

        public void SendMessage(string message)
        {
            try
            {
                var textarea = _driver.FindElement(By.CssSelector(
                    "textarea[name='search'], textarea[placeholder*='Message'], textarea[placeholder*='Сообщение']"));

                var js = (IJavaScriptExecutor)_driver;
                js.ExecuteScript("arguments[0].value = '';", textarea);

                js.ExecuteScript("arguments[0].value = arguments[1];", textarea, message);

                js.ExecuteScript("arguments[0].dispatchEvent(new Event('input', { bubbles: true }));", textarea);

                textarea.SendKeys(Keys.Enter);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Deepseek][Error] Ошибка при отправке: {ex.Message}\n");
            }
        }

        public void NewChat()
        {
            try
            {
                var button = _driver.FindElement(By.XPath("//span[text()='Новый чат' or text()='New chat']"));
                button.Click();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[Deepseek][Error] Ошибка: {e.Message}\n");
            }
        }

        public void DisposeWithoutSaving()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("taskkill", "/F /IM chrome.exe") { CreateNoWindow = true });
            }
            Environment.Exit(0);
        }

        public string GetLastMessageText()
        {
            try
            {
                var messages = _driver.FindElements(By.CssSelector(".ds-message"));

                if (messages.Count > 0)
                {
                    return messages[^1].Text;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool IsGenerating
        {
            get
            {
                try
                {
                    var sendButton = _driver.FindElement(By.CssSelector("div._52c986b.ds-icon-button"));
                    var svgPath = sendButton.FindElement(By.CssSelector("svg path"));
                    return !svgPath.GetAttribute("d")!.Contains("M8.3125 0.981587");
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}