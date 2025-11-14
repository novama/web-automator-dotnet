using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Serilog;

namespace WebAutomator.Automator.Selenium;

/// <summary>
///     Configuration options for SeleniumDriver
/// </summary>
public class SeleniumDriverOptions
{
    public string Browser { get; set; } = "chrome";
    public bool Headless { get; set; } = true;
    public (int Width, int Height) WindowSize { get; set; } = (1920, 1080);
    public int Timeout { get; set; } = 30000;
    public int ImplicitWait { get; set; } = 10000;
    public string? UserAgent { get; set; }
    public string DownloadsPath { get; set; } = "./downloads";
    public string OutputPath { get; set; } = "./output";
    public bool DisableImages { get; set; } = false;
    public bool DisableJavaScript { get; set; } = false;
    public bool AcceptInsecureCerts { get; set; } = true;
}

/// <summary>
///     Navigation result information
/// </summary>
public class SeleniumNavigationResult
{
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
}

/// <summary>
///     Base Selenium Driver - Core WebDriver wrapper with clean API
/// </summary>
public class SeleniumDriver : IDisposable
{
    private const string DefaultOutputDirectory = "./output";
    private const string DefaultDownloadsDirectory = "./downloads";
    private const string DefaultScreenshotsDirectory = "screenshots";
    private const string DefaultVideosDirectory = "videos";
    private readonly string _downloadDirectoryBasePath;

    private readonly ILogger _logger;
    private readonly SeleniumDriverOptions _options;
    private readonly string _outputDirectoryBasePath;
    private readonly string _screenshotsDirectory;
    private readonly string _videosDirectory;
    private string? _currentUrl;

    private IWebDriver? _driver;
    private bool _isStarted;

