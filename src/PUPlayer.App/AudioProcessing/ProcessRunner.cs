using System.Diagnostics;

namespace PUPlayer.App.AudioProcessing;

public sealed record ProcessCommand(string FileName, IReadOnlyList<string> Arguments, string? WorkingDirectory = null,
    IReadOnlyDictionary<string, string>? Environment = null);
public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(ProcessCommand command, CancellationToken cancellationToken)
    {
        var info = new ProcessStartInfo(command.FileName)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = command.WorkingDirectory ?? string.Empty
        };
        foreach (var argument in command.Arguments) info.ArgumentList.Add(argument);
        if (command.Environment is not null)
            foreach (var (name, value) in command.Environment) info.Environment[name] = value;
        using var process = Process.Start(info) ?? throw new InvalidOperationException($"Could not start {command.FileName}.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync(cancellationToken); }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(true);
            await process.WaitForExitAsync();
            throw;
        }
        return new(process.ExitCode, await output, await error);
    }
}
