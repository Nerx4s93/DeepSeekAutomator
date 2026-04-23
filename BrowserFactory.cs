using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace DeepSeekAutomator
{
    public static class BrowserFactory
    {
        public static async Task<IWebDriver> OpenBrowserAsync(
            string profileName,
            string windowSize = "1280,720")
        {
            return await Task.Run(async () =>
            {
                var profileDir = Path.Combine(AppContext.BaseDirectory, "profiles", profileName);

                if (!Directory.Exists(profileDir))
                {
                    Directory.CreateDirectory(profileDir);
                }

                var options = new ChromeOptions();
                options.AddArgument($"--user-data-dir={profileDir}");
                options.AddArgument($"--window-size={windowSize}");

                var driver = new ChromeDriver(options);
                return driver;
            });
        }

        public static async Task<IWebDriver> CreateOptimizedBrowserAsync(
            string profileName = "deepseek1",
            string windowSize = "1280,720",
            bool headless = false)
        {
            return await Task.Run(async () =>
            {
                var profileDir = Path.Combine(AppContext.BaseDirectory, "profiles", profileName);

                if (!Directory.Exists(profileDir))
                {
                    Directory.CreateDirectory(profileDir);
                }

                var options = new ChromeOptions();

                if (headless)
                {
                    options.AddArgument("--headless=new");
                    options.AddArgument("--disable-gpu");
                }

                options.AddArgument($"--user-data-dir={profileDir}");
                options.AddArgument($"--window-size={windowSize}");

                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-software-rasterizer");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-plugins");
                options.AddArgument("--disable-default-apps");

                options.AddArguments(
                    "--disable-background-networking",
                    "--disable-background-timer-throttling",
                    "--disable-backgrounding-occluded-windows",
                    "--disable-renderer-backgrounding",
                    "--disable-hang-monitor",
                    "--disable-ipc-flooding-protection",
                    "--disable-sync",
                    "--disable-client-side-phishing-detection",
                    "--disable-logging",
                    "--disable-breakpad",
                    "--disable-crash-reporter"
                );

                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--ignore-ssl-errors");
                options.AddArgument("--disable-web-security");
                options.AddArgument("--aggressive-cache-discard");
                options.AddArgument("--disable-session-crashed-bubble");

                var prefs = new Dictionary<string, object>
                {
                    ["profile.managed_default_content_settings.images"] = 2,
                    ["profile.managed_default_content_settings.stylesheets"] = 2,
                    ["profile.default_content_setting_values.notifications"] = 2,
                    ["profile.managed_default_content_settings.javascript"] = 1,
                    ["profile.default_content_setting_values.cookies"] = 1,
                    ["profile.default_content_setting_values.popups"] = 2,
                    ["profile.default_content_setting_values.geolocation"] = 2,
                    ["profile.default_content_setting_values.media_stream"] = 2,

                    ["autofill.profile_enabled"] = false,
                    ["credentials_enable_service"] = false,
                    ["profile.password_manager_enabled"] = false,
                    ["safebrowsing.enabled"] = false,
                    ["translate.enabled"] = false
                };
                options.AddUserProfilePreference("prefs", prefs);

                options.AddExcludedArguments("enable-automation", "enable-logging");
                options.AddAdditionalChromeOption("useAutomationExtension", false);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    options.AddArgument("--disable-features=WindowsSandbox");
                    options.AddArgument("--disable-windows10-custom-titlebar");
                }

                var service = ChromeDriverService.CreateDefaultService();
                service.HideCommandPromptWindow = true;

                var driver = new ChromeDriver(service, options);

                driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(30);

                var js = (IJavaScriptExecutor)driver;
                js.ExecuteScript("Object.defineProperty(navigator, 'webdriver', {get: () => undefined})");
                js.ExecuteScript("Object.defineProperty(navigator, 'plugins', {get: () => [1,2,3,4,5]})");
                js.ExecuteScript("Object.defineProperty(navigator, 'languages', {get: () => ['ru-RU', 'ru']})");

                return driver;
            });
        }
    }
}