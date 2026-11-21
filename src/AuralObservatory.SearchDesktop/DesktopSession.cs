using System;
using System.Diagnostics;
using System.IO;

namespace AuralObservatory.SearchDesktop;

/// <summary>
/// Cross-platform, browser-based desktop launcher and per-user process guard.
/// This is not an embedded native WebView or OS-specific application UI.
/// Local user state is stored outside the installed executable.
/// </summary>
public sealed class DesktopSession : IDisposable
{
    private readonly FileStream _lock;
    private DesktopSession(FileStream fileLock) => _lock = fileLock;
    public static DesktopSession? TryAcquire(string userDataDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userDataDirectory);
        Directory.CreateDirectory(userDataDirectory);
        try {
            // FileShare.None is enforced by OS; a stale file alone does not prevent reentry.
            var stream = new FileStream(Path.Combine(userDataDirectory, "desktop-session.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            return new DesktopSession(stream);
        }
        catch (IOException) { return null; }
    }

    public static bool ShouldLaunchBrowser(string[] args, Func<string, string?> getEnvironment)
    {
        ArgumentNullException.ThrowIfNull(args);
        if(args.Any(a => a is "--no-browser" or "--headless")) return false;
        if(string.Equals(getEnvironment("AURAL_NO_BROWSER"),"1",StringComparison.Ordinal)) return false;
        // Avoid trying to open a GUI from a headless Linux CI/SSH service.
        if(OperatingSystem.IsLinux() &&
            string.IsNullOrWhiteSpace(getEnvironment("DISPLAY")) &&
            string.IsNullOrWhiteSpace(getEnvironment("WAYLAND_DISPLAY"))) return false;
        return true;
    }

    public static bool TryLaunchBrowser(Uri url, Action<string> report)
    {
        if(url.Scheme != Uri.UriSchemeHttp || url.Host != "127.0.0.1")
            throw new ArgumentException("Desktop launcher only accepts loopback HTTP URLs");
        try {
            ProcessStartInfo start;
            if(OperatingSystem.IsMacOS()) {
                start = new ProcessStartInfo("open") { UseShellExecute=false, CreateNoWindow=true };
                start.ArgumentList.Add(url.AbsoluteUri);
            } else if(OperatingSystem.IsLinux()) {
                start = new ProcessStartInfo("xdg-open") { UseShellExecute=false, CreateNoWindow=true };
                start.ArgumentList.Add(url.AbsoluteUri);
            } else {
                start = new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute=true };
            }
            using var process = Process.Start(start);
            if(process is null)throw new InvalidOperationException("Default browser launcher returned no process");
            report("Opened default browser. Use 'Exit application' in the page or close this process to stop the local server.");
            return true;
        }
        catch(Exception ex) when(ex is System.ComponentModel.Win32Exception or InvalidOperationException or IOException) {
            report("Could not open the default browser: " + ex.Message + ". Open " + url.AbsoluteUri + " manually.");
            return false;
        }
    }
    public void Dispose() => _lock.Dispose();
}
