using System.Diagnostics;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.Services;

namespace DocumentGenerator.Infrastructure.Documents;

public sealed class LibreOfficeWordToPdfConverterService : IWordToPdfConverterService
{
    private const string PdfContentType = "application/pdf";
    private static readonly string[] ExecutableCandidates =
    [
        "soffice",
        "libreoffice"
    ];

    public async Task<GeneratedDocument> ConvertAsync(
        Stream wordDocumentStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wordDocumentStream);
        ResetStream(wordDocumentStream, cancellationToken);

        var workingDirectory = CreateWorkingDirectory();
        var inputPath = Path.Combine(workingDirectory, "document.docx");
        var outputPath = Path.Combine(workingDirectory, "document.pdf");

        try
        {
            await WriteInputDocumentAsync(wordDocumentStream, inputPath, cancellationToken);
            var conversionOutput = await ConvertDocumentAsync(inputPath, workingDirectory, cancellationToken);

            return await ReadGeneratedDocumentAsync(outputPath, conversionOutput, cancellationToken);
        }
        finally
        {
            TryDeleteDirectory(workingDirectory);
        }
    }

    private static Process StartConversionProcess(string inputPath, string outputDirectory)
    {
        var executablePath = ResolveExecutablePath();
        var startInfo = CreateStartInfo(executablePath, inputPath, outputDirectory);
        var process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        return process;
    }

    private static void ResetStream(Stream wordDocumentStream, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (wordDocumentStream.CanSeek)
        {
            wordDocumentStream.Position = 0;
        }
    }

    private static string CreateWorkingDirectory()
    {
        var workingDirectory = Path.Combine(
            Path.GetTempPath(),
            "document-generator-pdf",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(workingDirectory);
        return workingDirectory;
    }

    private static async Task WriteInputDocumentAsync(
        Stream wordDocumentStream,
        string inputPath,
        CancellationToken cancellationToken)
    {
        await using var inputFileStream = File.Create(inputPath);
        await wordDocumentStream.CopyToAsync(inputFileStream, cancellationToken);
    }

    private static async Task<ConversionOutput> ConvertDocumentAsync(
        string inputPath,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        using var process = StartConversionProcess(inputPath, outputDirectory);
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await WaitForExitOrKillAsync(process, cancellationToken);

        var conversionOutput = new ConversionOutput(
            process.ExitCode,
            await standardOutputTask,
            await standardErrorTask);

        EnsureSuccessfulConversion(conversionOutput);
        return conversionOutput;
    }

    private static async Task<GeneratedDocument> ReadGeneratedDocumentAsync(
        string outputPath,
        ConversionOutput conversionOutput,
        CancellationToken cancellationToken)
    {
        EnsureOutputExists(outputPath, conversionOutput);

        var pdfBytes = await File.ReadAllBytesAsync(outputPath, cancellationToken);
        var outputStream = new MemoryStream(pdfBytes);
        outputStream.Position = 0;

        return new GeneratedDocument(outputStream, PdfContentType);
    }

    private static async Task WaitForExitOrKillAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
    }

    private static void EnsureSuccessfulConversion(ConversionOutput conversionOutput)
    {
        if (conversionOutput.ExitCode == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"LibreOffice PDF conversion failed with exit code {conversionOutput.ExitCode}. Stdout: {conversionOutput.StandardOutput} Stderr: {conversionOutput.StandardError}");
    }

    private static void EnsureOutputExists(string outputPath, ConversionOutput conversionOutput)
    {
        if (File.Exists(outputPath))
        {
            return;
        }

        throw new InvalidOperationException(
            $"LibreOffice PDF conversion did not produce '{outputPath}'. Stdout: {conversionOutput.StandardOutput} Stderr: {conversionOutput.StandardError}");
    }

    private static ProcessStartInfo CreateStartInfo(string executablePath, string inputPath, string outputDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--headless");
        startInfo.ArgumentList.Add("--convert-to");
        startInfo.ArgumentList.Add("pdf:writer_pdf_Export");
        startInfo.ArgumentList.Add("--outdir");
        startInfo.ArgumentList.Add(outputDirectory);
        startInfo.ArgumentList.Add(inputPath);

        return startInfo;
    }

    private static string ResolveExecutablePath()
    {
        var executablePath = GetExecutableCandidates()
            .Select(TryResolveExecutablePath)
            .FirstOrDefault(path => !string.IsNullOrWhiteSpace(path));

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            return executablePath;
        }

        throw new InvalidOperationException(
            "LibreOffice executable was not found. Configure SOFFICE_PATH or install LibreOffice.");
    }

    private static IEnumerable<string> GetExecutableCandidates()
    {
        var configuredPath = Environment.GetEnvironmentVariable("SOFFICE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath;
        }

        configuredPath = Environment.GetEnvironmentVariable("LIBREOFFICE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            yield return configuredPath;
        }

        foreach (var candidate in ExecutableCandidates)
        {
            yield return candidate;
        }
    }

    private static string? TryResolveExecutablePath(string candidate)
    {
        if (Path.IsPathRooted(candidate))
        {
            return File.Exists(candidate) ? candidate : null;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var resolvedPath = Path.Combine(directory, candidate);
            if (File.Exists(resolvedPath))
            {
                return resolvedPath;
            }
        }

        return null;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup for a canceled conversion process.
        }
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup for temp conversion artifacts.
        }
    }

    private sealed record ConversionOutput(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
