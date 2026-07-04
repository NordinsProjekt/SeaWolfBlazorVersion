using SeaWolfBlazorVersion.Components;
using SeaWolfBlazorVersion.Client.Services;

namespace SeaWolfBlazorVersion;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddScoped<HighScoreService>();
        builder.Services.AddScoped<AudioService>();

        // Compress dynamically-rendered responses (the root document is
        // rendered by MapRazorComponents, not served as a static file, so
        // MapStaticAssets()'s build-time compression doesn't cover it).
        // EnableForHttps is safe here: this app has no per-user reflected
        // secrets in its responses for a BREACH-style attack to target.
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        var app = builder.Build();

        app.UseResponseCompression();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(SeaWolfBlazorVersion.Client.Program).Assembly);

        app.Run();
    }
}
