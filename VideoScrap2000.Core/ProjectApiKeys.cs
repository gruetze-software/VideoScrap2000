using System.Reflection;

namespace MovieScrap2000;

internal static class ProjectApiKeys
{
    private static readonly string? BundledTvdb = typeof(ProjectApiKeys).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(attribute => attribute.Key == "TvdbProjectApiKey")?.Value;

    internal static string? Tvdb(string? developmentOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(developmentOverride)) return developmentOverride.Trim();
        var environment = Environment.GetEnvironmentVariable("TVDB_PROJECT_API_KEY");
        return !string.IsNullOrWhiteSpace(environment) ? environment.Trim() : BundledTvdb;
    }
}
