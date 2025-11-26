using WebAutomator.Examples;

namespace WebAutomator;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("    Web Automator .NET 9 - Examples");
        Console.WriteLine("===========================================\n");

        Console.WriteLine("Select an example to run:");
        Console.WriteLine("1. Selenium Simple Example");
        Console.WriteLine("2. Playwright Simple Example");
        Console.WriteLine("3. Run Both Examples");
        Console.WriteLine("0. Exit\n");

        Console.Write("Enter your choice: ");
        var choice = Console.ReadLine();

        Console.WriteLine();

        try
        {
            switch (choice)
            {
                case "1":
                    await SimpleSelenium.Run();
                    break;

                case "2":
                    await SimplePlaywright.Run();
                    break;

                case "3":
                    Console.WriteLine(">>> Running Selenium Example...\n");
                    await SimpleSelenium.Run();

                    Console.WriteLine("\n\n>>> Running Playwright Example...\n");
                    await SimplePlaywright.Run();
                    break;

                case "0":
                    Console.WriteLine("Exiting...");
                    return;

                default:
                    Console.WriteLine("Invalid choice. Exiting...");
                    return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Fatal Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }

        Console.WriteLine("\n\nPress any key to exit...");
        Console.ReadKey();
    }
}