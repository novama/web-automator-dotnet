# Web Automator .NET

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)
[![Playwright](https://img.shields.io/badge/Playwright-1.56-green.svg)](https://playwright.dev/dotnet/)
[![Selenium](https://img.shields.io/badge/Selenium-4.38-blue.svg)](https://www.selenium.dev/)

A modern web automation framework for .NET supporting both **Selenium** and **Playwright**. Perfect for web scraping, E2E testing, and browser automation with clean, maintainable code.

## Key Features

- **Dual Framework Support** - Choose between Selenium and Playwright
- **Unified API** - Consistent interface across both automation engines
- **Smart Configuration** - JSON-based configuration management
- **Comprehensive Logging** - Structured logging with Serilog
- **Cross-Platform** - Works on Windows, macOS, and Linux
- **Modern .NET 9** - Latest C# features and performance
- **Production Ready** - Comprehensive error handling and path resolution

## Quick Start

### Prerequisites

- **.NET 9.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Git** (for cloning the repository)
- **Chrome/Edge/Firefox** (for Selenium - installed automatically)
- **Playwright Browsers** (installed automatically on first run)

### Installation

```bash
# Clone the repository
git clone https://github.com/novama/web-automator-dotnet.git
cd web-automator-dotnet

# Restore dependencies
cd src
dotnet restore

# Install Playwright browsers (first time only)
pwsh bin/Debug/net9.0/playwright.ps1 install

# Build the project
dotnet build
```

### Basic Usage

```bash
# Run the application
cd src
dotnet run

# Select an option from the menu:
# 1. Selenium Simple Example
# 2. Playwright Simple Example
# 3. Run Both Examples
# 0. Exit
```

## Project Structure

```text
web-automator-dotnet/
├── src/
│   ├── Automator/
│   │   ├── Playwright/          # Playwright automation driver
│   │   │   └── PlaywrightDriver.cs
│   │   └── Selenium/            # Selenium automation driver
│   │       └── SeleniumDriver.cs
│   ├── Common/Utils/            # Shared utilities
│   │   ├── ConfigManager.cs     # JSON configuration handler
│   │   └── LoggerFactory.cs     # Serilog logger factory
│   ├── Examples/                # Example implementations
│   │   ├── SimplePlaywright.cs  # Playwright example
│   │   └── SimpleSelenium.cs    # Selenium example
│   ├── Config/                  # Configuration files
│   │   ├── simple-example-config.json
│   │   └── lambda-config.json
│   ├── Program.cs               # Application entry point
│   └── WebAutomator.csproj      # Project file
├── Logs/                        # Application logs
├── Screenshots/                 # Captured screenshots
├── LICENSE                      # MIT License
└── README.md                    # This file
```

## Framework Comparison

Both frameworks are fully supported with a unified API:

| Aspect | Selenium | Playwright |
|--------|----------|------------|
| **Speed** | Good | Excellent |
| **Reliability** | Good | Excellent |
| **Browser Support** | Chrome, Firefox, Edge | Chromium, Firefox, WebKit |
| **API Style** | Synchronous | Asynchronous |
| **Setup** | Auto (WebDriver) | Auto (pwsh script) |
| **Best For** | Compatibility | Performance |

### Playwright Advantages

- ✅ Faster execution and startup
- ✅ Built-in auto-waiting for elements
- ✅ Modern async/await API
- ✅ Better network interception
- ✅ WebKit support (Safari testing)

### Selenium Advantages

- ✅ Broader ecosystem and community
- ✅ More third-party integrations
- ✅ Synchronous API (simpler for some cases)
- ✅ Longer track record and stability

## Configuration

### JSON Configuration

Create or modify `src/Config/simple-example-config.json`:

```json
{
  "app": {
    "name": "Web Automator .NET",
    "version": "1.0.0",
    "debug": true
  },
  "browser": {
    "type": "chromium",
    "headless": false,
    "width": 1920,
    "height": 1080
  },
  "automation": {
    "timeout": 30000,
    "recordVideo": true,
    "screenshotOnError": true
  },
  "output": {
    "screenshots": "output/screenshots",
    "videos": "output/videos",
    "downloads": "downloads"
  }
}
```

### Configuration Options

#### Browser Configuration

- `browser:type` - Browser type: `"chromium"`, `"firefox"`, `"webkit"` (Playwright) or `"chrome"`, `"firefox"`, `"edge"` (Selenium)
- `browser:headless` - Run browser in headless mode (true/false)
- `browser:width` - Browser viewport width (pixels)
- `browser:height` - Browser viewport height (pixels)

#### Automation Settings

- `automation:timeout` - Default timeout for operations (milliseconds)
- `automation:recordVideo` - Enable video recording (Playwright only)
- `automation:screenshotOnError` - Auto-capture screenshots on errors

#### Output Directories

- `output:screenshots` - Screenshot output directory
- `output:videos` - Video recording directory
- `output:downloads` - File download directory

## API Reference

### PlaywrightDriver

```csharp
var options = new PlaywrightDriverOptions
{
    BrowserType = "chromium",
    Headless = false,
    Timeout = 30000,
    RecordVideo = true,
    ViewportWidth = 1920,
    ViewportHeight = 1080
};

var driver = new PlaywrightDriver(options, logger);
await driver.StartAsync();

// Navigation
var result = await driver.NavigateToAsync("https://example.com");

// Element interaction
var element = await driver.FindElementAsync("h1", timeout: 5000);
await driver.ClickAsync("button.submit");
await driver.SendKeysAsync("#input", "text");
var text = await driver.GetTextAsync("p.description");

// Waiting
await driver.WaitForElementAsync("div.content");
await driver.WaitForVisibleAsync("img.logo");
await driver.WaitForClickableAsync("a.link");

// Screenshots
await driver.TakeScreenshotAsync("screenshot-name");

// JavaScript execution
var result = await driver.ExecuteScriptAsync<string>("return document.title;");

// Navigation controls
await driver.GoBackAsync();
await driver.GoForwardAsync();
await driver.RefreshAsync();

// Cleanup
await driver.QuitAsync();
```

### SeleniumDriver

```csharp
var options = new SeleniumDriverOptions
{
    BrowserType = "chrome",
    Headless = false,
    Timeout = 30000,
    ViewportWidth = 1920,
    ViewportHeight = 1080,
    DownloadDirectory = "downloads"
};

var driver = new SeleniumDriver(options, logger);
driver.Start();

// Navigation
var result = driver.NavigateTo("https://example.com");

// Element interaction (similar API to Playwright)
var element = driver.FindElement("h1", timeout: 5000);
driver.Click("button.submit");
driver.SendKeys("#input", "text");
var text = driver.GetText("p.description");

// Waiting
driver.WaitForElement("div.content");
driver.WaitForVisible("img.logo");
driver.WaitForClickable("a.link");

// Screenshots
driver.TakeScreenshot("screenshot-name");

// JavaScript execution
var result = driver.ExecuteScript<string>("return document.title;");

// Navigation controls
driver.GoBack();
driver.GoForward();
driver.Refresh();

// Cleanup
driver.Quit();
```

## Examples

### Basic Web Scraping

```csharp
using WebAutomator.Automator.Playwright;
using WebAutomator.Common.Utils;

var logger = LoggerFactory.CreateLogger();
var options = new PlaywrightDriverOptions
{
    BrowserType = "chromium",
    Headless = true
};

var driver = new PlaywrightDriver(options, logger);
await driver.StartAsync();

await driver.NavigateToAsync("https://example.com");
var title = await driver.GetTextAsync("h1");
await driver.TakeScreenshotAsync("homepage");

logger.Information($"Page title: {title}");

await driver.QuitAsync();
```

### Form Automation

```csharp
await driver.NavigateToAsync("https://example.com/form");

// Fill form fields
await driver.SendKeysAsync("#name", "John Doe");
await driver.SendKeysAsync("#email", "john@example.com");
await driver.ClickAsync("#country option[value='US']");

// Submit form
await driver.ClickAsync("button[type='submit']");

// Wait for success message
await driver.WaitForVisibleAsync(".success-message");
var message = await driver.GetTextAsync(".success-message");
logger.Information($"Result: {message}");
```

### Multiple Pages Navigation

```csharp
await driver.NavigateToAsync("https://example.com/page1");
await driver.TakeScreenshotAsync("page1");

await driver.NavigateToAsync("https://example.com/page2");
await driver.TakeScreenshotAsync("page2");

// Go back to previous page
await driver.GoBackAsync();
var currentUrl = driver.GetPage().Url;
logger.Information($"Current URL: {currentUrl}");
```

## Logging

The application uses **Serilog** for structured logging with both console and file sinks:

- **Console Output**: Color-coded, real-time logging
- **File Output**: Persistent logs in `Logs/web-automator-YYYYMMDD.log`
- **Log Levels**: Information, Warning, Error

### Log Configuration

```csharp
var logger = LoggerFactory.CreateLogger();

logger.Information("Normal operation message");
logger.Warning("Warning message");
logger.Error("Error message with details");
```

## Output Management

### Screenshots

- Automatically organized in `output/screenshots/`
- Timestamp-based naming: `{name}_YYYY-MM-DD_HH-mm-ss.png`
- Configurable through options or config file

### Videos (Playwright Only)

- Saved to `output/videos/`
- WebM format (playable in modern browsers)
- Automatically recorded when `RecordVideo = true`

### Downloads

- Configurable download directory
- Automatic directory creation
- Path resolution using `Directory.GetCurrentDirectory()`

## Troubleshooting

### Common Issues

**Playwright browsers not found:**

```bash
cd src/bin/Debug/net9.0
pwsh playwright.ps1 install
```

**Selenium WebDriver not found:**

- WebDriver is managed automatically via NuGet
- Ensure Chrome/Firefox/Edge is installed
- Check browser version compatibility

**Configuration file not found:**

- Ensure `Config/` folder is in `src/` directory
- Check that `.csproj` has `CopyToOutputDirectory` set to `Always`
- Verify file path in example classes

**Screenshots not saving:**

- Check output directory permissions
- Verify path resolution using `Directory.GetCurrentDirectory()`
- Ensure parent directories are created

**NullReferenceException in Selenium:**

- Check that browser is started before operations
- Verify element selectors are correct
- Add explicit waits for dynamic content

## Performance

### Execution Metrics

| Operation | Playwright | Selenium |
|-----------|-----------|----------|
| **Cold Start** | ~2-3 sec | ~3-5 sec |
| **Page Load** | ~1-2 sec | ~2-3 sec |
| **Screenshot** | ~100-200ms | ~200-400ms |
| **Element Find** | ~50-100ms | ~100-200ms |

### Optimization Tips

1. **Use Headless Mode** for faster execution in production
2. **Enable Video Recording** only when needed (adds overhead)
3. **Reuse Driver Instance** across multiple operations
4. **Set Appropriate Timeouts** to avoid unnecessary waits
5. **Use Playwright** for better overall performance

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Playwright | 1.56.0 | Playwright automation framework |
| Selenium.WebDriver | 4.38.0 | Selenium automation framework |
| Serilog | 4.3.0 | Logging framework |
| Serilog.Sinks.Console | 6.1.1 | Console logging output |
| Serilog.Sinks.File | 7.0.0 | File logging output |
| Microsoft.Extensions.Configuration | 10.0.0 | Configuration management |
| Microsoft.Extensions.Configuration.Json | 10.0.0 | JSON configuration provider |

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **[Playwright Team](https://playwright.dev/dotnet/)** - Modern web automation framework for .NET
- **[Selenium Project](https://selenium.dev/)** - Web automation standard
- **[Serilog](https://serilog.net/)** - Flexible logging library for .NET
- **[.NET Team](https://dotnet.microsoft.com/)** - Modern development platform
