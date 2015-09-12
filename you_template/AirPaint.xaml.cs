using System;
using System.ComponentModel;
using System.Linq;
using System.Resources;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Microsoft.Kinect.Toolkit.Controls;
using Microsoft.Kinect;
using YouInteract.YouBasic;
using YouInteract.YouInteractAPI;
using YouInteract.YouPlugin_Developing;
using You_AirPaint.YouPaint;
using Microsoft.Kinect.Toolkit.Interaction;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.Kinect.Toolkit;
using System.IO;

namespace You_AirPaint
{
    /// <summary>
    /// Interaction logic for Template.xaml
    /// </summary>
    public partial class AirPaint : Page, YouPlugin
    {
        // attributes
        private double w, h;
        private int i;
        private bool active = false;
        private short hoverScroller;
        private bool grip;
        private bool help;
        private bool camera;
        private Point lastPosition;
        private string activeTool;

        private DispatcherTimer timer;

        # region Constructor
        /// <summary>
        /// AirPaint Constructor
        /// </summary>
        public AirPaint()
        {
            InitializeComponent();

            help = true;
            grip = false;
            camera = false;
            activeTool = "lata";
            MyBrush = availableBrushes[1];
            MyColor = availableColors[1];
            MySize = availableSizes[3];
            Mail.IsEnabled = false;

            setWindow();
            HoverTimer.ButtonHoverClick += HoverTimer_ButtonHoverClick;
            this.Loaded += AirPaint_Loaded;
            this.Unloaded += AirPaint_Unloaded;
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += timer_Tick;
            hoverScroller = 0;
            KinectApi.setColor(true);
            KinectVideo.Visibility = Visibility.Hidden;
           
        }

        void AirPaint_Unloaded(object sender, RoutedEventArgs e)
        {
            KinectApi.ColorStreamEvent -= KinectApi_ColorStreamEvent;
            KinectApi.setColor(false);

            KinectApi.InteractionEvent -= KinectApi_InteractionEvent;
        }
        # endregion

        # region Events

        /// <summary>
        /// AirPaint_Loaded Event
        /// </summary>
        void AirPaint_Loaded(object sender, RoutedEventArgs e)
        {
            KinectApi.setColor(true);
            KinectApi.ColorStreamEvent += KinectApi_ColorStreamEvent;
            KinectApi.InteractionEvent += KinectApi_InteractionEvent;
            reset();
            active = true;
        }

        /// <summary>
        /// KinectApi_ColorStreamEvent Event
        /// </summary>
        void KinectApi_ColorStreamEvent(BitmapSource e)
        {
            Console.WriteLine("Color Stream - Air Paint");
            KinectVideo.Source = e;
        }
        
        /// <summary>
        /// KinectApi_ColorStreamEvent Leave
        /// </summary>
        void Leaving()
        {
            active = false;
            KinectApi.setColor(false);
        }
        # endregion

        # region Interaction

        /// <summary>
        /// Evento Interaction Stream
        /// </summary>
        
        private void KinectApi_InteractionEvent(InteractionStreamArgs e)
        {
            Console.WriteLine("Interaction Stream - Air Paint");
            if (active)
            {
                bool click = false, press = grip;
                double handx = -1, handy = -1;


                if (e.userinfo.Count() > 0)
                {

                    UserInfo activeuser = (from u in e.userinfo
                                           where u.SkeletonTrackingId > 0
                                           orderby u.SkeletonTrackingId
                                           select u).First();

                    foreach (var h in activeuser.HandPointers)
                    {

                        if (h.IsPrimaryForUser)
                        {
                            handx = h.X;
                            handy = h.Y;

                            if (h.IsPressed || h.HandEventType == InteractionHandEventType.Grip)
                                click = true;

                        }
                        else
                        {
                            if (!grip)
                            {
                                if (h.HandEventType == InteractionHandEventType.Grip)
                                {
                                    press = true;

                                }
                            }
                            else
                            {
                                if (h.HandEventType == InteractionHandEventType.GripRelease)
                                {
                                    press = false;
                                }
                            }

                        }
                    }
                    if (cursorPosition.X < 0.18 * this.w || cursorPosition.X > 0.82 * this.w ||
                        cursorPosition.Y < 0.1 * this.h || cursorPosition.Y > 0.9 * this.h)
                    {
                        press = false;
                    }

                    setPoint(handx, handy, click);

                    if (grip != press)
                    {
                        grip = press;
                        if (grip)
                        {
                            Painting();
                        }
                        else
                            NoPainting();

                    }
                    evento(this, new EventArgs());

                }
            }
        }

