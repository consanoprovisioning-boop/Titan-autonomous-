using System.Diagnostics;
using System.Text.Json;

namespace Titan.Launcher;

/// <summary>
/// Home-screen app for Titan Autonomous COS on Alienware-18.
/// This window never opens Eleads itself and never types a password:
/// it launches the local Titan.ChiefOfStaff program (checkin / tick / loop)
/// that works Glenn's already-signed-in Chrome on CDP 9222.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Label _status = new() { AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) };
    private readonly Label _detail = new() { AutoSize = true };
    private readonly RichTextBox _log = new() { ReadOnly = true, Dock = DockStyle.Fill, Font = new Font(FontFamily.GenericMonospace, 9f) };
    private readonly Button _checkin = new() { Text = "1  Check In / Verify", AutoSize = true };
    private readonly Button _startLoop = new() { Text = "2  Start Live CRM Loop", AutoSize = true };
    private readonly Button _tickOnce = new() { Text = "Tick Once", AutoSize = true };
    private readonly Button _stop = new() { Text = "Stop Loop", AutoSize = true, Enabled = false };
    private readonly Button _refresh = new() { Text = "Refresh Status", AutoSize = true };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 5000 };
    private Process? _loopProcess;

    public MainForm()
    {
        Text = "Titan Autonomous COS — Valdosta (Bordine, Glenn)";
        Width = 860;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;

        var header = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(10),
            Text = "Titan Autonomous COS  •  Valdosta Nissan 28206 / Mitsubishi 28546 only — never TKO 6220\r\n" +
                   "Sign in to Eleads yourself in Chrome. Titan never types your password. Leave Chrome open."
        };

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10) };
        buttons.Controls.AddRange([_checkin, _startLoop, _tickOnce, _stop, _refresh]);

        var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10, 0, 10, 5) };
        statusPanel.Controls.Add(_status);
        statusPanel.Controls.Add(_detail);

        Controls.Add(_log);
        Controls.Add(statusPanel);
        Controls.Add(buttons);
        Controls.Add(header);

        _checkin.Click += async (_, _) => await RunHostAsync("checkin", isLoop: false);
        _tickOnce.Click += async (_, _) => await RunHostAsync("tick", isLoop: false);
        _startLoop.Click += async (_, _) => await RunHostAsync("loop", isLoop: true);
        _stop.Click += (_, _) => StopLoop();
        _refresh.Click += (_, _) => RefreshStatus();
        _timer.Tick += (_, _) => RefreshStatus();
        _timer.Start();
        FormClosing += (_, _) => StopLoop();

        Append("Titan COS ready. Step 1: Check In / Verify. Step 2: Start Live CRM Loop.");
        RefreshStatus();
    }

    private static string HostExe()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, "Titan.ChiefOfStaff.exe");
        if (File.Exists(beside))
        {
            return beside;
        }

        var installed = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TitanChief", "app", "Titan.ChiefOfStaff.exe");
        return File.Exists(installed) ? installed : beside;
    }

    private static string DataRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TitanChief");

    private void RefreshStatus()
    {
        try
        {
            var verify = ReadJson(Path.Combine(DataRoot(), "verify.json"));
            var verified = verify is not null && verify.Value.TryGetProperty("glennVerified", out var g) && g.GetBoolean();
            var rooftop = verify is not null && verify.Value.TryGetProperty("rooftop", out var r) ? r.GetString() : null;
            var state = verify is not null && verify.Value.TryGetProperty("state", out var s) ? s.GetString() : null;
            var et = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
            var smsOpen = et.Hour >= 9 && et.Hour < 19;

            _status.Text = verified
                ? $"● VERIFIED — Bordine, Glenn on rooftop {rooftop} ({state})"
                : "○ Not verified yet — click Check In / Verify, sign in, then click it again";
            _detail.Text = $"ET now {et:ddd HH:mm} • SMS window {(smsOpen ? "OPEN 09:00–19:00" : "closed (email-only ticks)")} • Loop: {(_loopProcess is { HasExited: false } ? "RUNNING" : "stopped")} • Host: {HostExe()}";
        }
        catch (Exception ex)
        {
            _status.Text = "Status unreadable — run Check In / Verify";
            _detail.Text = ex.Message;
        }
    }

    private static JsonElement? ReadJson(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }

    private async Task RunHostAsync(string verb, bool isLoop)
    {
        var exe = HostExe();
        if (!File.Exists(exe))
        {
            Append($"Missing {exe}. Run INSTALL.cmd once, then reopen Titan COS.");
            return;
        }

        var psi = new ProcessStartInfo(exe, verb)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Append($"--- Titan.ChiefOfStaff {verb} ---");
        try
        {
            var process = Process.Start(psi)!;
            if (isLoop)
            {
                _loopProcess = process;
                _stop.Enabled = true;
                _startLoop.Enabled = false;
            }

            process.OutputDataReceived += (_, e) => { if (e.Data is not null) Append(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data is not null) Append(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            Append($"--- exit {process.ExitCode} ---");
        }
        catch (Exception ex)
        {
            Append("Could not start Titan: " + ex.Message);
        }
        finally
        {
            if (isLoop)
            {
                _loopProcess = null;
                _stop.Enabled = false;
                _startLoop.Enabled = true;
            }

            RefreshStatus();
        }
    }

    private void StopLoop()
    {
        try
        {
            if (_loopProcess is { HasExited: false })
            {
                _loopProcess.Kill(entireProcessTree: true);
                Append("Loop stopped. Chrome left open (Titan never closes your signed-in window).");
            }
        }
        catch (Exception ex)
        {
            Append("Stop failed: " + ex.Message);
        }
    }

    private void Append(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Append(line));
            return;
        }

        _log.AppendText(line + Environment.NewLine);
        _log.ScrollToCaret();
    }
}
