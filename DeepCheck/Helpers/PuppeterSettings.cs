using PuppeteerSharp;

namespace DeepCheck.Helpers;

public class PuppeteerSettings
{
    public PuppeteerSettings()
    {
        LaunchOptions = new LaunchOptions()
        {
            Headless = true,
        };
    }

    public LaunchOptions LaunchOptions { get; set; }
}