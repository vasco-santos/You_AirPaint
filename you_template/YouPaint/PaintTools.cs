using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;


namespace You_AirPaint.YouPaint
{
    /// <summary>
    /// Contains helper methods used when painting the image.
    /// </summary>
    public static class PaintTools
    {
        #region Static Fields

        const int AirbrushDots = 32;
        const int AirbrushRadiu = 9;
        private static byte[][] AirbrushBytes;

        #endregion

        #region Definitions

        /// <summary>
        /// Represents a bounding box, used here for determining which pixels to consider when processing line segments
        /// </summary>
        private struct BoundingBox
        {
            #region Properties

            /// <summary>
            /// Gets whether the bounding box is bounding any content
            /// </summary>
            public bool HasContent { get; private set; }

            /// <summary>
            /// The left edge of the bounding box
            /// </summary>
            public int Left { get; private set; }

            /// <summary>
            /// The right edge of the bounding box
            /// </summary>
            public int Right { get; private set; }

            /// <summary>
            /// The top edge of the bounding box
            /// </summary>
            public int Top { get; private set; }

            /// <summary>
            /// The bottom edge of the bounding box
            /// </summary>
            public int Bottom { get; private set; }

            /// <summary>
            /// The width of the bounding box
            /// </summary>
            public int Width { get; private set; }

            /// <summary>
            /// The height of the bounding box
            /// </summary>
            public int Height { get; private set; }


            #endregion

            #region Methods

            /// <summary>
            /// Adds a point that the bounding box must contain
            /// </summary>
            /// <param name="x">X coordinate of the point</param>
            /// <param name="y">Y coordinate of the point</param>
            /// <param name="radius">Radius of the point</param>
            public void AddPoint(int x, int y, int radius)
            {
                if (!HasContent)
                {
                    Left = x - radius;
                    Right = x + radius + 1;
                    Top = y - radius;
                    Bottom = y + radius + 1;
                    Width = Height = 2 * radius;
                    HasContent = true;
                }
                else
                {
                    if (x - radius < Left)
                        Left = x - radius;
                    if (x + radius + 1 > Right)
                        Right = x + radius + 1;
                    if (y - radius < Top)
                        Top = y - radius;
                    if (y + radius + 1 > Bottom)
                        Bottom = y + radius + 1;
                    Width = Right - Left;
                    Height = Bottom - Top;
                }
            }

            /// <summary>
            /// Clips this bounding box against a larger bounding region
            /// </summary>
            /// <param name="clipregion">The bounding box of the clipping region</param>
            public void Clip(BoundingBox clipregion)
            {
                if (Left < clipregion.Left)
                    Left = clipregion.Left;
                if (Left > clipregion.Right)
                    Left = clipregion.Right;
                if (Right < clipregion.Left)
                    Right = clipregion.Left;
                if (Right > clipregion.Right)
                    Right = clipregion.Right;
                if (Top < clipregion.Top)
                    Top = clipregion.Top;
                if (Top > clipregion.Bottom)
                    Top = clipregion.Bottom;
                if (Bottom < clipregion.Top)
                    Bottom = clipregion.Top;
                if (Bottom > clipregion.Bottom)
                    Bottom = clipregion.Bottom;
                Width = Right - Left;
                Height = Bottom - Top;
            }

            #endregion
        }

        /// <summary>
        /// Represents a line segment, and optimizes the calculation of its nearest distance to multiple points
        /// </summary>
        private struct MyLine
        {
            #region Data

            int fromx, fromy, tox, toy;     // Stores the 'to' and 'from' points as integers

            int bx, by, bxx, bxy, byy, L2;  // Stores intermediate calculations, so they aren't needlessly repeated

            #endregion

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="_from">The starting point of the line segment</param>
            /// <param name="_to">The ending point of the line segment</param>
            public MyLine(Point _from, Point _to)
            {
                fromx = (int)_from.X;
                fromy = (int)_from.Y;
                tox = (int)_to.X;
                toy = (int)_to.Y;
                bx = tox - fromx;
                by = toy - fromy;
                bxx = bx * bx;
                bxy = bx * by;
                byy = by * by;
                L2 = bxx + byy;
            }

            #region Methods

            /// <summary>
            /// Returns the squared distance between the line segment and the point represented by x,y
            /// </summary>
            /// <param name="x">X coordinate of the point</param>
            /// <param name="y">Y coordinate of the point</param>
            /// <returns></returns>
            public int DistanceSquared(int x, int y)
            {
                int nx, ny;

                int ay = y - fromy;
                int ax = x - fromx;

                // Compute the point along the line nearest to the current pixel (represented by nx,ny)                    
                if (L2 != 0)
                {
                    nx = (bxx * ax + bxy * ay) / L2;
                    ny = (bxy * ax + byy * ay) / L2;

                    // If the nearest point lies beyond the 'to' point, snap n to 'to'
                    if (nx * nx + ny * ny > L2)
                    {
                        nx = bx;
                        ny = by;
                    }
                    else
                    {
                        int bnx = bx - nx;
                        int bny = by - ny;

                        // If the nearest point lies before the 'from' point, snap n to 'from'
                        if (bnx * bnx + bny * bny > L2)
                            nx = ny = 0;
                    }
                }
                else
                    nx = ny = 0;

                // Return the squared distance between n and the given point
                int dx = nx - ax;
                int dy = ny - ay;

                return dx * dx + dy * dy;
            }

