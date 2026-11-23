using System.Reflection;

namespace WinUI.TableView.SampleApp;

/// <summary>
/// The newest released version of the library, read from the release notes the build lists in the assembly metadata.
/// </summary>
public static class ReleaseInfo
{
    public static Version LatestRelease { get; } = FindLatestRelease();

    private static Version FindLatestRelease()
    {
        var releases = typeof(ReleaseInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "ReleasedVersions")?.Value ?? string.Empty;

        // "v1.5.0-preview2" counts as 1.5: a preview of a version means that version is out.
        return releases.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(name => Version.TryParse(name.TrimStart('v').Split('-')[0], out var version) ? new Version(version.Major, version.Minor) : null)
            .OfType<Version>()
            .DefaultIfEmpty(new Version(0, 0))
            .Max()!;
    }
}
