using AuralObservatory.SearchDesktop;
using System;
using System.IO;

var dir=Path.Combine(Path.GetTempPath(),"aural-desktop-session-"+Guid.NewGuid().ToString("N"));
try
{
  using(var first=DesktopSession.TryAcquire(dir))
  {
    Assert(first is not null,"first session acquired");
    var concurrent=DesktopSession.TryAcquire(dir);
    Assert(concurrent is null,"concurrent process session rejected");
    Assert(DesktopSession.ShouldLaunchBrowser(Array.Empty<string>(),_=>"1")==false
      || OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(),
      "headless environment guard where supported");
    Assert(!DesktopSession.ShouldLaunchBrowser(new[]{"--no-browser"},_=>null),"headless CLI switch");
    Assert(!DesktopSession.ShouldLaunchBrowser(Array.Empty<string>(),key=>key=="AURAL_NO_BROWSER"?"1":null),
      "explicit environment opt out");
    try{DesktopSession.TryLaunchBrowser(new Uri("https://example.com"),Console.WriteLine);
      throw new Exception("Remote browser URL was accepted");}
    catch(ArgumentException){Console.WriteLine("PASS: remote host prohibited");}
  }
  using var again=DesktopSession.TryAcquire(dir);
  Assert(again is not null,"lock released on exit; stale lock filename harmless");
}
finally { if(Directory.Exists(dir))Directory.Delete(dir, true); }
Console.WriteLine("PASS desktop session guard and launch-policy tests");
static void Assert(bool yes,string what){if(!yes)throw new Exception("FAIL "+what);Console.WriteLine("PASS "+what);}
