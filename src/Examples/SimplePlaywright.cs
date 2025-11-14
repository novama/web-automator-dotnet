using WebAutomator.Automator.Playwright;
using WebAutomator.Common.Utils;

namespace WebAutomator.Examples;

/// <summary>
///     Simple Playwright web automation example
/// </summary>
public class SimplePlaywright
{
    // Define configuration file path  
    private const string ConfigFilePath = "Config/simple-example-config.json";
    private const string TargetUrl = "https://example.com";

    public static async Task Run()
    {
        // Initialize logger
        var logger = LoggerFactory.CreateLogger();

        logger.Information("🚀 Starting simple Playwright web automation example...");

        // Load configuration
        logger.Information("Loading configuration from JSON file...");
        var config = new ConfigManager(ConfigFilePath);

        // Get app configuration
        var appName = config.Get("app:name", "Web Automator");
        var debug = config.Get("app:debug", false);
        var appVersion = config.Get("app:version", "1.0.0");
        logger.Information($"App: {appName} v{appVersion}");
        logger.Information($"Debug: {(debug ? "Enabled" : "Disabled")}");

        // Get playwright configuration
        var browser = config.Get("playwright:browser", "chromium");
        var headless = config.Get("playwright:headless", false);
        var windowSizeWidth = config.Get("playwright:windowSize:width", 1920);
        var windowSizeHeight = config.Get("playwright:windowSize:height", 1080);
        var timeout = config.Get("playwright:timeout", 30000);
        var navigationTimeout = config.Get("playwright:navigationTimeout", 30000);
        var videoRecording = config.Get("playwright:recordVideo", false);
        var acceptInsecureCerts = config.Get("playwright:acceptInsecureCerts", true);
        var slowMo = config.Get("playwright:slowMo", 0);
        var disableImages = config.Get("playwright:disableImages", false);
        var disableJavaScript = config.Get("playwright:disableJavaScript", false);

        logger.Information($"Browser: {browser}");
        logger.Information($"Headless: {(headless ? "Yes" : "No")}");
        logger.Information($"Window Size: {windowSizeWidth}x{windowSizeHeight}");
        logger.Information($"Video Recording: {(videoRecording ? "Enabled" : "Disabled")}");
        logger.Information($"Target URL: {TargetUrl}");

        // Get output configuration
        var outputBaseFolder = config.Get("output:baseFolder", "./output");
        var downloadsFolder = config.Get("output:downloads:folder", "./downloads");

        // Create Playwright driver with options
        logger.Information("\n🌐 Creating web automation driver...");

        var driverOptions = new PlaywrightDriverOptions
        {
            Browser = browser,
            Headless = headless,
            WindowSize = (windowSizeWidth, windowSizeHeight),
            Timeout = timeout,
            NavigationTimeout = navigationTimeout,
            OutputPath = outputBaseFolder,
            DownloadsPath = downloadsFolder,
            AcceptInsecureCerts = acceptInsecureCerts,
            SlowMo = slowMo,
            RecordVideo = videoRecording,
            DisableImages = disableImages,
            DisableJavaScript = disableJavaScript
        };

        await using var driver = new PlaywrightDriver(logger, driverOptions);

        logger.Information($"Using Playwright with {driverOptions.Browser} engine");

        try
        {
            // Start the browser
            logger.Information("Starting browser...");
            await driver.StartAsync();

            // Check if video recording is enabled
            if (driverOptions.RecordVideo)
                logger.Information("📹 Video recording is enabled - session will be recorded");

            // Navigate to a simple webpage
            logger.Information($"Navigating to {TargetUrl}...");
            var navResult = await driver.NavigateToAsync(TargetUrl);

            // Get page information
            var pageTitle = await driver.GetTitleAsync();
            var currentUrl = driver.GetCurrentUrl();

            logger.Information($"Page Title: {pageTitle}");
            logger.Information($"Current URL: {currentUrl}");

            // Find and get text from the main heading
            logger.Information("Finding page elements...");
            try
            {
                var headingText = await driver.GetTextAsync("h1");
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
                var screenshotPath = await driver.TakeScreenshotAsync("example-page.png", includeTimestamp);

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
                logger.Information($"Videos directory: {paths.VideosDirectory} (ready for future video recording)");
            }
            catch (Exception pathError)
            {
                logger.Warning($"Could not get output paths: {pathError.Message}");
            }

            // Demonstrate some additional interactions for video recording
            if (driverOptions.RecordVideo)
            {
                logger.Information("📹 Recording additional interactions for video...");

                // Scroll down and up for visual effect
                await driver.ExecuteScriptAsync<object>("window.scrollTo(0, document.body.scrollHeight);");
                await driver.WaitAsync(1000);
                await driver.ExecuteScriptAsync<object>("window.scrollTo(0, 0);");
                await driver.WaitAsync(1000);

                // Get and display some page information
                var pageSource = await driver.GetPageSourceAsync();
                logger.Information($"� Page has {pageSource.Length} characters of HTML content");
            }

            // Wait a moment to see the browser (if not headless)
            if (!headless)
            {
                logger.Information("Waiting 3 seconds (browser visible)...");
                await driver.WaitAsync(3000);
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
                await driver.QuitAsync();

                // Video recording info
                if (driverOptions.RecordVideo)
                {
                    logger.Information("📹 Video recording completed");
                    try
                    {
                        var videosPath = await driver.CreateVideosDirectoryAsync();
                        logger.Information($"📁 Video files should be available in: {videosPath.VideosPath}");
                        logger.Information("🎬 Video files are automatically saved when the browser context closes");
                        logger.Information("📹 Video format: WebM (can be played with most modern video players)");
                    }
                    catch (Exception videoError)
                    {
                        logger.Warning($"Could not get video directory info: {videoError.Message}");
                    }
                }
            }

            LoggerFactory.CloseLogger();
        }
    }
}