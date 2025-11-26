using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using WebAutomatorCore.Automator.Playwright;
using WebAutomatorCore.Common.Utils;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace WebAutomatorLambda;

public class Handler
{
    public async Task<LambdaResponse> LambdaHandler(LambdaEvent lambdaEvent, ILambdaContext context)
    {
        var startTime = DateTime.UtcNow;
        var requestId = context.AwsRequestId;

        var logger = LoggerFactory.CreateLogger();
        logger.Information("LambdaHandler invoked. RequestId: {RequestId}", requestId);

        // Debug log for incoming event
        logger.Information("Received LambdaEvent: {@LambdaEvent}", lambdaEvent);

        var response = new LambdaResponse
        {
            StatusCode = 200,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "X-Request-ID", requestId }
            },
            Body = new LambdaResponseBody
            {
                Status = "success",
                RequestId = requestId,
                Timestamp = startTime.ToString("o"),
                Data = null,
                Error = null,
                ExecutionTime = 0
            }
        };

        PlaywrightDriver? driver = null; // Make driver nullable

        try
        {
            logger.Information($"Starting web automation handler (Request: {requestId})");

            // Load configuration
            // Use config packaged with the Lambda artifact
            var config = new ConfigManager("Config/lambda-config.json");

            // Get target URL and extraction config
            var targetUrl = lambdaEvent.Url ?? config.Get("automation:defaultUrl", "https://example.com");
            var extractConfig = lambdaEvent.Extract ?? new Dictionary<string, object>();

            logger.Information($"Target URL: {targetUrl}");
            logger.Information($"Extract configuration: {JsonSerializer.Serialize(extractConfig)}");

            // Initialize Playwright driver
            var driverOptions = new PlaywrightDriverOptions
            {
                Browser = config.Get("playwright:browser", "chromium"),
                Headless = true,
                WindowSize = (1920, 1080),
                Timeout = 25000,
                NavigationTimeout = 25000,
                UserAgent = config.Get("playwright:userAgent", string.Empty),
                DownloadsPath = "/tmp/automation-downloads",
                OutputPath = "/tmp/automation-output",
                DisableImages = false,
                DisableJavaScript = false
            };

            driver = new PlaywrightDriver(logger, driverOptions);

            logger.Information("Starting headless browser...");
            await driver.StartAsync();

            logger.Information($"Navigating to: {targetUrl}");
            var navigationResult = await driver.NavigateToAsync(targetUrl);

            if (!navigationResult.Success)
                throw new Exception($"Navigation failed with status: {navigationResult.Status}");

            logger.Information("Extracting data from page...");
            var extractedData = await ExtractPageDataAsync(driver, extractConfig, targetUrl);

            response.Body.Data = new
            {
                Url = targetUrl,
                PageTitle = navigationResult.Title,
                CurrentUrl = navigationResult.Url,
                ExtractedData = extractedData,
                PageMetadata = new
                {
                    LoadTime = (DateTime.UtcNow - startTime).TotalMilliseconds + "ms",
                    driverOptions.UserAgent
                }
            };

            logger.Information("Web automation completed successfully");
        }
        catch (Exception ex)
        {
            logger.Error($"Web automation failed: {ex.Message}");
            logger.Error($"Stack trace: {ex.StackTrace}");

            response.StatusCode = 500;
            response.Body.Status = "error";
            response.Body.Error = new
            {
                ex.Message,
                Type = ex.GetType().Name,
                Code = "AUTOMATION_ERROR"
            };
        }
        finally
        {
            // Add null checks for driver
            if (driver != null && driver.GetIsStarted())
                try
                {
                    logger.Information("Cleaning up browser resources...");
                    await driver.QuitAsync();
                }
                catch (Exception cleanupError)
                {
                    logger.Warning($"Cleanup warning: {cleanupError.Message}");
                }

            response.Body.ExecutionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            logger.Information($"Request completed in {response.Body.ExecutionTime}ms");
        }

        return response;
    }

    private async Task<object> ExtractPageDataAsync(PlaywrightDriver driver, Dictionary<string, object> extractConfig,
        string url)
    {
        var data = new Dictionary<string, object>();

        try
        {
            data["PageTitle"] = await driver.GetTitleAsync();
            data["CurrentUrl"] = driver.GetCurrentUrl();

            if (extractConfig.TryGetValue("heading", out var heading) && (bool)heading)
            {
                var selector = extractConfig.TryGetValue("headingSelector", out var headingSelector)
                    ? headingSelector.ToString()
                    : "h1";

                if (string.IsNullOrEmpty(selector))
                {
                    throw new ArgumentException("Selector cannot be null or empty.");
                }

                data["MainHeading"] = await driver.GetTextAsync(selector);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Data extraction failed: {ex.Message}", ex);
        }

        return data;
    }
}

public class LambdaEvent
{
    public string? Url { get; set; }
    public Dictionary<string, object>? Extract { get; set; }
}

public class LambdaResponse
{
    public int StatusCode { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public LambdaResponseBody Body { get; set; } = new();
}

public class LambdaResponseBody
{
    public string Status { get; set; } = "success";
    public string RequestId { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public object? Data { get; set; }
    public object? Error { get; set; }
    public double ExecutionTime { get; set; }
}
