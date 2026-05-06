namespace DocumentGenerator.Infrastructure.Documents;

internal static class ExecutablePathResolver
{
    private static readonly string[] LibreOfficePathCandidates =
    [
        "soffice",
        "libreoffice"
    ];

    private static readonly string[] UnoconvPathCandidates =
    [
        "unoconv"
    ];

    public static string ResolveLibreOfficePath() => ResolveExecutablePath(
        environmentVariableNames:
        [
            "SOFFICE_PATH",
            "LIBREOFFICE_PATH"
        ],
        pathCandidates: LibreOfficePathCandidates,
        missingExecutableMessage:
        "LibreOffice executable was not found. Configure SOFFICE_PATH or LIBREOFFICE_PATH, or install LibreOffice.");

    public static string ResolveUnoconvPath() => ResolveExecutablePath(
        environmentVariableNames:
        [
            "UNOCONV_PATH"
        ],
        pathCandidates: UnoconvPathCandidates,
        missingExecutableMessage:
        "unoconv executable was not found. Configure UNOCONV_PATH or install unoconv.");

    private static string ResolveExecutablePath(
        IReadOnlyCollection<string> environmentVariableNames,
        IReadOnlyCollection<string> pathCandidates,
        string missingExecutableMessage)
    {
        var executablePath = GetCandidates(environmentVariableNames, pathCandidates)
            .Select(TryResolveExecutablePath)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return executablePath;
        }

        throw new InvalidOperationException(missingExecutableMessage);
    }

    private static IEnumerable<string> GetCandidates(
        IReadOnlyCollection<string> environmentVariableNames,
        IReadOnlyCollection<string> pathCandidates)
    {
        foreach (var environmentVariableName in environmentVariableNames)
        {
            var configuredPath = Environment.GetEnvironmentVariable(environmentVariableName);
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                yield return configuredPath;
            }
        }

        foreach (var candidate in pathCandidates)
        {
            yield return candidate;
        }
    }

    private static string? TryResolveExecutablePath(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        if (File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        if (ContainsDirectorySeparator(candidate))
        {
            return null;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var fileName in EnumerateCandidateFileNames(candidate))
            {
                var resolvedPath = Path.Combine(directory, fileName);
                if (File.Exists(resolvedPath))
                {
                    return resolvedPath;
                }
            }
        }

        return null;
    }

    private static bool ContainsDirectorySeparator(string candidate) =>
        candidate.Contains(Path.DirectorySeparatorChar) || candidate.Contains(Path.AltDirectorySeparatorChar);

    private static IEnumerable<string> EnumerateCandidateFileNames(string candidate)
    {
        yield return candidate;

        if (!OperatingSystem.IsWindows() || Path.HasExtension(candidate))
        {
            yield break;
        }

        var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(pathExt))
        {
            yield break;
        }

        foreach (var extension in pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return candidate + extension;
        }
    }
}
