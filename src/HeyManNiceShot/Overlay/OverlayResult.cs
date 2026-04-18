using HeyManNiceShot.Geometry;

namespace HeyManNiceShot.Overlay;

public enum OverlayAction { Copy, Save, Edit }

public sealed record OverlayResult(PhysicalRect Selection, OverlayAction Action);
