using System.Diagnostics;
using DocumentGenerator.Domain.Documents;
using DocumentGenerator.Domain.Services;

namespace DocumentGenerator.Infrastructure.Documents;

public sealed class LibreOfficeWordToPdfConverterService : IWordToPdfConverterService
{
    private const string PdfContentType = "application/pdf";
    private const string InputFileName = "document.docx";
    private const string OutputFileName = "document.pdf";
    private static readonly SemaphoreSlim ConversionSemaphore = new(1, 1);
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromSeconds(30);

    public async Task<GeneratedDocument> ConvertAsync(
        Stream wordDocumentStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(wordDocumentStream);
        ResetStream(wordDocumentStream, cancellationToken);

        var semaphoreAcquired = false;
        string? workingDirectory = null;

        try
        {
            await ConversionSemaphore.WaitAsync(cancellationToken);
            semaphoreAcquired = true;

            workingDirectory = CreateWorkingDirectory();
            var inputPath = Path.Combine(workingDirectory, InputFileName);
            var outputPath = Path.Combine(workingDirectory, OutputFileName);

            await WriteInputDocumentAsync(wordDocumentStream, inputPath, cancellationToken);
            var conversionOutput = await ConvertDocumentAsync(workingDirectory, cancellationToken);

            return await ReadGeneratedDocumentAsync(outputPath, conversionOutput, cancellationToken);
        }
        finally
        {
            if (workingDirectory is not null)
            {
                TryDeleteDirectory(workingDirectory);
            }

            if (semaphoreAcquired)
            {
                ConversionSemaphore.Release();
            }
        }
    }

    private static Process StartConversionProcess(ConversionCommand conversionCommand, string workingDirectory)
    {
        var startInfo = CreateStartInfo(conversionCommand, workingDirectory);
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
        string workingDirectory,
        CancellationToken cancellationToken)
    {
        var conversionCommand = CreateConversionCommand(workingDirectory);

        using var process = StartConversionProcess(conversionCommand, workingDirectory);
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await WaitForExitOrKillAsync(process, conversionCommand, standardOutputTask, standardErrorTask, cancellationToken);

        var conversionOutput = new ConversionOutput(
            process.ExitCode,
            conversionCommand.BackendName,
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

    private static async Task WaitForExitOrKillAsync(
        Process process,
        ConversionCommand conversionCommand,
        Task<string> standardOutputTask,
        Task<string> standardErrorTask,
        CancellationToken cancellationToken)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource(ConversionTimeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutCancellationTokenSource.Token);

        try
        {
            await process.WaitForExitAsync(linkedCancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (timeoutCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None);

            var conversionOutput = new ConversionOutput(
                process.ExitCode,
                conversionCommand.BackendName,
                await standardOutputTask,
                await standardErrorTask);

            throw new TimeoutException(
                $"{conversionOutput.BackendName} PDF conversion timed out after {ConversionTimeout.TotalSeconds:0} seconds. Stdout: {conversionOutput.StandardOutput} Stderr: {conversionOutput.StandardError}");
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            await process.WaitForExitAsync(CancellationToken.None);
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
            $"{conversionOutput.BackendName} PDF conversion failed with exit code {conversionOutput.ExitCode}. Stdout: {conversionOutput.StandardOutput} Stderr: {conversionOutput.StandardError}");
    }

    private static void EnsureOutputExists(string outputPath, ConversionOutput conversionOutput)
    {
        if (File.Exists(outputPath))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{conversionOutput.BackendName} PDF conversion did not produce '{outputPath}'. Stdout: {conversionOutput.StandardOutput} Stderr: {conversionOutput.StandardError}");
    }

    private static ProcessStartInfo CreateStartInfo(ConversionCommand conversionCommand, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = conversionCommand.ExecutablePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in conversionCommand.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static ConversionCommand CreateConversionCommand(string workingDirectory)
    {
        if (OperatingSystem.IsMacOS())
        {
            var sofficePath = ExecutablePathResolver.ResolveLibreOfficePath();

            return new ConversionCommand(
                "soffice",
                sofficePath,
                [
                    "--headless",
                    "--convert-to",
                    "pdf:writer_pdf_Export",
                    "--outdir",
                    workingDirectory,
                    Path.Combine(workingDirectory, InputFileName)
                ]);
        }

        var unoconvPath = ExecutablePathResolver.ResolveUnoconvPath();

        return new ConversionCommand(
            "unoconv",
            unoconvPath,
            [
                "-f",
                "pdf",
                "-o",
                OutputFileName,
                InputFileName
            ]);
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
        string BackendName,
        string StandardOutput,
        string StandardError);

    private sealed record ConversionCommand(
        string BackendName,
        string ExecutablePath,
        IReadOnlyCollection<string> Arguments);
}
