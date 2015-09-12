using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading.Tasks;
using YouInteract.YouInteractAPI;
using System.Windows.Threading;
using Microsoft.Kinect.Toolkit.Controls;

namespace You_AirPaint
{
    public static class HoverTimer
    {
        private static DispatcherTimer timer = new DispatcherTimer();
        private static int i = 0;
        private static Button activeButton;
        private static bool flag = false;

        public static void startTimer(Button b){
            if (!flag)
            {
                flag = true;
                timer.Interval = new TimeSpan(0, 0, 0, 0, 333);
            }
            timer.Tick += timer_Tick;
            activeButton = b;
            timer.Start();

        }

        public static void handLeft(Button b)
        {
            i = 0;
            activeButton = null;
            timer.Stop();


        }

        static void timer_Tick(object sender, EventArgs e)
        {
            i++;
            ButtonTick(i);

            if(i == 6){
                ButtonHoverClick(activeButton);
                timer.Stop();
            }

        }


        public delegate void EventHandler(Button e);
        public delegate void NewEventHandler(int i);

        public static event EventHandler ButtonHoverClick;
        public static event NewEventHandler ButtonTick;

    }
}