            /// <summary>
            /// Interpolates along the line segment
            /// </summary>
            /// <param name="i">The step index to interpolate to</param>
            /// <param name="steps">The number of steps</param>
            /// <param name="x">The returned x coordinate</param>
            /// <param name="y">The returned y coordinate</param>
            public void Interpolate(int i, int steps, out int x, out int y)
            {
                x = fromx + bx * i / steps;
                y = fromy + by * i / steps;
            }

            #endregion
        }

        #endregion

        #region Constructor

        static PaintTools()
        {
            AirbrushBytes = new byte[2 * AirbrushRadiu + 1][];

            for (int i = 0; i < 2 * AirbrushRadiu + 1; i++)
                AirbrushBytes[i] = new byte[2 * AirbrushRadiu + 1];

            int x, y;
            for (int i = 0; i < AirbrushBytes.Length; i++)
            {
                x = i - AirbrushRadiu;
                for (int j = 0; j < AirbrushBytes.Length; j++)
                {
                    y = j - AirbrushRadiu;
                    AirbrushBytes[i][j] = (byte)(128 - (int)(128.0 * Math.Min(1, Math.Sqrt(x * x + y * y) / (double)AirbrushRadiu)));
                }
            }
        }

        #endregion

        /// <summary>
        /// Erases paint on a WriteableBitmap
        /// </summary>
        /// <param name="bmp">The bitmap to modify</param>
        /// <param name="from">The starting point of the stroke</param>
        /// <param name="to">The end point of the stroke</param>
        /// <param name="size">The stroke size</param>
        public static unsafe void Erase(WriteableBitmap bmp, Point from, Point to, int size)
        {
            if (bmp == null) return;

            bmp.Lock();

            // Intermediate storage of the square of the size
            int area = size * size;

            // Create a line segment representation to compare distance to
            MyLine line = new MyLine(from, to);

            // Get a bounding box for the line segment
            BoundingBox bitmapbounds = new BoundingBox();
            BoundingBox linebounds = new BoundingBox();

            bitmapbounds.AddPoint(0, 0, 0);
            bitmapbounds.AddPoint(bmp.PixelWidth - 1, bmp.PixelHeight - 1, 0);

            linebounds.AddPoint((int)from.X, (int)from.Y, size);
            linebounds.AddPoint((int)to.X, (int)to.Y, size);
            linebounds.Clip(bitmapbounds);

            // Get a pointer to the back buffer (we use an int pointer here, since we can safely assume a 32-bit pixel format)
            Int32* start = (Int32*)bmp.BackBuffer.ToPointer();

            // Move the starting pixel to the x offset
            start += linebounds.Left;

            // Loop through the relevant portion of the image and figure out which pixels need to be erased
            for (int y = linebounds.Top; y < linebounds.Bottom; y++)
            {
                Int32* pixel = start + bmp.BackBufferStride / sizeof(Int32) * y;

                for (int x = linebounds.Left; x < linebounds.Right; x++)
                {
                    if (line.DistanceSquared(x, y) <= area)
                        *pixel = 0;

                    // Move to the next pixel
                    pixel++;
                }
            }

            bmp.AddDirtyRect(new Int32Rect(linebounds.Left, linebounds.Top, linebounds.Width, linebounds.Height));
            bmp.Unlock();
        }