        public delegate void EventHandler(object s, EventArgs e);
        public event EventHandler evento = delegate { };

        /// <summary>
        /// Set Point
        /// </summary>

        private void setPoint(double handx, double handy, bool click)
        {
            int cursory = (int)(handy * h - h * 0.05);
            int cursorx = (int)(handx * w - w * 0.05);

            KinectMouseController.KinectMouseMethods.SendMouseInput(cursorx, cursory, (int)w, (int)h, click);
        }

        /// <summary>
        /// Mouse Move Handler 
        /// </summary>

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            PreviousCursorPosition = cursorPosition;
            Point position = e.GetPosition(this);
            double pX = position.X;
            double py = position.Y;
            cursorPosition = new Point(pX, py);

            Canvas.SetLeft(Pointer, pX - Pointer.Width / 2);
            Canvas.SetTop(Pointer, py - Pointer.Height / 2);
        }

        # endregion

        # region Cursor Properties

        /// <summary>
        /// Gets the current cursor position 
        /// </summary>
        public Point CursorPosition
        {
            get { return cursorPosition; }
            private set
            {
                cursorPosition = value;
            }
        }
        private Point cursorPosition;

        /// <summary>
        /// Gets the previous cursor position
        /// </summary>
        public Point PreviousCursorPosition { get; private set; }

        /// <summary>
        /// Gets the position of the cursor
        /// </summary>
        private Point GetPosition(Visual visual)
        {
            return TransformToDescendant(visual).Transform(CursorPosition);
        }

        /// <summary>
        /// Gets the previous position of the cursor
        /// </summary>
        private Point GetPreviousPosition(Visual visual)
        {
            return TransformToDescendant(visual).Transform(PreviousCursorPosition);
        }

        # endregion

        #region Painting

        private void reset()
        {
            help = true;
            grip = false;
            camera = false;

            CreatePaintableImage();
            Restart.Visibility = Visibility.Collapsed;
            start.Visibility = Visibility.Visible;
            // Hide Paint Canvas
            PaintCanvasBorder.Visibility = Visibility.Collapsed;
            PaintCanvas.Visibility = Visibility.Collapsed;
            HelpCanvas.Visibility = Visibility.Visible;

            // Block Buttons
            Mail.IsEnabled = false;
            Cam.IsEnabled = false;
        }

        /// <summary>
        /// Call to hide the UI and begin painting on the canvas
        /// </summary>
        public void Painting()
        {
            if (help)
            {
                return;
            }

            // Draw at the current position and start checking for updates until done
            Point pos = GetPosition(PaintImage);
            //Point pos = Mouse.GetPosition(Application.Current.MainWindow);
            Draw(pos, pos, null);
            lastPosition = pos;
            BitmapImage bmi = new BitmapImage();
            YouWindow.bitmapSource(bmi, "/You_AirPaint;component/Images/crosshair4.png", UriKind.Relative);
            Pointer.Source = bmi;

            evento += Drawing;


        }

        /// <summary>
        /// Call to show the UI and stop painting on the canvas
        /// </summary>
        public void NoPainting()
        {
            // Stop listening for cursor changes
            BitmapImage bmi = new BitmapImage();
            YouWindow.bitmapSource(bmi, "/You_AirPaint;component/Images/crosshair4.png", UriKind.Relative);
            Pointer.Source = bmi;
            evento -= Drawing;
        }

        # endregion

        #region Painting Process


        void Drawing(object sender, EventArgs e)
        {
            Console.WriteLine(GetPosition(PaintImage));
            Point pos = GetPosition(PaintImage);
            Point prev = GetPreviousPosition(PaintImage);
            Draw(prev, pos, lastPosition);
            lastPosition = prev;
        }

