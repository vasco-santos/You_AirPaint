using System;

namespace You_AirPaint.YouPaint
{
    /// <summary>
    /// Represents the different kinds of 'brushes' available to the user
    /// </summary>
    public enum KinectPaintTools
    {
        Brush,
        Pen,
        Airbrush,
        Eraser
    }

    /// <summary>
    /// Contains information about a particular kind of brush
    /// </summary>
    public class YouBrush
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="brush">The type of brush</param>
        public YouBrush(KinectPaintTools brush)
        {
            Brush = brush;
        }

        #region Properties

        /// <summary>
        /// URI of the icon representing the brush
        /// </summary>
        public Uri Icon { get; private set; }

        /// <summary>
        /// URI of the icon representing the brush when the tool is selected
        /// </summary>
        public Uri IconSelected { get; private set; }

        /// <summary>
        /// The type of brush
        /// </summary>
        public KinectPaintTools Brush { get; private set; }

        /// <summary>
        /// The user-friendly name of the brush
        /// </summary>
        public string FriendlyName { get; private set; }

        #endregion
    }
}
