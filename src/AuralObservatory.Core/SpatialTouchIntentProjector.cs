using System;

namespace AuralObservatory.Core;

/// <summary>
/// Platform-neutral input-only projection for a native Android/iPad touch
/// view. Never starts, stops or modulates physical audio. Caller owns native
/// user presence, permission, lifecycle and any separately approved renderer.
/// </summary>
public enum SpatialTouchZone { Direction = 0, HeightDistance = 1 }

public readonly record struct SpatialTouchSnapshot(
    double AzimuthDegrees,
    double ElevationDegrees,
    double DistanceScale,
    bool DirectionDragging,
    bool HeightDistanceDragging,
    long Revision);

public sealed class SpatialTouchIntentProjector
{
    private sealed class Contact
    {
        public long? PointerId;
        public double OriginX, OriginY, Radius;
    }

    private readonly Contact _direction = new();
    private readonly Contact _heightDistance = new();
    private double _azimuth;
    private double _elevation;
    private double _distance = 1.0;
    private long _revision;

    public SpatialTouchSnapshot Snapshot => new(
        _azimuth, _elevation, _distance,
        _direction.PointerId.HasValue, _heightDistance.PointerId.HasValue,
        _revision);

    /// <summary>Captures exactly one pointer per touch region.</summary>
    public bool Begin(SpatialTouchZone zone, long pointerId,
        double x, double y, double areaWidth, double areaHeight)
    {
        if (!Valid(x) || !Valid(y) || !Valid(areaWidth) || !Valid(areaHeight) ||
            areaWidth <= 0 || areaHeight <= 0) return false;
        Contact contact = Select(zone);
        if (contact.PointerId.HasValue ||
            _direction.PointerId == pointerId ||
            _heightDistance.PointerId == pointerId) return false;
        contact.PointerId = pointerId;
        contact.OriginX = x;
        contact.OriginY = y;
        contact.Radius = Math.Clamp(Math.Min(areaWidth, areaHeight) * .32, 40, 76);
        _revision++;
        return true;
    }

    /// <summary>
    /// Relative motion uses floating origins and an explicit radial deadzone.
    /// This is head-relative position metadata, not a calibrated HRTF.
    /// </summary>
    public bool Move(SpatialTouchZone zone, long pointerId, double x, double y)
    {
        Contact contact = Select(zone);
        if (contact.PointerId != pointerId || !Valid(x) || !Valid(y)) return false;
        double dx = x - contact.OriginX, dy = y - contact.OriginY;
        double magnitude = Math.Sqrt(dx * dx + dy * dy);
        double radius = contact.Radius;
        if (magnitude < radius * .14) return false;
        double factor = magnitude > radius ? radius / magnitude : 1;
        double bx = dx * factor, by = dy * factor;
        if (zone == SpatialTouchZone.Direction)
        {
            // Forward is negative Y. Left negative azimuth, right positive.
            _azimuth = Math.Atan2(dx, -dy) * 180.0 / Math.PI;
        }
        else
        {
            _elevation = Math.Clamp(-by / radius * 45.0, -45, 45);
            _distance = Math.Clamp(1.0 + Math.Max(0, bx / radius) * 2.0, 1, 3);
        }
        _revision++;
        return true;
    }

    /// <summary>
    /// Release/cancel remove input capture. The last chosen spatial position
    /// remains stable until explicit user re-centering, unlike game thrust.
    /// </summary>
    public bool End(SpatialTouchZone zone, long pointerId)
    {
        Contact contact = Select(zone);
        if (contact.PointerId != pointerId) return false;
        contact.PointerId = null;
        _revision++;
        return true;
    }

    public void CancelAll()
    {
        if (!_direction.PointerId.HasValue && !_heightDistance.PointerId.HasValue)
            return;
        _direction.PointerId = null;
        _heightDistance.PointerId = null;
        _revision++;
    }

    public void Center()
    {
        _azimuth = 0;
        _elevation = 0;
        _distance = 1;
        _revision++;
    }

    private static bool Valid(double value) => double.IsFinite(value);

    private Contact Select(SpatialTouchZone zone) => zone switch
    {
        SpatialTouchZone.Direction => _direction,
        SpatialTouchZone.HeightDistance => _heightDistance,
        _ => throw new ArgumentOutOfRangeException(nameof(zone))
    };
}
