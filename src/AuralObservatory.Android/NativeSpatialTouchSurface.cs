using System;
using Android.Content;
using Android.Graphics;
using Android.Views;
using AuralObservatory.Core;

namespace AuralObservatoryMobile;

/// <summary>
/// Native Android, UI-thread-only input/view candidate. No WebView, no
/// AudioTrack, no microphone and no authority to start sound. The coordinates
/// are observations only until separately qualified real spatial output exists.
/// </summary>
public sealed class NativeSpatialTouchSurface : View
{
    private readonly SpatialTouchIntentProjector _intents = new();
    private int? _directionPointer, _heightPointer;
    private PointF _directionOrigin, _directionCurrent;
    private PointF _heightOrigin, _heightCurrent;
    public event Action<SpatialTouchSnapshot>? Changed;
    public SpatialTouchSnapshot Snapshot => _intents.Snapshot;

    public NativeSpatialTouchSurface(Context context) : base(context)
    {
        Focusable = true;
        ContentDescription = "Input-only spatial touch demonstration. Left side controls direction, right side height and relative distance. An accessible STOP button is above the pads.";
        ImportantForAccessibility = Android.Views.ImportantForAccessibility.Yes;
    }

    public void CancelInput()
    {
        _directionPointer = null;
        _heightPointer = null;
        _intents.CancelAll();
        Changed?.Invoke(_intents.Snapshot);
        Invalidate();
    }

    public void Center()
    {
        CancelInput();
        _intents.Center();
        Changed?.Invoke(_intents.Snapshot);
        Invalidate();
    }

    protected override void OnDraw(Canvas? canvas)
    {
        base.OnDraw(canvas);
        if (canvas is null) return;
        float middle = Width * .5f;
        float density = Context.Resources?.DisplayMetrics?.Density ?? 1f;
        using var bg = new Paint(PaintFlags.AntiAlias) { Color = Color.Rgb(245, 247, 250) };
        using var stroke = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color.Rgb(40, 69, 98), StrokeWidth = 2f * density
        };
        stroke.SetStyle(Paint.Style.Stroke);
        using var label = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color.Rgb(28, 49, 73), TextSize = 14f * density
        };
        using var knob = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color.Rgb(219, 106, 18)
        };
        canvas.DrawRect(0, 0, Width, Height, bg);
        canvas.DrawLine(middle, 0, middle, Height, stroke);
        canvas.DrawText("LEFT: direction", 10f * density, 24f * density, label);
        canvas.DrawText("RIGHT: height / distance", middle + 10f * density, 24f * density, label);
        // Use exactly the reducer's view-relative radius for honest UI feedback.
        float radius = Math.Clamp(Math.Min(Width * .5f, Height) * .32f, 40f, 76f);
        DrawPad(canvas, stroke, knob, _directionPointer, _directionOrigin, _directionCurrent, radius, density);
        DrawPad(canvas, stroke, knob, _heightPointer, _heightOrigin, _heightCurrent, radius, density);
    }

    private static void DrawPad(Canvas canvas, Paint outline, Paint dot, int? pointer,
        PointF origin, PointF current, float radius, float density)
    {
        if (!pointer.HasValue) return;
        canvas.DrawCircle(origin.X, origin.Y, radius, outline);
        canvas.DrawCircle(current.X, current.Y, Math.Min(20f * density, radius * .35f), dot);
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
            case MotionEventActions.PointerDown:
            {
                int index = e.ActionIndex;
                int id = e.GetPointerId(index);
                float x = e.GetX(index), y = e.GetY(index);
                SpatialTouchZone zone = x < Width / 2f ?
                    SpatialTouchZone.Direction : SpatialTouchZone.HeightDistance;
                bool accepted = _intents.Begin(zone, id, x, y, Width / 2d, Height);
                if (accepted)
                {
                    Parent?.RequestDisallowInterceptTouchEvent(true);
                    if (zone == SpatialTouchZone.Direction)
                    {
                        _directionPointer = id;
                        _directionOrigin = _directionCurrent = new PointF(x, y);
                    }
                    else
                    {
                        _heightPointer = id;
                        _heightOrigin = _heightCurrent = new PointF(x, y);
                    }
                    Changed?.Invoke(_intents.Snapshot);
                    Invalidate();
                }
                return true;
            }
            case MotionEventActions.Move:
                for (int index = 0; index < e.PointerCount; index++)
                {
                    int id = e.GetPointerId(index);
                    float x = e.GetX(index), y = e.GetY(index);
                    if (_directionPointer == id)
                    {
                        _directionCurrent = new PointF(x, y);
                        _intents.Move(SpatialTouchZone.Direction, id, x, y);
                    }
                    else if (_heightPointer == id)
                    {
                        _heightCurrent = new PointF(x, y);
                        _intents.Move(SpatialTouchZone.HeightDistance, id, x, y);
                    }
                }
                Changed?.Invoke(_intents.Snapshot);
                Invalidate();
                return true;
            case MotionEventActions.Up:
            case MotionEventActions.PointerUp:
            {
                int id = e.GetPointerId(e.ActionIndex);
                if (_directionPointer == id)
                {
                    _intents.End(SpatialTouchZone.Direction, id);
                    _directionPointer = null;
                }
                if (_heightPointer == id)
                {
                    _intents.End(SpatialTouchZone.HeightDistance, id);
                    _heightPointer = null;
                }
                Changed?.Invoke(_intents.Snapshot);
                Invalidate();
                return true;
            }
            case MotionEventActions.Cancel:
                CancelInput();
                return true;
            default:
                return true;
        }
    }
}
