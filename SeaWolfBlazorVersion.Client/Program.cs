using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SeaWolfBlazorVersion.Client.Services;

namespace SeaWolfBlazorVersion.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddScoped<HighScoreService>();
        builder.Services.AddScoped<AudioService>();

        await builder.Build().RunAsync();
    }
}
