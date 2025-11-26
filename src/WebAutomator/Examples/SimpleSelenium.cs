using WebAutomatorCore.Automator.Selenium;
using WebAutomatorCore.Common.Utils;

namespace WebAutomator.Examples;

/// <summary>
///     Simple Selenium web automation example
/// </summary>
public class SimpleSelenium
{
    // Define configuration file path
    private const string ConfigFilePath = "Config/simple-example-config.json";
    private const string TargetUrl = "https://example.com";

    public static async Task Run()
    {
        // Initialize logger
        var logger = LoggerFactory.CreateLogger();

        logger.Information("🚀 Starting simple Selenium web automation example...");

        // Load configuration
        logger.Information("Loading configuration from JSON file...");
        var config = new ConfigManager(ConfigFilePath);

        // Get app configuration
        var appName = config.Get("app:name", "Web Automator");
        var debug = config.Get("app:debug", false);
        var appVersion = config.Get("app:version", "1.0.0");
        logger.Information($"App: {appName} v{appVersion}");
        logger.Information($"Debug: {(debug ? "Enabled" : "Disabled")}");

        // Get selenium configuration
        var browser = config.Get("selenium:browser", "chrome");
        var headless = config.Get("selenium:headless", false);
        var windowSizeStr = config.Get("selenium:windowSize", "1920,1080");
        var windowSizeParts = windowSizeStr.Split(',');
        var windowSizeWidth = int.Parse(windowSizeParts[0]);
        var windowSizeHeight = int.Parse(windowSizeParts[1]);
        var timeout = config.Get("selenium:pageLoadTimeout", 30000);
        var implicitWait = config.Get("selenium:implicitWait", 10000);

        logger.Information($"Browser: {browser}");
        logger.Information($"Headless: {(headless ? "Yes" : "No")}");
        logger.Information($"Window Size: {windowSizeWidth}x{windowSizeHeight}");
        logger.Information($"Target URL: {TargetUrl}");

        // Get output configuration
        var outputBaseFolder = config.Get("output:baseFolder", "./output");
        var downloadsFolder = config.Get("output:downloads:folder", "./downloads");

        // Create Selenium driver with options
        logger.Information("\n🌐 Creating web automation driver...");

        var driverOptions = new SeleniumDriverOptions
        {
            Browser = browser,
            Headless = headless,
            WindowSize = (windowSizeWidth, windowSizeHeight),
            Timeout = timeout,
            ImplicitWait = implicitWait,
            OutputPath = outputBaseFolder,
            DownloadsPath = downloadsFolder,
            AcceptInsecureCerts = true
        };

        using var driver = new SeleniumDriver(logger, driverOptions);

        logger.Information($"Using Selenium with {driverOptions.Browser} browser");

        try
        {
            // Start the browser
            logger.Information("Starting browser...");
            driver.Start();

            // Navigate to a simple webpage
            logger.Information($"Navigating to {TargetUrl}...");
            var navResult = driver.NavigateTo(TargetUrl);

            // Get page information
            var pageTitle = driver.GetTitle();
            var currentUrl = driver.GetCurrentUrl();

            logger.Information($"Page Title: {pageTitle}");
            logger.Information($"Current URL: {currentUrl}");

            // Find and get text from the main heading
            logger.Information("Finding page elements...");
            try
            {
                var headingText = driver.GetText("h1");
                logger.Information($"Main Heading: \"{headingText}\"");
            }
            catch (Exception elementError)
            {
                logger.Warning($"Could not find or read h1 element: {elementError.Message}");
                logger.Information("Continuing without heading text...");
            }

            // Take a screenshot using configured output directories
            logger.Information("Taking screenshot...");
            try
            {
                var includeTimestamp = config.Get("output:screenshots:includeTimestamp", true);
                var screenshotPath = driver.TakeScreenshot("example-page.png", includeTimestamp);

                if (screenshotPath != null)
                    logger.Information($"Screenshot saved to organized folder: {Path.GetFileName(screenshotPath)}");
            }
            catch (Exception screenshotError)
            {
                logger.Warning($"Screenshot failed: {screenshotError.Message}");
            }

            // Get output directory paths for reference
            try
            {
                var paths = driver.GetOutputDirectoryConfig();
                logger.Information($"Output directories available at: {paths.BaseDirectory}");
                logger.Information($"Download directory: {paths.DownloadDirectory}");
                logger.Information($"Screenshots directory: {paths.ScreenshotsDirectory}");
                logger.Information($"Videos directory: {paths.VideosDirectory}");
            }
            catch (Exception pathError)
            {
                logger.Warning($"Could not get output paths: {pathError.Message}");
            }

            // Demonstrate some additional interactions
            logger.Information("Performing additional interactions...");

            // Scroll down and up for visual effect
            driver.ExecuteScript<object>("window.scrollTo(0, document.body.scrollHeight);");
            driver.Wait(1000);
            driver.ExecuteScript<object>("window.scrollTo(0, 0);");
            driver.Wait(1000);

            // Get and display some page information
            var pageSource = driver.GetPageSource();
            logger.Information($"� Page has {pageSource.Length} characters of HTML content");

            // Wait a moment to see the browser (if not headless)
            if (!headless)
            {
                logger.Information("Waiting 3 seconds (browser visible)...");
                await Task.Run(() => driver.Wait(3000));
            }

            logger.Information("✅ Web automation example completed successfully!");
        }
        catch (Exception error)
        {
            logger.Error($"❌ Web automation failed: {error.Message}");
            if (error.StackTrace != null) logger.Error($"Stack trace: {error.StackTrace}");
            throw;
        }
        finally
        {
            // Always close the browser
            if (driver.GetIsStarted())
            {
                logger.Information("Closing browser...");
                driver.Quit();
            }

            LoggerFactory.CloseLogger();
        }
    }
}