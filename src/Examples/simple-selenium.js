/**
 * Super Simple Example - Logger, Configuration & Web Automation
 * 
 * Basic demonstration of importing and using:
 * - Logger (with automatic fallback)
 * - ConfigManager (loading from JSON file)
 * - Selenium Web Automation (with configuration-based setup)
 */

const path = require('path');

// Import the logger and config manager
const logger = require('../common/utils/logger');
const ConfigManager = require('../common/utils/configManager');

// Import selenium drivers
const ChromeSeleniumDriver = require('../automator/selenium/drivers/chromeSeleniumDriver');
const FirefoxSeleniumDriver = require('../automator/selenium/drivers/firefoxSeleniumDriver');
const EdgeSeleniumDriver = require('../automator/selenium/drivers/edgeSeleniumDriver');

// Define configuration file path
const CONFIG_FILE_PATH = '../../config/simple-example-config.json';
const TARGET_URL = 'https://example.com';

async function simpleWebAutomationExample() {
    console.log('🚀 Starting simple Selenium web automation example...\n');

    // Load configuration
    logger.info('Loading configuration from JSON file...');
    const config = new ConfigManager(CONFIG_FILE_PATH);

    // Get app configuration
    const app = config.get('app', {});
    logger.info(`App: ${app.name} v${app.version}`);
    logger.info(`Debug: ${app.debug ? 'Enabled' : 'Disabled'}`);

    // Get selenium configuration
    const selenium = config.get('selenium', {});
    logger.info(`Browser: ${selenium.browser}`);
    logger.info(`Headless: ${selenium.headless ? 'Yes' : 'No'}`);
    logger.info(`Window Size: ${selenium.windowSize}`);
    logger.info(`Target URL: ${playwright.targetUrl || 'Not specified'}`);

    // Create selenium driver based on configuration
    logger.info('\n🌐 Creating web automation driver...');
    
    let driver;
    const driverOptions = {
        headless: selenium.headless || false,
        windowSize: {
            width: 1920,
            height: 1080
        },
        timeout: selenium.pageLoadTimeout || 30000,
        implicitWait: selenium.implicitWait || 10000
    };

    // Select driver based on browser configuration
    switch (selenium.browser?.toLowerCase()) {
        case 'firefox':
            driver = new FirefoxSeleniumDriver(driverOptions);
            break;
        case 'edge':
            driver = new EdgeSeleniumDriver(driverOptions);
            break;
        case 'chrome':
        default:
            driver = new ChromeSeleniumDriver(driverOptions);
            break;
    }

    try {
        // Start the browser
        logger.info('Starting browser...');
        await driver.start();

        // Navigate to a simple webpage
        logger.info(`Navigating to ${TARGET_URL}...`);
        await driver.navigateTo(TARGET_URL);

        // Get page information
        const pageTitle = await driver.getTitle();
        const currentUrl = await driver.getCurrentUrl();
        
        logger.info(`Page Title: ${pageTitle}`);
        logger.info(`Current URL: ${currentUrl}`);

        // Find and get text from the main heading
        logger.info('Finding page elements...');
        try {
            // Option 1: Use getText directly with selector (finds element internally)
            const headingText = await driver.getText('h1');
            logger.info(`Main Heading: "${headingText}"`);
        } catch (elementError) {
            logger.warn(`Could not find or read h1 element: ${elementError.message}`);
            logger.info('Continuing without heading text...');
        }

        // Take a screenshot using configured output directories
        logger.info('Taking screenshot...');
        try {
            const outputConfig = config.get('output', {});
            const baseFolder = outputConfig.baseFolder || './output';
            const screenshotsFolder = outputConfig.screenshots?.folder || 'screenshots';
            const includeTimestamp = outputConfig.screenshots?.includeTimestamp !== false;
            
            const screenshotPath = await driver.takeScreenshot('example-page.png', includeTimestamp, baseFolder, screenshotsFolder, );
            logger.info(`Screenshot saved to organized folder: ${path.basename(screenshotPath)}`);
        } catch (screenshotError) {
            logger.warn(`Screenshot failed: ${screenshotError.message}`);
        }

        // Get output directory paths for reference
        try {
            const paths = await driver.getOutputDirectoryConfig();
            logger.info(`Output directories available at: ${paths.baseDirectory}`);
            logger.info(`Screenshots directory: ${paths.screenshotsDirectory}`);
            logger.info(`Videos directory: ${paths.videosDirectory} (ready for future video recording)`);
        } catch (pathError) {
            logger.warn(`Could not get output paths: ${pathError.message}`);
        }

        // Wait a moment to see the browser (if not headless)
        if (!selenium.headless) {
            logger.info('Waiting 3 seconds (browser visible)...');
            await driver.wait(3000);
        }

        logger.info('✅ Web automation example completed successfully!');

    } catch (error) {
        logger.error(`❌ Web automation failed: ${error.message || error.toString()}`);
        if (error.stack) {
            logger.error(`Stack trace: ${error.stack}`);
        }
    } finally {
        // Always close the browser
        if (driver && driver.isStarted) {
            logger.info('Closing browser...');
            await driver.quit();
        }
    }
}

// Run the example
if (require.main === module) {
    simpleWebAutomationExample().catch(error => {
        console.error('Example failed:', error.message);
        process.exit(1);
    });
}