using Microsoft.Playwright;
using Serilog;

namespace WebAutomator.Automator.Playwright;

/// <summary>
///     Configuration options for PlaywrightDriver
/// </summary>
public class PlaywrightDriverOptions
{
    public string Browser { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public (int Width, int Height) WindowSize { get; set; } = (1920, 1080);
    public int Timeout { get; set; } = 30000;
    public int NavigationTimeout { get; set; } = 30000;
    public string? UserAgent { get; set; }
    public string DownloadsPath { get; set; } = "./downloads";
    public string OutputPath { get; set; } = "./output";
    public bool DisableImages { get; set; } = false;
    public bool DisableJavaScript { get; set; } = false;
    public bool AcceptInsecureCerts { get; set; } = true;
    public bool RecordVideo { get; set; } = false;
    public int SlowMo { get; set; } = 0;
}

/// <summary>
///     Navigation result information
/// </summary>
public class NavigationResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Status { get; set; }
}

/// <summary>
///     Unified Playwright Driver - Modern browser automation with unified API
/// </summary>
public class PlaywrightDriver : IAsyncDisposable
{
    private const string DefaultOutputDirectory = "./output";
    private const string DefaultDownloadsDirectory = "./downloads";
    private const string DefaultScreenshotsDirectory = "screenshots";
    private const string DefaultVideosDirectory = "videos";
    private readonly string _downloadDirectoryBasePath;

    private readonly ILogger _logger;
    private readonly PlaywrightDriverOptions _options;
    private readonly string _outputDirectoryBasePath;
    private readonly string _screenshotsDirectory;
    private readonly string _videosDirectory;

    private IBrowser? _browser;
    private IBrowserContext? _context;
    private string? _currentUrl;
    private bool _isStarted;
    private IPage? _page;
    private IPlaywright? _playwright;

    public PlaywrightDriver(ILogger logger, PlaywrightDriverOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new PlaywrightDriverOptions();

        // Initialize directory paths
        // Use current directory as project root (where the application is executed from)
        var projectRoot = Directory.GetCurrentDirectory();
        _outputDirectoryBasePath = Path.IsPathRooted(_options.OutputPath)
            ? _options.OutputPath
            : Path.GetFullPath(Path.Combine(projectRoot, _options.OutputPath));
        _downloadDirectoryBasePath = Path.IsPathRooted(_options.DownloadsPath)
            ? _options.DownloadsPath
            : Path.GetFullPath(Path.Combine(projectRoot, _options.DownloadsPath));
        _screenshotsDirectory = DefaultScreenshotsDirectory;
        _videosDirectory = DefaultVideosDirectory;
    }

    /// <summary>
    ///     Alternative constructor for backward compatibility
    /// </summary>
    public PlaywrightDriver(ILogger logger, string browserType, bool headless)
        : this(logger, new PlaywrightDriverOptions { Browser = browserType, Headless = headless })
    {
    }

    /// <summary>
    ///     Closes the browser and disposes resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_page != null)
        {
            _logger.Information("Closing Playwright page");
            await _page.CloseAsync();
            _page = null;
        }

        if (_browser != null)
        {
            _logger.Information("Closing Playwright browser");
            await _browser.CloseAsync();
            _browser = null;
        }

