using Microsoft.Web.WebView2.Core;
using NexaPlay.Core.Constants;

namespace NexaPlay.Presentation.Helpers;

internal static class WebView2EnvironmentHelper
{
    private static readonly Lazy<Task<CoreWebView2Environment>> SharedEnvironment =
        new(CreateEnvironmentAsync);

    public static Task<CoreWebView2Environment> GetAsync() => SharedEnvironment.Value;

    private static async Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        var userDataFolder = Path.Combine(AppConstants.AppDataPath, "WebView2");
        Directory.CreateDirectory(userDataFolder);
        return await CoreWebView2Environment.CreateWithOptionsAsync(null, userDataFolder, null);
    }
}
