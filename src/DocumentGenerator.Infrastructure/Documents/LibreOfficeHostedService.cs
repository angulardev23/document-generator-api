using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DocumentGenerator.Infrastructure.Documents;

public sealed class LibreOfficeHostedService(ILogger<LibreOfficeHostedService> logger) : IHostedService, IDisposable
{
    private readonly Lock _syncLock = new();
    private Process? _process;
    private bool _isStopping;
    private bool _disposed;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        Process? startedProcess = null;
        var standardOutput = new StringBuilder();
        var standardError = new StringBuilder();

        try
        {
            var executablePath = ExecutablePathResolver.ResolveLibreOfficePath();
            startedProcess = CreateProcess(executablePath, standardOutput, standardError);

            if (!startedProcess.Start())
            {
                throw new InvalidOperationException("Failed to start the LibreOffice listener process.");
            }

            startedProcess.BeginOutputReadLine();
            startedProcess.BeginErrorReadLine();

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            if (startedProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"LibreOffice listener exited prematurely with exit code {startedProcess.ExitCode}. Stdout: {standardOutput} Stderr: {standardError}");
            }

            lock (_syncLock)
            {
                _process = startedProcess;
                _isStopping = false;
            }

            logger.LogInformation(
                "Started LibreOffice listener using '{ExecutablePath}' on 127.0.0.1:2002.",
                executablePath);
        }
        catch (Exception ex)
        {
            if (startedProcess is not null)
            {
                await TryKillAndWaitAsync(startedProcess, CancellationToken.None);
                startedProcess.Dispose();
            }

            logger.LogError(ex, "Failed to start LibreOffice listener.");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Process? processToStop;

        lock (_syncLock)
        {
            processToStop = _process;
            _process = null;
            _isStopping = true;
        }

        if (processToStop is null)
        {
            return;
        }

        logger.LogInformation("Stopping LibreOffice listener.");

        await TryKillAndWaitAsync(processToStop, cancellationToken);
        processToStop.Dispose();

        logger.LogInformation("Stopped LibreOffice listener.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        Process? processToDispose;

        lock (_syncLock)
        {
            processToDispose = _process;
            _process = null;
            _isStopping = true;
        }

        if (processToDispose is null)
        {
            return;
        }

        TryKill(processToDispose);
        processToDispose.Dispose();
    }

    private Process CreateProcess(
        string executablePath,
        StringBuilder standardOutput,
        StringBuilder standardError)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        processStartInfo.ArgumentList.Add("--headless");
        processStartInfo.ArgumentList.Add("--nologo");
        processStartInfo.ArgumentList.Add("--nodefault");
        processStartInfo.ArgumentList.Add("--nofirststartwizard");
        processStartInfo.ArgumentList.Add("--nolockcheck");
        processStartInfo.ArgumentList.Add("--norestore");
        processStartInfo.ArgumentList.Add("--invisible");
        processStartInfo.ArgumentList.Add("--accept=socket,host=127.0.0.1,port=2002;urp;");

        var libreOfficeProcess = new Process
        {
            StartInfo = processStartInfo,
            EnableRaisingEvents = true
        };

        libreOfficeProcess.OutputDataReceived += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }

            AppendOutput(standardOutput, args.Data);
            logger.LogDebug("LibreOffice stdout: {Output}", args.Data);
        };

        libreOfficeProcess.ErrorDataReceived += (_, args) =>
        {
            if (string.IsNullOrWhiteSpace(args.Data))
            {
                return;
            }

            AppendOutput(standardError, args.Data);
            logger.LogWarning("LibreOffice stderr: {Output}", args.Data);
        };

        libreOfficeProcess.Exited += (_, _) =>
        {
            var exitCode = libreOfficeProcess.ExitCode;

            if (_isStopping)
            {
                logger.LogInformation("LibreOffice listener exited with code {ExitCode}.", exitCode);
                return;
            }

            logger.LogError(
                "LibreOffice listener stopped unexpectedly with exit code {ExitCode}. Stdout: {Stdout} Stderr: {Stderr}",
                exitCode,
                standardOutput,
                standardError);
        };

        return libreOfficeProcess;
    }

    private static void AppendOutput(StringBuilder buffer, string line)
    {
        lock (buffer)
        {
            if (buffer.Length > 0)
            {
                buffer.AppendLine();
            }

            buffer.Append(line);
        }
    }

    private static async Task TryKillAndWaitAsync(Process process, CancellationToken cancellationToken)
    {
        TryKill(process);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown should still attempt process cleanup even if host cancellation is signaled.
        }
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
            // Best effort cleanup for the background LibreOffice process.
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
