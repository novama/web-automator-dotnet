using Microsoft.Extensions.Configuration;

namespace WebAutomator.Common.Utils;

/// <summary>
///     Configuration manager for loading and accessing JSON configuration files
/// </summary>
public class ConfigManager
{
    private readonly string _configPath;
    private readonly IConfiguration _configuration;

    /// <summary>
    ///     Initializes a new instance of the ConfigManager class
    /// </summary>
    /// <param name="configPath">Path to the JSON configuration file</param>
    public ConfigManager(string configPath)
    {
        _configPath = configPath;

        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configPath, false, true);

        _configuration = configurationBuilder.Build();

        Console.WriteLine($"Configuration loaded from: {configPath}");
    }

    /// <summary>
    ///     Gets a configuration value by key with optional default value
    /// </summary>
    /// <param name="key">Configuration key (supports nested keys with : separator)</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>Configuration value or default value</returns>
    public string Get(string key, string defaultValue = "")
    {
        var value = _configuration[key];
        return value ?? defaultValue;
    }

    /// <summary>
    ///     Gets a boolean configuration value by key with optional default value
    /// </summary>
    /// <param name="key">Configuration key (supports nested keys with : separator)</param>
    /// <param name="defaultValue">Default value if key is not found</param>
    /// <returns>Boolean configuration value or default value</returns>
    public bool Get(string key, bool defaultValue = false)
    {
        var value = _configuration[key];
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Gets an integer configuration value by key with optional default value
    /// </summary>
    /// <param name="key">Configuration key (supports nested keys with : separator)</param>
    /// <param name="defaultValue">Default value if key is not found or cannot be parsed</param>
    /// <returns>Integer configuration value or default value</returns>
    public int? Get(string key, int? defaultValue = null)
    {
        var value = _configuration[key];
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Gets an integer configuration value by key with non-nullable default value
    /// </summary>
    /// <param name="key">Configuration key (supports nested keys with : separator)</param>
    /// <param name="defaultValue">Default value if key is not found or cannot be parsed</param>
    /// <returns>Integer configuration value or default value</returns>
    public int Get(string key, int defaultValue)
    {
        var value = _configuration[key];
        if (string.IsNullOrEmpty(value))
            return defaultValue;

        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    /// <summary>
    ///     Gets a strongly-typed configuration section
    /// </summary>
    /// <typeparam name="T">Type to bind the configuration to</typeparam>
    /// <param name="sectionKey">Section key</param>
    /// <returns>Configuration section bound to type T</returns>
    public T GetSection<T>(string sectionKey) where T : new()
    {
        var section = _configuration.GetSection(sectionKey);
        var result = new T();
        section.Bind(result);
        return result;
    }

    /// <summary>
    ///     Checks if a configuration key exists
    /// </summary>
    /// <param name="key">Configuration key</param>
    /// <returns>True if key exists</returns>
    public bool Has(string key)
    {
        return _configuration[key] != null;
    }

    /// <summary>
    ///     Gets all configuration as dictionary
    /// </summary>
    /// <returns>Dictionary of all configuration values</returns>
    public Dictionary<string, string> GetAll()
    {
        return _configuration.AsEnumerable()
            .Where(pair => pair.Value != null)
            .ToDictionary(pair => pair.Key, pair => pair.Value!);
    }
}