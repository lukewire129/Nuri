namespace Nuri.UI.Values
{
    /// <summary>
    /// How children are distributed along the main axis of a row or column layout. Used with <c>JustifyContent</c>.
    /// </summary>
    public enum ContentDistribution
    {
        /// <summary>Pack children at the start.</summary>
        Start,
        /// <summary>Center children.</summary>
        Center,
        /// <summary>Pack children at the end.</summary>
        End,
        /// <summary>Distribute with space between children.</summary>
        SpaceBetween,
        /// <summary>Distribute with space around children.</summary>
        SpaceAround,
        /// <summary>Distribute with even space between and around children.</summary>
        SpaceEvenly
    }
}