        if (_playwright != null)
        {
            _logger.Information("Disposing Playwright");
            _playwright.Dispose();
            _playwright = null;
        }
    }

    /// <summary>
    ///     Start the browser and create context/page
    /// </summary>
    public async Task StartAsync()
    {
        if (_isStarted)
        {
            _logger.Warning("Driver already started");
            return;
        }

        _logger.Information($"Starting {_options.Browser} browser (headless: {_options.Headless})");

        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            // Normalize browser type
            var normalizedBrowser = _options.Browser.ToLower() switch
            {
                "chrome" => "chromium",
                "chromium" => "chromium",
                "firefox" => "firefox",
                "webkit" => "webkit",
                "safari" => "webkit",
                _ => throw new ArgumentException(
                    $"Unsupported browser: {_options.Browser}. Supported: chromium, firefox, webkit")
            };

            // Launch browser
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = _options.Headless,
                SlowMo = _options.SlowMo
            };

            _browser = normalizedBrowser switch
            {
                "chromium" => await _playwright.Chromium.LaunchAsync(launchOptions),
                "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
                "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
                _ => throw new ArgumentException($"Unsupported browser type: {normalizedBrowser}")
            };

            // Create browser context with options
            var contextOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = _options.WindowSize.Width,
                    Height = _options.WindowSize.Height
                },
                IgnoreHTTPSErrors = _options.AcceptInsecureCerts,
                AcceptDownloads = true
            };

            // Set user agent if provided
            if (!string.IsNullOrWhiteSpace(_options.UserAgent)) contextOptions.UserAgent = _options.UserAgent;

            // Configure video recording if enabled
            if (_options.RecordVideo)
            {
                var videoDirInfo = await CreateVideosDirectoryAsync();
                contextOptions.RecordVideoDir = videoDirInfo.VideosPath;
                contextOptions.RecordVideoSize = new RecordVideoSize
                {
                    Width = _options.WindowSize.Width,
                    Height = _options.WindowSize.Height
                };
            }

            _context = await _browser.NewContextAsync(contextOptions);

            // Set timeouts
            _context.SetDefaultTimeout(_options.Timeout);
            _context.SetDefaultNavigationTimeout(_options.NavigationTimeout);

            // Create page
            _page = await _context.NewPageAsync();

            // Configure page-level settings
            if (_options.DisableImages)
                await _page.RouteAsync("**/*", async route =>
                {
                    if (route.Request.ResourceType == "image")
                        await route.AbortAsync();
                    else
                        await route.ContinueAsync();
                });

            _isStarted = true;
            _logger.Information("Browser driver started successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start browser driver");
            throw new Exception($"Driver startup failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Initializes the Playwright browser and page (backward compatibility)
    /// </summary>
    [Obsolete("Use StartAsync() instead")]
    public Task InitializeAsync()
    {
        return StartAsync();
    }

    /// <summary>
    ///     Stop and close the browser
    /// </summary>
    public async Task QuitAsync()
    {
        if (!_isStarted)
        {
            _logger.Warning("Driver not started or already quit");
            return;
        }

        try
        {
            if (_page != null)
            {
                await _page.CloseAsync();
                _page = null;
            }

            if (_context != null)
            {
                await _context.CloseAsync();
                _context = null;
            }

            if (_browser != null)
            {
                await _browser.CloseAsync();
                _browser = null;
            }

            _isStarted = false;
            _currentUrl = null;
            _logger.Information("Browser driver quit successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error quitting driver");
            throw;
        }
    }

    /// <summary>
    ///     Navigate to a URL
    /// </summary>
    public async Task<NavigationResult> NavigateToAsync(string url)
    {
        EnsureStarted();

        _logger.Information($"Navigating to: {url}");

        try
        {
            var response = await _page!.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = _options.NavigationTimeout
            });

            _currentUrl = url;
            _logger.Information("Navigation completed successfully");

            return new NavigationResult
            {
                Url = _page.Url,
                Title = await _page.TitleAsync(),
                Success = response?.Ok ?? false,
                Status = response?.Status ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Navigation failed: {ex.Message}");
            throw new Exception($"Failed to navigate to {url}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Navigates to the specified URL (backward compatibility)
    /// </summary>
    [Obsolete("Use NavigateToAsync() instead")]
    public async Task GoToAsync(string url)
    {
        await NavigateToAsync(url);
    }

    /// <summary>
    ///     Find a single element by selector
    /// </summary>
    public async Task<IElementHandle?> FindElementAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            var element = await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs,
                State = WaitForSelectorState.Attached
            });

            if (element == null)
                throw new Exception($"Element not found: {selector}");

            return element;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Element not found: {selector}");
            throw new Exception($"Element not found: {selector}", ex);
        }
    }

    /// <summary>
    ///     Find multiple elements by selector
    /// </summary>
    public async Task<IReadOnlyList<ILocator>> FindElementsAsync(string selector)
    {
        EnsureStarted();

        try
        {
            return await _page!.Locator(selector).AllAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Elements not found: {selector}");
            return Array.Empty<ILocator>();
        }
    }

    /// <summary>
    ///     Click an element
    /// </summary>
    public async Task<(bool Success, string Selector)> ClickAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            await _page!.ClickAsync(selector, new PageClickOptions { Timeout = timeoutMs });
            _logger.Information($"Clicked element: {selector}");
            return (true, selector);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Click failed: {selector}");
            throw new Exception($"Failed to click {selector}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Send keys to an element
    /// </summary>
    public async Task<(bool Success, string Selector, string Text)> SendKeysAsync(string selector, string text,
        int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            await _page!.FillAsync(selector, text, new PageFillOptions { Timeout = timeoutMs });
            _logger.Information($"Sent keys to element: {selector}");
            return (true, selector, text);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Send keys failed: {selector}");
            throw new Exception($"Failed to send keys to {selector}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Types text into an input element (backward compatibility)
    /// </summary>
    [Obsolete("Use SendKeysAsync() instead")]
    public async Task TypeAsync(string selector, string text)
    {
        await SendKeysAsync(selector, text);
    }

    /// <summary>
    ///     Clear text from an element
    /// </summary>
    public async Task<(bool Success, string Selector)> ClearTextAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            await _page!.FillAsync(selector, "", new PageFillOptions { Timeout = timeoutMs });
            _logger.Information($"Cleared text from element: {selector}");
            return (true, selector);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Clear text failed: {selector}");
            throw new Exception($"Failed to clear text from {selector}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Get text content from an element
    /// </summary>
    public async Task<string> GetTextAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            var text = await _page!.TextContentAsync(selector, new PageTextContentOptions { Timeout = timeoutMs });
            return text ?? "";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Get text failed: {selector}");
            throw new Exception($"Failed to get text from {selector}: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Gets text content from an element (backward compatibility)
    /// </summary>
    [Obsolete("Use GetTextAsync() instead")]
    public async Task<string?> GetElementTextAsync(string selector)
    {
        try
        {
            return await GetTextAsync(selector);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets an attribute value from an element
    /// </summary>
    public async Task<string?> GetElementAttributeAsync(string selector, string attributeName)
    {
        EnsureStarted();

        try
        {
            var attribute = await _page!.GetAttributeAsync(selector, attributeName);
            return attribute;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, $"Failed to get attribute '{attributeName}' for: {selector}");
            return null;
        }
    }

    /// <summary>
    ///     Wait for an element to be present
    /// </summary>
    public async Task<(bool Success, string Selector)> WaitForElementAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs,
                State = WaitForSelectorState.Attached
            });
            return (true, selector);
        }
        catch
        {
            throw new Exception($"Element not found within {timeoutMs}ms: {selector}");
        }
    }

    /// <summary>
    ///     Wait for an element to be visible
    /// </summary>
    public async Task<(bool Success, string Selector)> WaitForVisibleAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs,
                State = WaitForSelectorState.Visible
            });
            return (true, selector);
        }
        catch
        {
            throw new Exception($"Element not visible within {timeoutMs}ms: {selector}");
        }
    }

    /// <summary>
    ///     Wait for an element to be clickable
    /// </summary>
    public async Task<(bool Success, string Selector)> WaitForClickableAsync(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            await _page!.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
            {
                Timeout = timeoutMs,
                State = WaitForSelectorState.Visible
            });

            var isEnabled = await _page.IsEnabledAsync(selector);
            if (!isEnabled)
                throw new Exception("Element is disabled");

            return (true, selector);
        }
        catch
        {
            throw new Exception($"Element not clickable within {timeoutMs}ms: {selector}");
        }
    }

    /// <summary>
    ///     Waits for an element to be visible (backward compatibility)
    /// </summary>
    [Obsolete("Use WaitForVisibleAsync() instead")]
    public async Task WaitForSelectorAsync(string selector, int timeout = 30000)
    {
        await WaitForVisibleAsync(selector, timeout);
    }

    /// <summary>
    ///     Generate filename with timestamp if configured
    /// </summary>
    private string GenerateFilename(string baseName, bool includeTimestamp = true)
    {
        if (!includeTimestamp) return baseName;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var extension = Path.GetExtension(baseName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(baseName);
        return $"{nameWithoutExt}_{timestamp}{extension}";
    }

    /// <summary>
    ///     Take a screenshot with configured output directory
    /// </summary>
    public async Task<string?> TakeScreenshotAsync(string? filename = null, bool includeTimestamp = true,
        string? baseDirectory = null, string? screenshotsDirectory = null)
    {
        EnsureStarted();

        try
        {
            if (filename != null)
            {
                var actualBaseDirectory = baseDirectory ?? _outputDirectoryBasePath;
                var actualScreenshotsDirectory = screenshotsDirectory ?? _screenshotsDirectory;

                var paths = await CreateScreenshotsDirectoryAsync(actualBaseDirectory, actualScreenshotsDirectory);
                var finalFilename = GenerateFilename(filename, includeTimestamp);

                var filepath = Path.Combine(paths.ScreenshotsPath, finalFilename);
                var ext = Path.GetExtension(filepath);
                var finalPath = string.IsNullOrEmpty(ext) ? $"{filepath}.png" : filepath;

                await _page!.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = finalPath,
                    FullPage = true
                });

                _logger.Information($"Screenshot saved: {finalPath}");
                return finalPath;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Screenshot failed");
            throw new Exception($"Failed to take screenshot: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Create the screenshots output directory
    /// </summary>
    public async Task<(string ScreenshotsPath, string BaseDirectory)> CreateScreenshotsDirectoryAsync(
        string? baseDirectory = null, string? screenshotsDirectory = null, bool createDirectories = true)
    {
        try
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var resolvedBaseDirectory = baseDirectory != null && Path.IsPathRooted(baseDirectory)
                ? baseDirectory
                : Path.GetFullPath(Path.Combine(projectRoot, baseDirectory ?? _outputDirectoryBasePath));
            var screenshotsPath = Path.Combine(resolvedBaseDirectory, screenshotsDirectory ?? _screenshotsDirectory);

            if (createDirectories)
            {
                Directory.CreateDirectory(screenshotsPath);
                _logger.Information($"Screenshots output directory created: {screenshotsPath}");
            }

            return (screenshotsPath, resolvedBaseDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Screenshots directory creation failed");
            throw new Exception($"Failed to create screenshots directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Create videos output directory
    /// </summary>
    public async Task<(string VideosPath, string BaseDirectory)> CreateVideosDirectoryAsync(
        string? baseDirectory = null, string? videosDirectory = null, bool createDirectories = true)
    {
        try
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var resolvedBaseDirectory = baseDirectory != null && Path.IsPathRooted(baseDirectory)
                ? baseDirectory
                : Path.GetFullPath(Path.Combine(projectRoot, baseDirectory ?? _outputDirectoryBasePath));
            var videosPath = Path.Combine(resolvedBaseDirectory, videosDirectory ?? _videosDirectory);

            if (createDirectories)
            {
                Directory.CreateDirectory(videosPath);
                _logger.Information($"Videos output directory created: {videosPath}");
            }

            return (videosPath, resolvedBaseDirectory);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Video directory creation failed");
            throw new Exception($"Failed to create video directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Get or create the download directory
    /// </summary>
    public async Task<string> GetDownloadDirectoryAsync(string? downloadsPath = null, bool createDirectory = true)
    {
        try
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var downloadPath = downloadsPath != null && Path.IsPathRooted(downloadsPath)
                ? downloadsPath
                : Path.GetFullPath(Path.Combine(projectRoot, downloadsPath ?? _downloadDirectoryBasePath));

            if (createDirectory)
            {
                Directory.CreateDirectory(downloadPath);
                _logger.Information($"Download directory ready: {downloadPath}");
            }

            return downloadPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Download directory creation failed");
            throw new Exception($"Failed to setup download directory: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Start video recording
    /// </summary>
    public Task StartVideoRecordingAsync(string filename = "recording.webm", string? baseDirectory = null,
        string? videosDirectory = null, bool includeTimestamp = true)
    {
        EnsureStarted();

        _logger.Warning(
            "Video recording should be enabled at browser start. Use RecordVideo: true in options and restart browser.");

        if (!_options.RecordVideo)
            throw new Exception(
                "Video recording not enabled. Set RecordVideo: true in options and restart browser.");

        _logger.Information("Video recording is active (managed by Playwright context)");
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Stop video recording and get video path
    /// </summary>
    public async Task<string> StopVideoRecordingAsync()
    {
        EnsureStarted();

        if (!_options.RecordVideo)
            throw new Exception("Video recording not enabled");

        try
        {
            var videoPath = await _page!.Video!.PathAsync();
            _logger.Information($"Video recording saved: {videoPath}");
            return videoPath;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to get video path");
            throw new Exception($"Failed to stop video recording: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Get page source HTML
    /// </summary>
    public async Task<string> GetPageSourceAsync()
    {
        EnsureStarted();

        try
        {
            return await _page!.ContentAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get page source failed");
            throw new Exception($"Failed to get page source: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Gets the current page title
    /// </summary>
    public async Task<string> GetTitleAsync()
    {
        EnsureStarted();

        try
        {
            var title = await _page!.TitleAsync();
            _logger.Information($"Page title: {title}");
            return title;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get title failed");
            throw new Exception($"Failed to get page title: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Get current URL
    /// </summary>
    public string GetCurrentUrl()
    {
        EnsureStarted();

        try
        {
            _currentUrl = _page!.Url;
            return _currentUrl;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get current URL failed");
            throw new Exception($"Failed to get current URL: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Navigate back in browser history
    /// </summary>
    public async Task GoBackAsync()
    {
        EnsureStarted();

        try
        {
            await _page!.GoBackAsync();
            _currentUrl = _page.Url;
            _logger.Information("Navigated back");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Go back failed");
            throw new Exception($"Failed to go back: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Navigate forward in browser history
    /// </summary>
    public async Task GoForwardAsync()
    {
        EnsureStarted();

        try
        {
            await _page!.GoForwardAsync();
            _currentUrl = _page.Url;
            _logger.Information("Navigated forward");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Go forward failed");
            throw new Exception($"Failed to go forward: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Refresh the current page
    /// </summary>
    public async Task RefreshAsync()
    {
        EnsureStarted();

        try
        {
            await _page!.ReloadAsync();
            _logger.Information("Page refreshed");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Refresh failed");
            throw new Exception($"Failed to refresh page: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Execute JavaScript in the browser
    /// </summary>
    public async Task<T> ExecuteScriptAsync<T>(string script, params object[] args)
    {
        EnsureStarted();

        try
        {
            return await _page!.EvaluateAsync<T>(script, args);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Execute script failed");
            throw new Exception($"Failed to execute script: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Wait for a specified amount of time
    /// </summary>
    public async Task WaitAsync(int milliseconds)
    {
        await Task.Delay(milliseconds);
    }

    /// <summary>
    ///     Check if driver is started
    /// </summary>
    public bool GetIsStarted()
    {
        return _isStarted;
    }

    /// <summary>
    ///     Get driver options
    /// </summary>
    public PlaywrightDriverOptions GetOptions()
    {
        return _options;
    }

    /// <summary>
    ///     Get current output directory configuration
    /// </summary>
    public (string BaseDirectory, string ScreenshotsDirectory, string VideosDirectory, string DownloadDirectory)
        GetOutputDirectoryConfig()
    {
        return (_outputDirectoryBasePath, _screenshotsDirectory, _videosDirectory, _downloadDirectoryBasePath);
    }

    /// <summary>
    ///     Get the underlying Playwright Page instance for advanced operations
    /// </summary>
    public IPage GetPage()
    {
        EnsureStarted();
        return _page!;
    }

    /// <summary>
    ///     Get the underlying Playwright Browser Context for advanced operations
    /// </summary>
    public IBrowserContext GetContext()
    {
        EnsureStarted();
        return _context!;
    }

    /// <summary>
    ///     Get the underlying Playwright Browser instance for advanced operations
    /// </summary>
    public IBrowser GetBrowser()
    {
        EnsureStarted();
        return _browser!;
    }

    /// <summary>
    ///     Private method to ensure driver is started
    /// </summary>
    private void EnsureStarted()
    {
        if (!_isStarted || _page == null)
            throw new InvalidOperationException("Driver not started. Call StartAsync() first.");
    }
}