        private void CreatePaintableImage()
        {
            MyImage = new WriteableBitmap(
                (int)PaintCanvasBorder.Width,
                (int)PaintCanvasBorder.Height,
                96.0,
                96.0,
                PixelFormats.Pbgra32,
                null);
        }

        // paints on the canvas using the currently selected settings.
        private void Draw(Point from, Point to, Point? past)
        {
            switch (MyBrush.Brush)
            {
                case KinectPaintTools.Eraser:
                    PaintTools.Erase(
                        MyImage,
                        from, to,
                        (int)MySize);
                    break;
                case KinectPaintTools.Pen:
                    PaintTools.Brush(
                        MyImage,
                        from, to, past,
                        Color.FromArgb(128, MyColor.R, MyColor.G, MyColor.B),
                        (int)MySize);
                    break;
                case KinectPaintTools.Airbrush:
                    PaintTools.Airbrush(
                        MyImage,
                        from, to,
                        MyColor,
                        (int)MySize * 2);
                    break;
                case KinectPaintTools.Brush:
                    PaintTools.Brush(
                        MyImage,
                        from, to, past,
                        MyColor,
                        (int)MySize);
                    break;
            }
        }


        // Recreate the paintable image when the size changes, since their sizes need to match
        private void PaintCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CreatePaintableImage();
        }

        #endregion

        #region Interface

        /// <summary>
        /// Set Window
        /// </summary>

        private void setWindow()
        {
            // Get Window Measures
            h = this.Height;
            w = this.Width;

            //Set pointer size
            Pointer.Width = w * 0.1;
            Pointer.Height = h * 0.1;
            // Set Help Canvas
            setHelp();
            // Set Colours Scroller
            setColoursScroller();
            // Set Tools Panel
            setToolsPanel();
            // Set Back Button
            setButtons(MainMenuButton, 0.11, 0.22, 0.015, 0.02);
            // Restart Button
            setButtons(Restart, 0.11, 0.22, 0.75, 0.86);
            // Cam Button
            setButtons(Cam, 0.11, 0.22, 0.75, 0.02);
            // Mail Button
            setButtons(Mail, 0.11, 0.22, 0.015, 0.86);
            // Set Tela

            PaintCanvasBorder.BorderBrush = Brushes.Green;
            PaintCanvasBorder.BorderThickness = new Thickness(h * 0.01);

            PaintCanvasBorder.Width = w * 0.64;
            PaintCanvasBorder.Height = h * 0.8;
            KinectVideo.Height = PaintCanvasBorder.Height - 10;
            KinectVideo.Width = PaintCanvasBorder.Width - 10;
            Canvas.SetTop(PaintCanvasBorder, h * 0.1);
            Canvas.SetLeft(PaintCanvasBorder, w * 0.18);

            // Block Buttons
            Mail.IsEnabled = false;
            Cam.IsEnabled = false;
        }

        # region Layout



        # region Panels

        /// <summary>
        /// Set Help Panel
        /// </summary>
        
        private void setHelp()
        {
            HelpCanvas.Width = w * 0.64;
            HelpCanvas.Height = h * 0.8;
            Canvas.SetTop(HelpCanvas, h * 0.1);
            Canvas.SetLeft(HelpCanvas, w * 0.18);

            // Start Button
            setButtons(start, 0.2, 0.3, 0.4, 0.4);

            // Set Select Colour
            SelectColour.Width = w * 0.18;
            SelectColour.Height = h * 0.06;
            Canvas.SetTop(SelectColour, h * 0.32);
            Canvas.SetLeft(SelectColour, w * 0.055);

            SelectColourDesc.Width = w * 0.2;
            SelectColourDesc.Height = h * 0.07;
            Canvas.SetTop(SelectColourDesc, h * 0.4);
            Canvas.SetLeft(SelectColourDesc, w * 0.045);

            LeftArrowColours.Width = w * 0.04;
            LeftArrowColours.Height = h * 0.05;
            Canvas.SetTop(LeftArrowColours, h * 0.36);
            Canvas.SetLeft(LeftArrowColours, 0);

            // Set Select Tool
            SelectTool.Width = w * 0.25;
            SelectTool.Height = h * 0.06;
            Canvas.SetTop(SelectTool, h * 0.32);
            Canvas.SetLeft(SelectTool, w * 0.34);

            SelectToolDesc.Width = w * 0.23;
            SelectToolDesc.Height = h * 0.07;
            Canvas.SetTop(SelectToolDesc, h * 0.4);
            Canvas.SetLeft(SelectToolDesc, w * 0.35);

            RightArrowTools.Width = w * 0.04;
            RightArrowTools.Height = h * 0.05;
            Canvas.SetTop(RightArrowTools, h * 0.37);
            Canvas.SetLeft(RightArrowTools, w * 0.595);

            // Set Start Painting
            StartPainting.Width = w * 0.25;
            StartPainting.Height = h * 0.06;
            Canvas.SetTop(StartPainting, h * 0.02);
            Canvas.SetLeft(StartPainting, w * 0.195);

            StartPaintingDesc.Width = w * 0.44;
            StartPaintingDesc.Height = h * 0.16;
            Canvas.SetTop(StartPaintingDesc, h * 0.09);
            Canvas.SetLeft(StartPaintingDesc, w * 0.10);

            // Set Features
            Features.Width = w * 0.19;
            Features.Height = h * 0.06;
            Canvas.SetTop(Features, h * 0.55);
            Canvas.SetLeft(Features, w * 0.12);

            FeaturesDesc.Width = w * 0.43;
            FeaturesDesc.Height = h * 0.14;
            Canvas.SetTop(FeaturesDesc, h * 0.63);
            Canvas.SetLeft(FeaturesDesc, w * 0.025);

            // START
            StartImage.Width = w * 0.14;
            StartImage.Height = h * 0.08;
            Canvas.SetTop(StartImage, h * 0.7);
            Canvas.SetLeft(StartImage, w * 0.46);

            start.Width = w * 0.11;
            start.Height = h * 0.22;
            Canvas.SetTop(start, h * 0.65);
            Canvas.SetLeft(start, w * 0.68);

            RightArrowStart.Width = w * 0.04;
            RightArrowStart.Height = h * 0.05;
            Canvas.SetTop(RightArrowStart, h * 0.72);
            Canvas.SetLeft(RightArrowStart, w * 0.6);

            Restart.Visibility = Visibility.Collapsed;

            // Hide Paint Canvas
            PaintCanvasBorder.Visibility = Visibility.Collapsed;
            PaintCanvas.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Set Tools Panel
        /// </summary>

        private void setToolsPanel()
        {
            // Tools Panel
            WrapTools.Width = w * 0.15;
            WrapTools.Height = h * 0.48;
            Canvas.SetTop(WrapTools, h * 0.25);
            Canvas.SetLeft(WrapTools, w * 0.85);

            String[] s = { "lapis1", "lata1", "erase1", "pincel1" };
            String[] sa = { "lapis2", "lata2", "erase2", "pincel2" };

            Tool0.Height = Tool2.Height = Tool3.Height = Tool1.Height = h * 0.11;
            Tool0.Width = Tool1.Width = Tool2.Width = Tool3.Width = w * 0.15;
            Tool0.Stretch = Tool1.Stretch = Tool2.Stretch = Tool3.Stretch = Stretch.Fill;
            WrapTools.Children.Clear();

            // Fill Tools
            for (i = 0; i < 4; i++)
            {
                // Tool Button
                var button = new Button() { };
                button.Background = null;
                button.Width = w * 0.15;
                button.Height = h * 0.11;
                button.Name = "Tool" + i;
                button.MouseEnter += button_MouseEnter;
                button.MouseLeave += button_MouseLeave;
                button.Click += tool_Click;
                
                button.Opacity = 1;
                button.BorderThickness = new Thickness(0, 0, 0, 0);

                // Image Button
                BitmapImage bitmap = new BitmapImage();

                if (i != 1)
                {
                    YouWindow.bitmapSource(bitmap, "/You_AirPaint;component/Images/" + s[i] + ".png", UriKind.Relative);

                    switch (i)
                    {
                        case 0:
                        {
                            button.Content = Tool0;
                            Tool0.Source = bitmap;
                            break;
                        }
                        case 2:
                        {
                            button.Content = Tool2;
                            Tool2.Source = bitmap;
                            break;
                        }
                        case 3:
                        {
                            button.Content = Tool3;
                            Tool3.Source = bitmap;
                            break;
                        }
                    }
                }
                else
                {
                    YouWindow.bitmapSource(bitmap, "/You_AirPaint;component/Images/" + sa[1] + ".png", UriKind.Relative);
                    button.Content = Tool1;
                    Tool1.Source = bitmap;
                }
                button.VerticalContentAlignment = VerticalAlignment.Stretch;
                button.HorizontalContentAlignment = HorizontalAlignment.Right;
                // Add to Panel
                this.WrapTools.Children.Add(button);

                Canvas.SetTop(button, (i * h * 0.12));
            }
        }

        /// <summary>
        /// Set Colours Panel
        /// </summary>

        private void setColoursScroller()
        {
            //private Button activeColor;
            //private Button[] buttonColors;

            # region Scroller

            // Scroll Panel
            WrapScrollPanel.Width = w * 0.15;
            Canvas.SetTop(WrapScrollPanel, h * 0.325);
            Canvas.SetLeft(WrapScrollPanel, w * 0.015);

            // scrollViewer
            ScrollViewer.Height = h * 0.325;
            ScrollViewer.Width = w * 0.15;
            Canvas.SetTop(ScrollViewer, h * 0.325);
            Canvas.SetLeft(ScrollViewer, w * 0.015);

            // Up Hover Button
            Up.Width = w * 0.13;
            Up.Height = h * 0.06;
            Canvas.SetTop(Up, h * 0.26);
            Canvas.SetLeft(Up, w * 0.025);
            Up.MouseEnter += Up_MouseEnter;
            Up.MouseLeave += OurMouseLeave;

            // Down Hover Button
            Down.Width = w * 0.13;
            Down.Height = h * 0.06;
            Canvas.SetTop(Down, h * 0.66);
            Canvas.SetLeft(Down, w * 0.025);
            Down.MouseEnter += Down_MouseEnter;
            Down.MouseLeave += OurMouseLeave;
            #endregion

            Style s = this.FindResource("MyButtonStyle") as Style;

            // Fill Colours Scroller
            for (i = 0; i < 8; i++)
            {
                // Video Button
                Button button = new Button();
                button.Style = s;
                button.Background = new SolidColorBrush(availableColors[i]);
                button.Width = w * 0.13;
                button.Height = h * 0.13;
                button.Name = "Colour" + i;
                button.Margin = new Thickness(w * 0.01, 0, 0, 0);
                // Add to Scroller
                this.WrapScrollPanel.Children.Add(button);
            }
        }

        # endregion

        private void setButtons(Button button, double bW, double bH, double top, double left)
        {
            button.Width = w * bW;
            button.Height = h * bH;
            Canvas.SetTop(button, h * top);
            Canvas.SetLeft(button, w * left);
        }

        #endregion

        # endregion

        # region Button Handlers

        /// <summary>
        /// Button Click
        /// </summary>
        
        private void click(Button b)
        {
            switch (b.Name)
            {
                case "Mail":
                    if (camera)
                        ExportToPng(PaintCanvas);
                    else
                        saveImage(PaintImage);
                    Leaving();
                    YouNavigation.requestFrameChange(this, "YouKeyboard");
                    break;
                case "Cam":
                    {
                        if (camera)
                        {
                            KinectVideo.Visibility = Visibility.Hidden;
                            camera = false;
                        }
                        else
                        {
                            KinectVideo.Visibility = Visibility.Visible;
                            camera = true;
                        }
                        Console.WriteLine(camera);
                    }
                    break;
                case "Restart":
                    CreatePaintableImage();
                    break;
                case "MainMenuButton":
                    reset();
                    Leaving();
                    YouNavigation.navigateToMainMenu(this);
                    break;
            }
        }

        /// <summary>
        /// Button Click
        /// </summary>
        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)e.OriginalSource;
            click(b);
        }

        /// <summary>
        /// Button Click Colours
        /// </summary>

        private void Button_Click_Colours(object sender, RoutedEventArgs e)
        {
            var b = (Button)e.OriginalSource;
            if (b.Name.Contains("Colour"))
            {
                char[] myChar = b.Name.ToCharArray();

                string colourNumber = myChar.Where(char.IsDigit).Aggregate("", (current, ch) => current + ch.ToString());

                int colour = Convert.ToInt32(colourNumber);
                MyColor = availableColors[colour];
                Brush imageColor = new SolidColorBrush(MyColor);

                PaintCanvasBorder.BorderBrush = imageColor;
            }
        }

        /// <summary>
        /// Button Click Tools
        /// </summary>  

        private void tool_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)e.OriginalSource;
            int i = int.Parse(Regex.Match(b.Name, @"\d+").Value);

            char[] myChar = b.Name.ToCharArray();

            string toolNumber = myChar.Where(char.IsDigit).Aggregate("", (current, ch) => current + ch.ToString());

            int tool = Convert.ToInt32(toolNumber);
            MyBrush = availableBrushes[tool];

            String[] s = { "lapis2", "lata2", "erase2", "pincel2" };
            Console.WriteLine(i);
            BitmapImage bitmap = new BitmapImage();
            YouWindow.bitmapSource(bitmap, "/You_AirPaint;component/Images/" + s[i] + ".png", UriKind.Relative);

            allToolsDown();

            switch (i)
            {
                case 0:
                    {
                        Tool0.Source = bitmap;
                        activeTool = "lapis";
                        break;
                    }
                case 1:
                    {
                        Tool1.Source = bitmap;
                        activeTool = "lata";
                        break;
                    }
                case 2:
                    {
                        Tool2.Source = bitmap;
                        activeTool = "erase";
                        break;
                    }
                case 3:
                    {
                        Tool3.Source = bitmap;
                        activeTool = "pincel";
                        break;
                    }

            }

        }

        /// <summary>
        /// Release all Tools
        /// </summary>
        
        private void allToolsDown()
        {
            BitmapImage bitmap0 = new BitmapImage();
            YouWindow.bitmapSource(bitmap0, "/You_AirPaint;component/Images/lapis1.png", UriKind.Relative);
            Tool0.Source = bitmap0;
            BitmapImage bitmap1 = new BitmapImage();
            YouWindow.bitmapSource(bitmap1, "/You_AirPaint;component/Images/lata1.png", UriKind.Relative);
            Tool1.Source = bitmap1;
            BitmapImage bitmap2 = new BitmapImage();
            YouWindow.bitmapSource(bitmap2, "/You_AirPaint;component/Images/erase1.png", UriKind.Relative);
            Tool2.Source = bitmap2;
            BitmapImage bitmap3 = new BitmapImage();
            YouWindow.bitmapSource(bitmap3, "/You_AirPaint;component/Images/pincel1.png", UriKind.Relative);
            Tool3.Source = bitmap3;
        }

        /// <summary>
        /// Button Leave
        /// </summary>

        void button_MouseLeave(object sender, MouseEventArgs e)
        {
            var b = (Button)e.OriginalSource;

            int i = int.Parse(Regex.Match(b.Name, @"\d+").Value);
            String[] s = { "lapis1", "lata1", "erase1", "pincel1" };
            Console.WriteLine(i);
            BitmapImage bitmap = new BitmapImage();
            YouWindow.bitmapSource(bitmap, "/You_AirPaint;component/Images/" + s[i] + ".png", UriKind.Relative);
            switch (i)
            {
                case 0:
                    {
                        if (activeTool != "lapis")
                        {
                            Tool0.Source = bitmap;
                        }
                        break;
                    }
                case 1:
                    {
                        if (activeTool != "lata")
                        {
                            Tool1.Source = bitmap;
                        }
                        break;
                    }
                case 2:
                    {
                        if (activeTool != "erase")
                        {
                            Tool2.Source = bitmap;
                        }
                        break;
                    }
                case 3:
                    {
                        if (activeTool != "pincel")
                        {
                            Tool3.Source = bitmap;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Button Enter
        /// </summary>
        
        void button_MouseEnter(object sender, MouseEventArgs e)
        {

            var b = (Button)e.OriginalSource;

            int i = int.Parse(Regex.Match(b.Name, @"\d+").Value);
            String[] s = { "lapis2", "lata2", "erase2", "pincel2" };
            Console.WriteLine(i);
            BitmapImage bitmap = new BitmapImage();
            YouWindow.bitmapSource(bitmap, "/You_AirPaint;component/Images/" + s[i] + ".png", UriKind.Relative);
            switch (i)
            {
                case 0:
                    {
                        Tool0.Source = bitmap;
                        break;
                    }
                case 1:
                    {
                        Tool1.Source = bitmap;
                        break;
                    }
                case 2:
                    {
                        Tool2.Source = bitmap;
                        break;
                    }
                case 3:
                    {
                        Tool3.Source = bitmap;
                        break;
                    }

            }

        }

        /// <summary>
        /// Button Leave
        /// </summary>
        
        void OurMouseLeave(object sender, MouseEventArgs e)
        {
            hoverScroller = 0;
            timer.Stop();
        }

        /// <summary>
        /// Button Down Enter
        /// </summary>

        void Down_MouseEnter(object sender, MouseEventArgs e)
        {
            timer.Start();
            hoverScroller = 1;

        }

        /// <summary>
        /// Button Up Enter
        /// </summary>

        void Up_MouseEnter(object sender, MouseEventArgs e)
        {
            timer.Start();
            hoverScroller = 2;
        }

        /// <summary>
        /// Button Start Click
        /// </summary>

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            help = false;

            // Block Buttons
            Mail.IsEnabled = true;
            Cam.IsEnabled = true;

            // Draw Visibilities
            HelpCanvas.Visibility = Visibility.Collapsed;
            PaintCanvasBorder.Visibility = Visibility.Visible;
            PaintCanvas.Visibility = Visibility.Visible;
            ((Button)sender).Visibility = Visibility.Collapsed;
            Restart.Visibility = Visibility.Visible;
        }

        # endregion

        # region Tool Configurations

        /// <summary>
        /// Available Colors Enumerable
        /// </summary>

        public IEnumerable<Color> AvailableColors { get { return availableColors; } }
        private Color[] availableColors = new Color[]
        {
            Colors.Blue,
            Colors.Green,
            Colors.Orange,
            Colors.Purple,
            Colors.Red,    
            Colors.Yellow,
            Colors.Gray,
            Colors.Fuchsia,
        };

        /// <summary>
        /// Available Sizes Enumerable
        /// </summary>
        public IEnumerable<double> AvailableSizes { get { return availableSizes; } }
        private readonly double[] availableSizes = new double[]
        {
            2.0,
            4.0,
            6.0,
            8.0,
            10.0,
            12.0,
            14.0,
        };

        /// <summary>
        /// Available Brushes Enumerable
        /// </summary>

        public IEnumerable<YouBrush> AvailableBrushes { get { return availableBrushes; } }
        private readonly YouBrush[] availableBrushes = new YouBrush[]
        {
            new YouBrush(KinectPaintTools.Pen),
            new YouBrush(KinectPaintTools.Airbrush),
            new YouBrush(KinectPaintTools.Eraser),
            new YouBrush(KinectPaintTools.Brush),
        };

        # endregion

        #region Window Properties


        #region MyColor

        public const string MyColorPropertyName = "SelectedColor";

        /// <summary>
        /// Gets or sets the value of the currently selected color.
        /// This is a dependency property.
        /// </summary>
        public Color MyColor
        {
            get { return (Color)GetValue(MyColorProperty); }
            set { SetValue(MyColorProperty, value); }
        }

        public static readonly DependencyProperty MyColorProperty = DependencyProperty.Register(
            "SelectedColor",
            typeof(Color),
            typeof(AirPaint),
            new UIPropertyMetadata(null));

        #endregion

        #region MySize

        public const string MySizePropertyName = "SelectedSize";

        /// <summary>
        /// Gets or sets the value of the currently selected size.
        /// </summary>
        public double MySize
        {
            get
            {
                return (double)GetValue(MySizeProperty);
            }
            set
            {
                SetValue(MySizeProperty, value);
            }
        }

        public static readonly DependencyProperty MySizeProperty = DependencyProperty.Register(
            MySizePropertyName,
            typeof(double),
            typeof(AirPaint),
            new UIPropertyMetadata(0.0));

        #endregion

        #region MyBrush

        /// <summary>
        /// The <see cref="MyBrush" /> dependency property's name.
        /// </summary>
        public const string MyBrushPropertyName = "SelectedBrush";

        /// <summary>
        /// Gets or sets the value of the currently selected brush.
        /// </summary>
        public YouBrush MyBrush
        {
            get
            {
                return (YouBrush)GetValue(MyBrushProperty);
            }
            set
            {
                SetValue(MyBrushProperty, value);
            }
        }

        public static readonly DependencyProperty MyBrushProperty = DependencyProperty.Register(
            MyBrushPropertyName,
            typeof(YouBrush),
            typeof(AirPaint),
            new UIPropertyMetadata(null));

        #endregion

        #region MyImage

        /// <summary>
        /// Currently paintable image
        /// </summary>
        public WriteableBitmap MyImage
        {
            get { return myImage; }
            set
            {
                myImage = value;

                PaintImage.Source = myImage;

            }
        }
        private WriteableBitmap myImage;

        #endregion

        #endregion

        #region Scroller

        /// <summary>
        /// Scroller Hover Buttons Timer Tick
        /// </summary>
        
        void timer_Tick(object sender, EventArgs e)
        {
            if (hoverScroller == 1)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset + 8);
            }
            else if (hoverScroller == 2)
            {
                ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - 8);
            }
        }

        /// <summary>
        /// Scroller Hover Buttons Timer Click
        /// </summary>

        void HoverTimer_ButtonHoverClick(Button e)
        {
            click(e);
        }

        # endregion

        # region Save Draft

        /// <summary>
        /// Save the Image to send
        /// </summary>
        private void saveImage(Image PaintImage)
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create((BitmapSource)PaintImage.Source));
            using (FileStream stream = new FileStream("AirPaint.png", FileMode.Create))
                encoder.Save(stream);
        }

        /// <summary>
        /// image to .png
        /// </summary>
        public void ExportToPng(Canvas surface)
        {
            // Save current canvas transform
            Transform transform = surface.LayoutTransform;
            // reset current transform (in case it is scaled or rotated)
            surface.LayoutTransform = null;

            // Get the size of canvas
            Size size = new Size(PaintCanvasBorder.Width, PaintCanvasBorder.Height);
            // Measure and arrange the surface
            // VERY IMPORTANT
            surface.Measure(size);
            surface.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            // Create a file stream for saving image
            using (FileStream outStream = new FileStream("AirPaint.png", FileMode.Create))
            {
                // Use png encoder for our data
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                // push the rendered bitmap to it
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                // save the data to the stream
                encoder.Save(outStream);
            }

            // Restore previously saved layout
            surface.LayoutTransform = transform;
        }
        # endregion

        #region YouPlugin Interface Methods

        // Name of the app and namespace. MUST start by "You_*"
        public string getAppName()
        {
            return "You_AirPaint";
        }
        // To identify the main page of the Plugin
        public bool getIsFirstPage()
        {
            return true;
        }
        // To identify which Kinect Requirements need to be active
        // Kinect Region; Skeleton Stream; Interaction Stream
        public KinectRequirements getKinectRequirements()
        {
            return new KinectRequirements(false, true, true);
        }
        // To identify the page name
        public string getName()
        {
            return this.Name;
        }
        // This Page
        public Page getPage()
        {
            return this;
        }
        // To identigy the kinect Region
        // Return your Kinect Region if it is active
        // else return Null
        public KinectRegion getRegion()
        {
            return null;
        }

        #endregion
    }
}
