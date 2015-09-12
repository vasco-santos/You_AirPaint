using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace You_AirPaint.YouPaint
{
    public static class Tool
    {
        private static KinectPaintTools activeTool;

        public static void SetActiveTool(KinectPaintTools active)
        {
            activeTool = active;
        }

        public static KinectPaintTools GetActiveTool()
        {
            return activeTool;
        }
    }
}
