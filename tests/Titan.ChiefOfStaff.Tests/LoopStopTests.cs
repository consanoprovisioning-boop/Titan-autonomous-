using System.Diagnostics;
using Titan.ChiefOfStaff;
using Xunit;

namespace Titan.ChiefOfStaff.Tests;

public class LoopStopTests
{
    [Fact]
    public void Stopping_the_host_leaves_its_child_process_running()
    {
        if (!OperatingSystem.IsLinux() || !File.Exists("/bin/bash") || !File.Exists("/bin/sleep"))
        {
            return;
        }

        var host = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/bash",
            ArgumentList = { "-c", "/bin/sleep 30 & echo $!; wait" },
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        Assert.NotNull(host);

        var childIdText = host!.StandardOutput.ReadLine();
        Assert.True(int.TryParse(childIdText, out var childId));
        var child = Process.GetProcessById(childId);
        try
        {
            LoopStop.StopHost(host);
            Assert.True(host.WaitForExit(5000));
            child.Refresh();
            Assert.False(child.HasExited);
        }
        finally
        {
            try
            {
                child.Refresh();
                if (!child.HasExited)
                {
                    child.Kill();
                    child.WaitForExit(2000);
                }
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                if (!host.HasExited)
                {
                    host.Kill();
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
