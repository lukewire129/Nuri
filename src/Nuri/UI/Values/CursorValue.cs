using System;

namespace Nuri.UI.Values
{
    /// <summary>
    /// The cursor shown when hovering an element.
    /// </summary>
    public readonly struct CursorValue : IEquatable<CursorValue>
    {
        /// <summary>
        /// Creates a cursor value.
        /// </summary>
        /// <param name="kind">The cursor kind.</param>
        public CursorValue(CursorKind kind)
        {
            Kind = kind;
        }

        /// <summary>Gets the cursor kind.</summary>
        public CursorKind Kind { get; }

        /// <summary>Standard arrow cursor.</summary>
        public static CursorValue Arrow => new CursorValue(CursorKind.Arrow);

        /// <summary>Hand (link) cursor.</summary>
        public static CursorValue Hand => new CursorValue(CursorKind.Hand);

        /// <summary>Text (I-beam) cursor.</summary>
        public static CursorValue IBeam => new CursorValue(CursorKind.IBeam);

        /// <summary>Wait (busy) cursor.</summary>
        public static CursorValue Wait => new CursorValue(CursorKind.Wait);

        /// <summary>Crosshair cursor.</summary>
        public static CursorValue Cross => new CursorValue(CursorKind.Cross);

        /// <summary>Help cursor.</summary>
        public static CursorValue Help => new CursorValue(CursorKind.Help);

        /// <summary>No-action cursor.</summary>
        public static CursorValue No => new CursorValue(CursorKind.No);

        /// <summary>All-direction resize cursor.</summary>
        public static CursorValue SizeAll => new CursorValue(CursorKind.SizeAll);

        /// <summary>Determines whether this cursor equals another.</summary>
        public bool Equals(CursorValue other)
        {
            return Kind == other.Kind;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is CursorValue other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Kind;
        }
    }

    /// <summary>
    /// The available cursor kinds.
    /// </summary>
    public enum CursorKind
    {
        /// <summary>No cursor.</summary>
        None,
        /// <summary>Standard arrow.</summary>
        Arrow,
        /// <summary>App starting.</summary>
        AppStarting,
        /// <summary>Crosshair.</summary>
        Cross,
        /// <summary>Hand (link).</summary>
        Hand,
        /// <summary>Help.</summary>
        Help,
        /// <summary>Text (I-beam).</summary>
        IBeam,
        /// <summary>No action.</summary>
        No,
        /// <summary>Pen.</summary>
        Pen,
        /// <summary>All-direction scroll.</summary>
        ScrollAll,
        /// <summary>East scroll.</summary>
        ScrollE,
        /// <summary>North scroll.</summary>
        ScrollN,
        /// <summary>North-east scroll.</summary>
        ScrollNE,
        /// <summary>North-south scroll.</summary>
        ScrollNS,
        /// <summary>North-west scroll.</summary>
        ScrollNW,
        /// <summary>South scroll.</summary>
        ScrollS,
        /// <summary>South-east scroll.</summary>
        ScrollSE,
        /// <summary>South-west scroll.</summary>
        ScrollSW,
        /// <summary>West scroll.</summary>
        ScrollW,
        /// <summary>West-east scroll.</summary>
        ScrollWE,
        /// <summary>All-direction resize.</summary>
        SizeAll,
        /// <summary>North-east/south-west resize.</summary>
        SizeNESW,
        /// <summary>North-south resize.</summary>
        SizeNS,
        /// <summary>North-west/south-east resize.</summary>
        SizeNWSE,
        /// <summary>West-east resize.</summary>
        SizeWE,
        /// <summary>Up arrow.</summary>
        UpArrow,
        /// <summary>Wait (busy).</summary>
        Wait
    }
}
