using System;

namespace Nuri.UI.Values
{
    public readonly struct CursorValue : IEquatable<CursorValue>
    {
        public CursorValue(CursorKind kind)
        {
            Kind = kind;
        }

        public CursorKind Kind { get; }

        public static CursorValue Arrow => new CursorValue(CursorKind.Arrow);

        public static CursorValue Hand => new CursorValue(CursorKind.Hand);

        public static CursorValue IBeam => new CursorValue(CursorKind.IBeam);

        public static CursorValue Wait => new CursorValue(CursorKind.Wait);

        public static CursorValue Cross => new CursorValue(CursorKind.Cross);

        public static CursorValue Help => new CursorValue(CursorKind.Help);

        public static CursorValue No => new CursorValue(CursorKind.No);

        public static CursorValue SizeAll => new CursorValue(CursorKind.SizeAll);

        public bool Equals(CursorValue other)
        {
            return Kind == other.Kind;
        }

        public override bool Equals(object? obj)
        {
            return obj is CursorValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Kind;
        }
    }

    public enum CursorKind
    {
        None,
        Arrow,
        AppStarting,
        Cross,
        Hand,
        Help,
        IBeam,
        No,
        Pen,
        ScrollAll,
        ScrollE,
        ScrollN,
        ScrollNE,
        ScrollNS,
        ScrollNW,
        ScrollS,
        ScrollSE,
        ScrollSW,
        ScrollW,
        ScrollWE,
        SizeAll,
        SizeNESW,
        SizeNS,
        SizeNWSE,
        SizeWE,
        UpArrow,
        Wait
    }
}