        /// <summary>
        /// Paints on a WriteableBitmap like a paintbrush
        /// </summary>
        /// <param name="bmp">The bitmap to modify</param>
        /// <param name="from">The starting point of the stroke</param>
        /// <param name="to">The end point of the stroke</param>
        /// <param name="previous">The point prior to the 'from' point, or null</param>
        /// <param name="color">The color of the brush</param>
        /// <param name="size">The stroke size</param>
        public static unsafe void Brush(WriteableBitmap bmp, Point from, Point to, Point? previous, Color color, int size)
        {
            if (bmp == null) return;

            bmp.Lock();

            // Intermediate storage of the square of the size
            int area = size * size;
            uint flatcolor = (uint)((int)color.A << 24) + (uint)((int)color.R << 16) + (uint)((int)color.G << 8) + color.B;

            // Create a line segment representation to compare distance to
            MyLine line = new MyLine(from, to);

            // Get a bounding box for the line segment
            BoundingBox bitmapbounds = new BoundingBox();
            BoundingBox linebounds = new BoundingBox();

            bitmapbounds.AddPoint(0, 0, 0);
            bitmapbounds.AddPoint(bmp.PixelWidth - 1, bmp.PixelHeight - 1, 0);

            linebounds.AddPoint((int)from.X, (int)from.Y, size);
            linebounds.AddPoint((int)to.X, (int)to.Y, size);
            linebounds.Clip(bitmapbounds);

            // Get a pointer to the back buffer (we use an int pointer here, since we can safely assume a 32-bit pixel format)
            UInt32* start = (UInt32*)bmp.BackBuffer.ToPointer();

            // Move the starting pixel to the x offset
            start += linebounds.Left;

            if (previous.HasValue)
            {
                MyLine previoussegment = new MyLine(previous.Value, from);

                // Loop through the relevant portion of the image and figure out which pixels need to be erased
                for (int y = linebounds.Top; y < linebounds.Bottom; y++)
                {
                    UInt32* pixel = start + bmp.BackBufferStride / sizeof(UInt32) * y;

                    for (int x = linebounds.Left; x < linebounds.Right; x++)
                    {
                        if (line.DistanceSquared(x, y) <= area && previoussegment.DistanceSquared(x, y) > area)
                        {
                            if (color.A == 255)
                                *pixel = flatcolor;
                            else
                                AlphaBlended(pixel, color);
                        }

                        // Move to the next pixel
                        pixel++;
                    }
                }
            }
            else
            {
                // Loop through the relevant portion of the image and figure out which pixels need to be erased
                for (int y = linebounds.Top; y < linebounds.Bottom; y++)
                {
                    UInt32* pixel = start + bmp.BackBufferStride / sizeof(UInt32) * y;

                    for (int x = linebounds.Left; x < linebounds.Right; x++)
                    {
                        if (line.DistanceSquared(x, y) <= area)
                        {
                            if (color.A == 255)
                                *pixel = flatcolor;
                            else
                                AlphaBlended(pixel, color);
                        }

                        // Move to the next pixel
                        pixel++;
                    }
                }
            }

            bmp.AddDirtyRect(new Int32Rect(linebounds.Left, linebounds.Top, linebounds.Width, linebounds.Height));
            bmp.Unlock();
        }

        /// <summary>
        /// Paints on a WriteableBitmap with a stylized airbrush
        /// </summary>
        /// <param name="bmp">The bitmap to modify</param>
        /// <param name="from">The starting point of the stroke</param>
        /// <param name="to">The end point of the stroke</param>
        /// <param name="color">The color of the stroke</param>
        /// <param name="size">The size of the stroke</param>
        public static unsafe void Airbrush(WriteableBitmap bmp, Point from, Point to, Color color, int size)
        {
            Random r = new Random();

            if (bmp == null) return;

            bmp.Lock();

            // Create a line segment representation
            MyLine line = new MyLine(from, to);

            // Get a bounding box for the painted area
            BoundingBox bitmapbounds = new BoundingBox();
            BoundingBox linebounds = new BoundingBox();

            bitmapbounds.AddPoint(0, 0, 0);
            bitmapbounds.AddPoint(bmp.PixelWidth - 1, bmp.PixelHeight - 1, 0);

            linebounds.AddPoint((int)from.X, (int)from.Y, size + AirbrushRadiu);
            linebounds.AddPoint((int)to.X, (int)to.Y, size + AirbrushRadiu);
            linebounds.Clip(bitmapbounds);

            UInt32* start = (UInt32*)bmp.BackBuffer.ToPointer();
            int stride = bmp.BackBufferStride / sizeof(UInt32);
            // Move from 'from' to 'to' along timestep intervals, with one dot painted per interval
            for (int i = 0; i < AirbrushDots; i++)
            {
                int x, y;
                line.Interpolate(i, AirbrushDots, out x, out y);

                int dist = r.Next() % size;
                double angle = r.NextDouble() * 2 * Math.PI;

                double dx = Math.Cos(angle) * dist;
                double dy = Math.Sqrt(dist * dist - dx * dx);
                if (angle > Math.PI) dy = -dy;

                int bx = x + (int)dx;
                int by = y + (int)dy;

                BoundingBox dotbounds = new BoundingBox();

                dotbounds.AddPoint(bx, by, AirbrushRadiu);
                dotbounds.Clip(bitmapbounds);

                for (int k = dotbounds.Top, row = 0; k < dotbounds.Bottom; k++, y++, row++)
                    for (int j = dotbounds.Left, col = 0; j < dotbounds.Right; j++, col++)
                        AlphaBlended(start + stride * k + j, Color.FromArgb(AirbrushBytes[row][col], color.R, color.G, color.B));
            }

            bmp.AddDirtyRect(new Int32Rect(linebounds.Left, linebounds.Top, linebounds.Width, linebounds.Height));
            bmp.Unlock();
        }

        // Alpha blends a color with its destination pixel using the standard formula
        private static unsafe void AlphaBlended(UInt32* pixel, Color c)
        {
            byte* component = (byte*)pixel;
            ushort alpha = (ushort)c.A;
            component[3] += (byte)(((ushort)(255 - component[3]) * alpha) / 255);
            component[2] = (byte)(((ushort)component[2] * (255 - alpha) + (ushort)c.R * alpha) / 255);
            component[1] = (byte)(((ushort)component[1] * (255 - alpha) + (ushort)c.G * alpha) / 255);
            component[0] = (byte)(((ushort)component[0] * (255 - alpha) + (ushort)c.B * alpha) / 255);
        }
    }
}