    public SeleniumDriver(ILogger logger, SeleniumDriverOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new SeleniumDriverOptions();

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
    public SeleniumDriver(ILogger logger, string browser, bool headless)
        : this(logger, new SeleniumDriverOptions { Browser = browser, Headless = headless })
    {
    }

    /// <summary>
    ///     Closes the browser and disposes resources
    /// </summary>
    public void Dispose()
    {
        if (_driver != null)
        {
            _logger.Information("Closing Selenium WebDriver");
            _driver.Quit();
            _driver.Dispose();
            _driver = null;
            _isStarted = false;
        }
    }

    /// <summary>
    ///     Start the browser driver
    /// </summary>
    public void Start()
    {
        if (_isStarted)
        {
            _logger.Warning("Driver already started");
            return;
        }

        _logger.Information($"Starting {_options.Browser} browser (headless: {_options.Headless})");

        try
        {
            _driver = _options.Browser.ToLower() switch
            {
                "chrome" => CreateChromeDriver(),
                "firefox" => CreateFirefoxDriver(),
                "edge" => CreateEdgeDriver(),
                _ => throw new ArgumentException($"Unsupported browser: {_options.Browser}")
            };

            // Set timeouts
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(_options.ImplicitWait);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMilliseconds(_options.Timeout);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMilliseconds(_options.Timeout);

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
    ///     Initializes the Selenium WebDriver (backward compatibility)
    /// </summary>
    [Obsolete("Use Start() instead")]
    public void Initialize()
    {
        Start();
    }

    private IWebDriver CreateChromeDriver()
    {
        var options = new ChromeOptions();

        if (_options.Headless)
            options.AddArgument("--headless=new");

        options.AddArguments(
            $"--window-size={_options.WindowSize.Width},{_options.WindowSize.Height}",
            "--no-sandbox",
            "--disable-dev-shm-usage",
            "--disable-gpu",
            "--disable-web-security",
            "--allow-running-insecure-content"
        );

        if (!string.IsNullOrWhiteSpace(_options.UserAgent))
            options.AddArgument($"--user-agent={_options.UserAgent}");

        if (_options.DisableImages)
            options.AddArgument("--blink-settings=imagesEnabled=false");

        if (_options.DisableJavaScript)
            options.AddArgument("--disable-javascript");

        // Configure download directory
        if (!string.IsNullOrEmpty(_options.DownloadsPath))
        {
            options.AddUserProfilePreference("download.default_directory", _downloadDirectoryBasePath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", true);

            Directory.CreateDirectory(_downloadDirectoryBasePath);
            _logger.Information($"Chrome download directory set to: {_downloadDirectoryBasePath}");
        }

        return new ChromeDriver(options);
    }

    private IWebDriver CreateFirefoxDriver()
    {
        var options = new FirefoxOptions();

        if (_options.Headless)
            options.AddArgument("--headless");

        options.AddArguments(
            $"--width={_options.WindowSize.Width}",
            $"--height={_options.WindowSize.Height}"
        );

        // Configure download directory
        if (!string.IsNullOrEmpty(_options.DownloadsPath))
        {
            options.SetPreference("browser.download.dir", _downloadDirectoryBasePath);
            options.SetPreference("browser.download.folderList", 2);
            options.SetPreference("browser.download.useDownloadDir", true);
            options.SetPreference("browser.helperApps.neverAsk.saveToDisk",
                "application/pdf,application/zip,text/csv,application/xml,application/octet-stream");

            Directory.CreateDirectory(_downloadDirectoryBasePath);
            _logger.Information($"Firefox download directory set to: {_downloadDirectoryBasePath}");
        }

        return new FirefoxDriver(options);
    }

    private IWebDriver CreateEdgeDriver()
    {
        var options = new EdgeOptions();

        if (_options.Headless)
            options.AddArgument("--headless");

        options.AddArguments(
            $"--window-size={_options.WindowSize.Width},{_options.WindowSize.Height}"
        );

        // Configure download directory
        if (!string.IsNullOrEmpty(_options.DownloadsPath))
        {
            options.AddUserProfilePreference("download.default_directory", _downloadDirectoryBasePath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", true);

            Directory.CreateDirectory(_downloadDirectoryBasePath);
            _logger.Information($"Edge download directory set to: {_downloadDirectoryBasePath}");
        }

        return new EdgeDriver(options);
    }

    /// <summary>
    ///     Stop and quit the browser driver
    /// </summary>
    public void Quit()
    {
        if (!_isStarted || _driver == null)
        {
            _logger.Warning("Driver not started or already quit");
            return;
        }

        try
        {
            _driver.Quit();
            _driver.Dispose();
            _driver = null;
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
    public SeleniumNavigationResult NavigateTo(string url)
    {
        EnsureStarted();

        _logger.Information($"Navigating to: {url}");

        try
        {
            _driver!.Navigate().GoToUrl(url);
            _currentUrl = url;
            _logger.Information("Navigation completed successfully");

            return new SeleniumNavigationResult
            {
                Url = _driver.Url,
                Title = _driver.Title,
                Success = true
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
    [Obsolete("Use NavigateTo() instead")]
    public async Task GoToAsync(string url)
    {
        await Task.Run(() => NavigateTo(url));
    }

    /// <summary>
    ///     Find a single element by selector
    /// </summary>
    public IWebElement FindElement(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var timeoutMs = timeout ?? _options.Timeout;
            var by = ParseSelector(selector);

            var wait = new WebDriverWait(_driver!, TimeSpan.FromMilliseconds(timeoutMs));
            return wait.Until(drv => drv.FindElement(by));
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
    public IReadOnlyCollection<IWebElement> FindElements(string selector)
    {
        EnsureStarted();

        try
        {
            var by = ParseSelector(selector);
            return _driver!.FindElements(by);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Elements not found: {selector}");
            return Array.Empty<IWebElement>();
        }
    }

    /// <summary>
    ///     Parse selector string to By object
    /// </summary>
    private By ParseSelector(string selector)
    {
        // Support different selector types
        if (selector.StartsWith("//") || selector.StartsWith("(//"))
            return By.XPath(selector);
        if (selector.StartsWith("#"))
            return By.Id(selector.Substring(1));
        if (selector.StartsWith("."))
            return By.ClassName(selector.Substring(1));
        if (selector.Contains("="))
        {
            var parts = selector.Split('=', 2);
            return By.CssSelector($"[{parts[0]}=\"{parts[1]}\"]");
        }

        return By.CssSelector(selector);
    }

    /// <summary>
    ///     Click an element
    /// </summary>
    public (bool Success, string Selector) Click(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var element = FindElement(selector, timeout);
            var wait = new WebDriverWait(_driver!, TimeSpan.FromMilliseconds(timeout ?? _options.Timeout));
            wait.Until(drv => element.Enabled);
            element.Click();
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
    public (bool Success, string Selector, string Text) SendKeys(string selector, string text, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var element = FindElement(selector, timeout);
            var wait = new WebDriverWait(_driver!, TimeSpan.FromMilliseconds(timeout ?? _options.Timeout));
            wait.Until(drv => element.Enabled);
            element.SendKeys(text);
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
    ///     Clear text from an element
    /// </summary>
    public (bool Success, string Selector) ClearText(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var element = FindElement(selector, timeout);
            element.Clear();
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
    public string GetText(string selector, int? timeout = null)
    {
        EnsureStarted();

        try
        {
            var element = FindElement(selector, timeout);
            return element.Text;
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
    [Obsolete("Use GetText() instead")]
    public string? GetElementText(string selector)
    {
        try
        {
            return GetText(selector);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Gets an attribute value from an element
    /// </summary>
    public string? GetElementAttribute(string selector, string attributeName)
    {
        try
        {
            var element = FindElement(selector);
            return element.GetAttribute(attributeName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Wait for an element to be present
    /// </summary>
    public (bool Success, string Selector) WaitForElement(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            FindElement(selector, timeoutMs);
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
    public (bool Success, string Selector) WaitForVisible(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            var element = FindElement(selector, timeoutMs);
            var wait = new WebDriverWait(_driver!, TimeSpan.FromMilliseconds(timeoutMs));
            wait.Until(drv => element.Displayed);
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
    public (bool Success, string Selector) WaitForClickable(string selector, int? timeout = null)
    {
        EnsureStarted();

        var timeoutMs = timeout ?? _options.Timeout;

        try
        {
            var element = FindElement(selector, timeoutMs);
            var wait = new WebDriverWait(_driver!, TimeSpan.FromMilliseconds(timeoutMs));
            wait.Until(drv => element.Enabled);
            return (true, selector);
        }
        catch
        {
            throw new Exception($"Element not clickable within {timeoutMs}ms: {selector}");
        }
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
    public string? TakeScreenshot(string? filename = null, bool includeTimestamp = true,
        string? baseDirectory = null, string? screenshotsDirectory = null)
    {
        EnsureStarted();

        try
        {
            var screenshot = ((ITakesScreenshot)_driver!).GetScreenshot();

            if (filename != null)
            {
                var actualBaseDirectory = baseDirectory ?? _outputDirectoryBasePath;
                var actualScreenshotsDirectory = screenshotsDirectory ?? _screenshotsDirectory;

                var paths = CreateScreenshotsDirectory(actualBaseDirectory, actualScreenshotsDirectory);
                var finalFilename = GenerateFilename(filename, includeTimestamp);

                var filepath = Path.Combine(paths.ScreenshotsPath, finalFilename);
                var ext = Path.GetExtension(filepath);
                var finalPath = string.IsNullOrEmpty(ext) ? $"{filepath}.png" : filepath;

                screenshot.SaveAsFile(finalPath);
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
    public (string ScreenshotsPath, string BaseDirectory) CreateScreenshotsDirectory(
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
    public (string VideosPath, string BaseDirectory) CreateVideosDirectory(
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
    ///     Start video recording (NOT YET IMPLEMENTED)
    /// </summary>
    public void StartVideoRecording(string filename = "recording.mp4", string? baseDirectory = null,
        string? videosDirectory = null, bool includeTimestamp = true)
    {
        var actualBaseDirectory = baseDirectory ?? _outputDirectoryBasePath;
        var actualVideosDirectory = videosDirectory ?? _videosDirectory;

        CreateVideosDirectory(actualBaseDirectory, actualVideosDirectory);

        throw new NotImplementedException(
            "Video recording not yet implemented. Use StartVideoRecording() when this feature is added.");
    }

    /// <summary>
    ///     Stop video recording (NOT YET IMPLEMENTED)
    /// </summary>
    public void StopVideoRecording()
    {
        throw new NotImplementedException(
            "Video recording not yet implemented. Use StopVideoRecording() when this feature is added.");
    }

    /// <summary>
    ///     Get page source HTML
    /// </summary>
    public string GetPageSource()
    {
        EnsureStarted();

        try
        {
            return _driver!.PageSource;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Get page source failed");
            throw new Exception($"Failed to get page source: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Get current page title
    /// </summary>
    public string GetTitle()
    {
        EnsureStarted();

        try
        {
            return _driver!.Title;
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
            _currentUrl = _driver!.Url;
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
    public void GoBack()
    {
        EnsureStarted();

        try
        {
            _driver!.Navigate().Back();
            _currentUrl = GetCurrentUrl();
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
    public void GoForward()
    {
        EnsureStarted();

        try
        {
            _driver!.Navigate().Forward();
            _currentUrl = GetCurrentUrl();
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
    public void Refresh()
    {
        EnsureStarted();

        try
        {
            _driver!.Navigate().Refresh();
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
    public T ExecuteScript<T>(string script, params object[] args)
    {
        EnsureStarted();

        try
        {
            var jsExecutor = (IJavaScriptExecutor)_driver!;
            var result = jsExecutor.ExecuteScript(script, args);
            return (T)result;
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
    public void Wait(int milliseconds)
    {
        Thread.Sleep(milliseconds);
    }

    /// <summary>
    ///     Waits for the specified number of milliseconds (backward compatibility)
    /// </summary>
    [Obsolete("Use Wait() instead")]
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
    public SeleniumDriverOptions GetOptions()
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
    ///     Get or create the download directory
    /// </summary>
    public string GetDownloadDirectory(string? downloadsPath = null, bool createDirectory = true)
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
    ///     Get the underlying WebDriver instance for advanced operations
    /// </summary>
    public IWebDriver GetWebDriver()
    {
        EnsureStarted();
        return _driver!;
    }

    /// <summary>
    ///     Private method to ensure driver is started
    /// </summary>
    private void EnsureStarted()
    {
        if (!_isStarted || _driver == null)
            throw new InvalidOperationException("Driver not started. Call Start() first.");
    }
}