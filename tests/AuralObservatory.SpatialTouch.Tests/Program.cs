using AuralObservatory.Core;

static class TouchIntentQualify
{
    private static int _cases;
    private static void Require(bool condition, string why)
    {
        _cases++;
        if (!condition) throw new InvalidOperationException("Spatial-touch oracle: " + why);
    }
    private static bool Near(double actual, double expected, double tolerance = 0.000001)
        => Math.Abs(actual - expected) <= tolerance;

    public static void Main()
    {
        var control = new SpatialTouchIntentProjector();
        Require(control.Snapshot == new SpatialTouchSnapshot(0, 0, 1, false, false, 0),
            "defaults are neutral and no side effect");
        Require(control.Begin(SpatialTouchZone.Direction, 41, 100, 100, 200, 300),
            "first direction captured");
        Require(control.Begin(SpatialTouchZone.HeightDistance, 42, 100, 100, 200, 300),
            "simultaneous other pointer captured");
        Require(!control.Begin(SpatialTouchZone.HeightDistance, 41, 100, 100, 200, 300),
            "one ID may not claim both zones");
        Require(!control.Begin(SpatialTouchZone.Direction, 99, 100, 100, 200, 300),
            "one region cannot be stolen by new finger");
        Require(control.Move(SpatialTouchZone.Direction, 41, 140, 60),
            "direction touch moved");
        Require(Near(control.Snapshot.AzimuthDegrees, 45), "front-right yaw from touch angle");
        Require(control.Move(SpatialTouchZone.HeightDistance, 42, 140, 60),
            "right touch moved concurrently");
        var moving = control.Snapshot;
        Require(moving.ElevationDegrees > 0 && moving.ElevationDegrees <= 45,
            "height is bounded");
        Require(moving.DistanceScale > 1 && moving.DistanceScale <= 3,
            "distance is bounded");
        Require(!control.Move(SpatialTouchZone.Direction, 42, -999, 999),
            "wrong pointer ID cannot spoof active direction");
        Require(!control.Move(SpatialTouchZone.Direction, 41, double.NaN, 0),
            "nonfinite input rejected");
        Require(control.End(SpatialTouchZone.Direction, 41),
            "direction pointer canceled");
        Require(!control.Snapshot.DirectionDragging && control.Snapshot.HeightDistanceDragging,
            "right contact survives cancel of left");
        Require(!control.Move(SpatialTouchZone.Direction, 41, 0, 0),
            "released pointer cannot mutate azimuth");
        Require(control.End(SpatialTouchZone.HeightDistance, 42),
            "right pointer released");
        Require(!control.Move(SpatialTouchZone.HeightDistance, 42, 0, 0),
            "stale right ID ignored");
        var stable = control.Snapshot;
        Require(Near(stable.AzimuthDegrees, 45) &&
            Near(stable.ElevationDegrees, moving.ElevationDegrees),
            "input release does not invent a new sound coordinate");
        control.CancelAll();
        Require(control.Snapshot == stable, "canceling empty pointers is idempotent");
        control.Center();
        Require(control.Snapshot is {AzimuthDegrees: 0, ElevationDegrees: 0, DistanceScale: 1},
            "user center returns to reference");
        Require(!control.Begin(SpatialTouchZone.Direction, 1, 0, 0, 0, 100),
            "zero dimension rejected");
        Require(!control.Begin(SpatialTouchZone.Direction, 1, 0, 0, double.PositiveInfinity, 100),
            "nonfinite dimensions rejected");
        Require(control.Begin(SpatialTouchZone.Direction, 1, 50, 50, 200, 200),
            "fresh pointer after release");
        var beforeDeadzone = control.Snapshot;
        Require(!control.Move(SpatialTouchZone.Direction, 1, 51, 51),
            "bounded deadzone suppresses spurious motion");
        Require(control.Snapshot == beforeDeadzone, "deadzone does not increment revision");
        control.CancelAll();
        Require(!control.Snapshot.DirectionDragging &&
            !control.Snapshot.HeightDistanceDragging, "orientation/blur cancellation neutralizes input");
        Require(control.Begin(SpatialTouchZone.HeightDistance, 99, 0, 0, 250, 400),
            "fresh touch after orientation cancellation");
        Require(control.Move(SpatialTouchZone.HeightDistance, 99, 999, -999),
            "extreme motion clamped without arithmetic failure");
        Require(control.Snapshot.ElevationDegrees <= 45 &&
            control.Snapshot.DistanceScale <= 3 &&
            double.IsFinite(control.Snapshot.DistanceScale),
            "no unbounded output");
        // Deterministic, CPU-only adverse sampling. No native AudioTrack exists here.
        for (var i = 0; i < 2048; i++)
        {
            var probe = new SpatialTouchIntentProjector();
            Require(probe.Begin(SpatialTouchZone.Direction, i, 20, 20, 200, 200),
                "seeded begin " + i);
            double dx = ((i * 37) % 701) - 350;
            double dy = ((i * 71) % 701) - 350;
            _ = probe.Move(SpatialTouchZone.Direction, i, 20 + dx, 20 + dy);
            Require(double.IsFinite(probe.Snapshot.AzimuthDegrees) &&
                Math.Abs(probe.Snapshot.AzimuthDegrees) <= 180,
                "sample yaw bound " + i);
            probe.CancelAll();
            Require(!probe.Snapshot.DirectionDragging, "sample release " + i);
        }
        Console.WriteLine("PASS native-ready C# touch intent oracle: " + _cases +
            " independent assertions; zero audio/device/start effects");
    }
}
