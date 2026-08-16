using System.Diagnostics;
using System.IO;

namespace PUPlayer.IntegrationTests.App;

public sealed class DistributionTests
{
    [Fact]
    public void InstallerSelector_UsesIndependentDefinitions()
    {
        var script = Path.Combine(TestPaths.Repository, "scripts", "select-installer.ps1");
        Assert.EndsWith("installer\\IgoonTube.iss", Select(false), StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("installer\\IgoonTube-NoAI.iss", Select(true), StringComparison.OrdinalIgnoreCase);

        string Select(bool noAi)
        {
            var flag = noAi ? " -NoAI" : "";
            using var process = Process.Start(new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ProjectRoot \"{TestPaths.Repository}\"{flag}")
            { UseShellExecute = false, RedirectStandardOutput = true })!;
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            Assert.Equal(0, process.ExitCode);
            return output;
        }
    }

    [Fact]
    public void OptionalAssets_AreIncludedOnlyInFullPackage()
    {
        var root = Path.Combine(Path.GetTempPath(), "igoontube-layout-" + Guid.NewGuid().ToString("N"));
        var project = Path.Combine(root, "project");
        var full = Path.Combine(root, "full");
        var noAi = Path.Combine(root, "noai");
        Directory.CreateDirectory(Path.Combine(project, ".tools", "ai"));
        Directory.CreateDirectory(Path.Combine(project, "data", "models"));
        Directory.CreateDirectory(Path.Combine(project, "scripts"));
        Directory.CreateDirectory(Path.Combine(project, "docs"));
        File.WriteAllText(Path.Combine(project, ".tools", "ai", "runtime.bin"), "ai");
        File.WriteAllText(Path.Combine(project, "data", "models", "model.bin"), "model");
        File.WriteAllText(Path.Combine(project, "scripts", "vision_host.py"), "vision");
        File.WriteAllText(Path.Combine(project, "docs", "IgoonTube-LEEME.txt"), "full");
        File.WriteAllText(Path.Combine(project, "docs", "IgoonTube-NoAI-README.txt"), "noai");
        try
        {
            var script = Path.Combine(TestPaths.Repository, "scripts", "stage-optional-assets.ps1");
            Assert.Equal(0, Run(script, project, full, false));
            Assert.Equal(0, Run(script, project, noAi, true));
            Assert.True(File.Exists(Path.Combine(full, ".tools", "ai", "runtime.bin")));
            Assert.True(File.Exists(Path.Combine(full, "data", "models", "model.bin")));
            Assert.True(File.Exists(Path.Combine(full, "scripts", "vision_host.py")));
            Assert.False(Directory.Exists(Path.Combine(noAi, ".tools", "ai")));
            Assert.False(Directory.Exists(Path.Combine(noAi, "data", "models")));
            Assert.False(File.Exists(Path.Combine(noAi, "scripts", "vision_host.py")));
            Assert.Equal("noai", File.ReadAllText(Path.Combine(noAi, "LEEME.txt")));
        }
        finally { Directory.Delete(root, true); }

        static int Run(string script, string project, string target, bool noAi)
        {
            var flag = noAi ? " -NoAI" : "";
            using var process = Process.Start(new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -ProjectRoot \"{project}\" -Staging \"{target}\"{flag}") { UseShellExecute = false })!;
            process.WaitForExit();
            return process.ExitCode;
        }
    }

    [Fact]
    public void HashScript_WritesSortedLowercaseHashesForFinalArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "igoontube-hashes-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "nested"));
        File.WriteAllText(Path.Combine(root, "z.zip"), "z");
        File.WriteAllText(Path.Combine(root, "nested", "a.exe"), "a");
        File.WriteAllText(Path.Combine(root, "ignored.txt"), "ignore");
        try
        {
            var script = Path.Combine(TestPaths.Repository, "scripts", "write-release-hashes.ps1");
            var process = Process.Start(new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -DistRoot \"{root}\"")
            { UseShellExecute = false })!;
            process.WaitForExit();

            Assert.Equal(0, process.ExitCode);
            var lines = File.ReadAllLines(Path.Combine(root, "SHA256SUMS.txt"));
            Assert.Equal(2, lines.Length);
            Assert.Contains("nested/a.exe", lines[0]);
            Assert.Contains("z.zip", lines[1]);
            Assert.All(lines, line => Assert.Equal(line, line.ToLowerInvariant()));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void Release_IsDocumentedAsUnsignedAndEnglishFirst()
    {
        var readme = File.ReadAllText(Path.Combine(TestPaths.Repository, "docs", "IgoonTube-LEEME.txt"));
        Assert.Contains("English is the default", readme);
        Assert.Contains("unsigned", readme, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Spanish", readme);
        Assert.DoesNotContain("se añadirán en fases posteriores", File.ReadAllText(Path.Combine(TestPaths.Repository, "PRODUCT.md")));
    }
}
