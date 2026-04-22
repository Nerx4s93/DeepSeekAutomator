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

        public async Task EnableNormalMode() => await ClickModelTypeAsync("default", "Обычный режим");

        public async Task EnableExpertMode() => await ClickModelTypeAsync("expert", "Режим Эксперт");

        private async Task ClickModelTypeAsync(string type, string label)
        {
            await Task.Run(() =>
            {
                try
                {
                    _driver.FindElement(By.XPath($"//div[@data-model-type='{type}']")).Click();
                }
                catch
                {
                    Console.WriteLine($"[Deepseek][Error] {label} не найден");
                }
            });
        }

        public async Task EnableDeepThinking() => await SetToggleButtonAsync("Глубокое мышление", "DeepThink", true);
        public async Task DisableDeepThinking() => await SetToggleButtonAsync("Глубокое мышление", "DeepThink", false);

        public async Task EnableOnlineSearch() => await SetToggleButtonAsync("Умный поиск", "Search", true);
        public async Task DisableOnlineSearch() => await SetToggleButtonAsync("Умный поиск", "Search", false);

        private async Task SetToggleButtonAsync(string ruText, string enText, bool enable)
        {
            await Task.Run(() =>
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
                catch
                {
                    Console.WriteLine($"[Deepseek][Error] Кнопка '{ruText}' не найдена");
                }
            });
        }

        public async Task SendMessageAsync(string message)
        {
            await Task.Run(() =>
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
                    Console.WriteLine($"[Deepseek][Error] Ошибка при отправке: {ex.Message}");
                }
            });
        }

        public async Task NewChatAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var button = _driver.FindElement(By.XPath("//span[text()='Новый чат' or text()='New chat']"));
                    button.Click();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Deepseek][Error] Ошибка: {ex.Message}");
                }
            });
        }

        public async Task<string> GetLastMessageTextAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var messages = _driver.FindElements(By.CssSelector(".ds-message"));
                    return messages.Count > 0 ? messages[^1].Text : string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            });
        }

        private async Task<bool> GetIsGeneratingAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var sendButton = _driver.FindElement(By.CssSelector("div._52c986b.ds-icon-button"));
                    var svgPath = sendButton.FindElement(By.CssSelector("svg path"));
                    return !svgPath.GetAttribute("d")!.Contains("M8.3125 0.981587");
                }
                catch { return false; }
            });
        }

        public async Task<string> WaitAndGetResultAsync(int pollDelayMs = 500)
        {
            await Task.Delay(1000);

            while (await GetIsGeneratingAsync())
            {
                await Task.Delay(pollDelayMs);
            }

            return await GetLastMessageTextAsync();
        }

        public void DisposeWithoutSaving()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("taskkill", "/F /IM chrome.exe") { CreateNoWindow = true });
            }
            Environment.Exit(0);
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}