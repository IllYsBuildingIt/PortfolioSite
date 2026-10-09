using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TemplateEngine.Abstractions;
using Microsoft.TemplateEngine.Abstractions.TemplatePackage;

namespace OnyxSpeedrun.Views;

internal sealed class BuiltInTemplatePackageProviderFactory : ITemplatePackageProviderFactory
{
    public static readonly Guid FactoryId = new("B6B5603D-3F5F-4F7E-9B5E-1A4F5C1C2E7E");

    public string DisplayName => ".NET SDK";

    public Guid Id => FactoryId;

    public ITemplatePackageProvider CreateProvider(IEngineEnvironmentSettings settings)
        => new BuiltInTemplatePackageProvider(this, settings);
}

internal sealed class BuiltInTemplatePackageProvider : ITemplatePackageProvider
{
    private readonly IEngineEnvironmentSettings _settings;

    public BuiltInTemplatePackageProvider(
        BuiltInTemplatePackageProviderFactory factory,
        IEngineEnvironmentSettings settings)
    {
        Factory = factory;
        _settings = settings;
    }

    public ITemplatePackageProviderFactory Factory { get; }

    public event Action? TemplatePackagesChanged
    {
        add { }
        remove { }
    }

    public Task<IReadOnlyList<ITemplatePackage>> GetAllTemplatePackagesAsync(
        CancellationToken cancellationToken)
    {
        var packages = new List<ITemplatePackage>();

        foreach (string folder in GetTemplateFolders())
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (string nupkg in Directory.EnumerateFiles(folder, "*.nupkg", SearchOption.TopDirectoryOnly))
            {
                packages.Add(new TemplatePackage(this, nupkg, File.GetLastWriteTime(nupkg)));
            }
        }

        return Task.FromResult<IReadOnlyList<ITemplatePackage>>(packages);
    }

    private static IEnumerable<string> GetTemplateFolders()
    {
        string dotnetRoot = FindDotnetRoot();
        if (string.IsNullOrEmpty(dotnetRoot))
            return [];

        var folders = new List<string>();

        string templatesRoot = Path.Combine(dotnetRoot, "templates");
        if (Directory.Exists(templatesRoot))
        {
            var versionDirs = new List<(string Path, Version Version)>();
            foreach (string dir in Directory.EnumerateDirectories(templatesRoot))
            {
                if (Version.TryParse(Path.GetFileName(dir), out Version? v))
                    versionDirs.Add((dir, v));
            }

            var bestByMajorMinor = new Dictionary<(int Major, int Minor), (string Path, Version Version)>();
            foreach (var (path, version) in versionDirs)
            {
                var key = (version.Major, version.Minor);
                if (!bestByMajorMinor.TryGetValue(key, out var best) || version > best.Version)
                {
                    bestByMajorMinor[key] = (path, version);
                }
            }

            foreach (var entry in bestByMajorMinor.OrderBy(e => e.Value.Version))
                folders.Add(entry.Value.Path);
        }

        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        string? sdkDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(runtimeDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))));
        if (sdkDir is not null)
        {
            string sdkTemplates = Path.Combine(sdkDir, "Templates");
            if (Directory.Exists(sdkTemplates))
                folders.Add(sdkTemplates);
        }

        return folders;
    }

    private static string FindDotnetRoot()
    {
        string? envRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrEmpty(envRoot) && Directory.Exists(envRoot))
            return envRoot;

        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        string candidate = Path.GetFullPath(Path.Combine(runtimeDir, "..", "..", ".."));
        if (Directory.Exists(candidate))
            return candidate;

        return string.Empty;
    }
}
