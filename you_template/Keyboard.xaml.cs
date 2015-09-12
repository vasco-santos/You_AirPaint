using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect.Toolkit.Controls;
using YouInteract.YouBasic;
using YouInteract.YouInteractAPI;
using YouInteract.YouPlugin_Developing;

namespace You_AirPaint
{
    /// <summary>
    /// Interaction logic for Keyboard.xaml
    /// </summary>
    public partial class Keyboard : Page, YouPlugin
    {
        private int i;
        private string[] numeros = { "0", "7", "1", "8", "2", "9", "3", "traco", "4", "ponto", "5", "under", "6","arroba"};
        private string[] letras = { "q", "a", "z", "w", "s", "x", "e", "d", "c", "r", "f", "v", "t", "g", "b", "y", "h", "n", "u", "j", "m", "i", "k", "o", "l", "p" };
        private double w, h;
        private bool clear = false;
        //Nos listeners vai se preenchendo esta string com o email
        private string _mail = "";
        //Está este email mas depende do servider smpton onde se irá criar(hotmail,gmail,outlook...)
        private const string Sender = "youinteract@hotmail.com";
        private const double ScrollErrorMargin = 0.001;
        private const int PixelScrollByAmount = 15;
        private bool lettersOn = true;

        public static readonly DependencyProperty PageLeftEnabledProperty = DependencyProperty.Register(
              "PageLeftEnabled", typeof(bool), typeof(AirPaint), new PropertyMetadata(false));

        public static readonly DependencyProperty PageRightEnabledProperty = DependencyProperty.Register(
              "PageRightEnabled", typeof(bool), typeof(AirPaint), new PropertyMetadata(false));

        //Construtor
        public Keyboard()
        {
            InitializeComponent();
            setWindow();
            setScrollLetters();
        }

        //atualizar valor da text box
        private void textBoxRefresh()
        {
            Mail.Text = _mail;
        }
        //funçao de criação da conecção smtp e anexo
        private Boolean sendEmailDois(string email)
        {
            if (email == "")
            {
                return false;
            }
            try
            {
                var Mail = new MailMessage();
                Mail.To.Add(email);

                var MailAdress = new MailAddress(Sender);

                Mail.From = MailAdress;
                Mail.Subject = "YouPaint Image";
                Mail.Body = "Here is your YouPaint image , thanks for using our app! :) ";

                var attachment = new System.Net.Mail.Attachment("AirPaint.png");
                Mail.Attachments.Add(attachment);
                var smtp = new SmtpClient("smtp.live.com", 25);
                smtp.EnableSsl = true;

                var credencial = new NetworkCredential(Sender, "aaaa4AAAA");

                smtp.Credentials = credencial;

                smtp.Send(Mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
            return true;
        }

        //defenir posições dos botões e de outros elementos
        public void setWindow()
        {
            // Get Window Measures
            h = this.Height;
            w = this.Width;

            //Send Email
            Send.Width = w * 0.11;
            Send.Height = h * 0.22;
            Canvas.SetTop(Send, h * 0.01);
            Canvas.SetLeft(Send, w * 0.85);

            //Clear
            Clear.Width = w * 0.15;
            Clear.Height = h * 0.22;
            Canvas.SetTop(Clear, h * 0.01);
            Canvas.SetLeft(Clear, w * 0.15);

            //Numeros
            Numeros.Width = w * 0.18;
            Numeros.Height = h * 0.22;
            Canvas.SetTop(Numeros, h * 0.01);
            Canvas.SetLeft(Numeros, w * 0.30);

            //TextBox
            Mail.Height = h * 0.08;
            Mail.Width = w * 0.27;
            Mail.FontSize = 25;
            Canvas.SetTop(Mail, h * 0.1);
            Canvas.SetLeft(Mail, w * 0.55);

            // Back Button
            MainMenuButton.Width = w * 0.11;
            MainMenuButton.Height = h * 0.22;
            Canvas.SetTop(MainMenuButton, h * 0.01);
            Canvas.SetLeft(MainMenuButton, w * 0.01);

        }
        //event handlers 
        private void Button_Leave_Event(object sender, HandPointerEventArgs e)
        {
        }
        //event handlers 
        private void Button_Hover_Event(object sender, HandPointerEventArgs e)
        {
        }
        //event handlers 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var b = (KinectTileButton)e.OriginalSource;
            if (b.Name == "Send")
            {
                clear = true;
                if (sendEmailDois(Mail.GetLineText(0)))
                {
                    //terá de ser substituido por algo que dê com a kinect(YouButton)
                    //MessageBox.Show("email enviado com sucesso :)");
                    _mail = "Email enviado com sucesso :)";
                    textBoxRefresh();
                    Console.WriteLine("email enviado com sucesso,\n volte ao menu anterior");
                    
                    return;
                    //mudar ppara frama de desenho
                }
                else
                {
                    _mail = "Email enviado com sucesso :)";
                    textBoxRefresh();
                    Console.WriteLine("email nao enviado, volte ao menu anterior");
                    //mudar ppara frama de desenho
                 
                    return;
                }
              

            }
            else if (b.Name == "Clear")
            {
                if (_mail != "")
                {
                    if (clear == false)
                    {
                        _mail = _mail.Remove(_mail.Length - 1);
                    }
                    else
                    {
                        _mail = "";
                        clear = false;
                    }

                    textBoxRefresh();
                }
            }
            else if (b.Name == "aarroba")
            {
                _mail += "@";
                textBoxRefresh();
            }
            else if (b.Name == "aponto")
            {
                _mail += ".";
                textBoxRefresh();
            }
            else if (b.Name == "aunder")
            {
                _mail += "_";
                textBoxRefresh();
            }
            else if (b.Name == "atraco")
            {
                _mail += "-";
                textBoxRefresh();
            }
            else if (b.Name == "Numeros")
            {
                lettersOn = !lettersOn;
                if (lettersOn)
                {
                    BitmapImage bitmapL = new BitmapImage();
                    Image imgL = new Image();
                    bitmapL.BeginInit();
                    bitmapL.UriSource = new Uri("/You_AirPaint;component/Images/Keyboard/simbols.png", UriKind.Relative);
                    bitmapL.EndInit();
                    imgL.Stretch = Stretch.Fill;
                    imgL.Source = bitmapL;
                    Numeros.Content = imgL;
                    Numeros.Background = new ImageBrush(bitmapL);
                    setScrollLetters();
                }
                else
                {
                    BitmapImage bitmapT = new BitmapImage();
                    Image imgT = new Image();
                    bitmapT.BeginInit();
                    bitmapT.UriSource = new Uri("/You_AirPaint;component/Images/Keyboard/letras.png", UriKind.Relative);
                    bitmapT.EndInit();
                    imgT.Stretch = Stretch.Fill;
                    imgT.Source = bitmapT;
                    Numeros.Content = imgT;
                    Numeros.Background = new ImageBrush(bitmapT);
                    setScrollNumbers();
                }
            }
            else if (b.Name == "MainMenuButton")
            {
                _mail = "";
                textBoxRefresh();
                YouNavigation.requestFrameChange(this, "YouAirPaint");
            }
            else
            {
                char[] x = b.Name.ToString().ToCharArray();
                _mail += x[1];
                textBoxRefresh();
            }
        }
        //colocação dos botões dos numeros no scroll viewer
        private void setScrollNumbers()
        {
            this.WrapScrollPanel.Children.Clear();
            // Set Scroll Arrows
            YouWindow.setScrollArrows(ScrollLeft, ScrollRight, 0.65, 0.1, 0.17, 0, 0.17, 0.9);

            // Scroll Panel
            WrapScrollPanel.Height = h * 0.55;
            Canvas.SetTop(WrapScrollPanel, h * 0.27);
            Canvas.SetLeft(WrapScrollPanel, 0);

            // scrollViewer
            ScrollViewer.HoverBackground = Brushes.Transparent;
            ScrollViewer.Height = h * 0.65;
            ScrollViewer.Width = w;
            ScrollViewer.ScrollToHorizontalOffset(150);
            Canvas.SetTop(ScrollViewer, h * 0.27);
            Canvas.SetLeft(ScrollViewer, 0);

            for (i = 0; i < 14; i++)
            {
                // Button
                var button = new YouButton() { };
                //button.Background = new SolidColorBrush(Colors.LightSeaGreen);
                button.Width = w * 0.15;
                button.Height = h * 0.20;

                button.Name = "a" + numeros[i];
                // button.Label = numeros[i];
                button.Background = null;
                button.LabelBackground = null;
                button.Click += new RoutedEventHandler(Button_Click);
                button.EnterEvent += new onHandEnterHandler(Button_Hover_Event);
                button.LeaveEvent += new onhandLeaveHandler(Button_Leave_Event);

                // Image Button
                BitmapImage bitmap = new BitmapImage();
                Image img = new Image();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("/You_AirPaint;component/Images/Keyboard/" + numeros[i] + ".png", UriKind.Relative);
                bitmap.EndInit();
                img.Stretch = Stretch.Fill;
                img.Source = bitmap;
                button.Content = img;
                button.Background = new ImageBrush(bitmap);

                // Add to Scroller
                this.WrapScrollPanel.Children.Add(button);
            }

        }
        //colocação dos botões das letras no scroll viewer
        private void setScrollLetters()
        {
            this.WrapScrollPanel.Children.Clear();
            // Set Scroll Arrows
            YouWindow.setScrollArrows(ScrollLeft, ScrollRight, 0.65, 0.1, 0.17, 0, 0.17, 0.9);

            // Scroll Panel
            WrapScrollPanel.Height = h * 0.65;
            Canvas.SetTop(WrapScrollPanel, h * 0.27);
            Canvas.SetLeft(WrapScrollPanel, 0);


            // scrollViewer
            ScrollViewer.HoverBackground = Brushes.Transparent;
            ScrollViewer.Height = h * 0.65;
            ScrollViewer.Width = w;
            ScrollViewer.ScrollToHorizontalOffset(350);
            Canvas.SetTop(ScrollViewer, h * 0.27);
            Canvas.SetLeft(ScrollViewer, 0);

            // Add in display content
            for (i = 0; i < letras.Length; i++)
            {
                // Video Button
                var button = new YouButton() { };
                //button.Background = new SolidColorBrush(Colors.LightSeaGreen);
                button.Width = w * 0.15;
                button.Height = h * 0.20;
                button.Name = "a" + letras[i];
                //button.Label = letras[i];
                button.LabelBackground = null;
                button.Background = null;
                button.Click += new RoutedEventHandler(Button_Click);
                button.EnterEvent += new onHandEnterHandler(Button_Hover_Event);
                button.LeaveEvent += new onhandLeaveHandler(Button_Leave_Event);

                if (i == 23 || i == 25)
                {
                    var button2 = new Button();
                    button2.Width = w * 0.15;
                    button2.Height = h * 0.20;
                    button2.Visibility = Visibility.Hidden;
                    this.WrapScrollPanel.Children.Add(button2);
                }
                if (i == 1)
                {
                    button.Margin = new Thickness(w * 0.05, 0, 0, 0);
                }
                else if (i == 2)
                {
                    button.Margin = new Thickness(w * 0.1, 0, 0, 0);
                }
                else if ((i != 0 && (i - 1) % 3 == 0) || i == 24)
                {
                    button.Margin = new Thickness(-w * 0.05, 0, 0, 0);
                }
                else if ((i != 0 && i%3 == 0) || i == 23)
                {
                    button.Margin = new Thickness(-w * 0.1, 0, 0, 0);
                }
                

                BitmapImage bitmap = new BitmapImage();
                Image img = new Image();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri("/You_AirPaint;component/Images/Keyboard/" + letras[i] + ".png", UriKind.Relative);
                bitmap.EndInit();
                img.Stretch = Stretch.Fill;
                img.Source = bitmap;
                button.Content = img;
                button.Background = new ImageBrush(bitmap);

                // Add to Scroller
                this.WrapScrollPanel.Children.Add(button);
            }

        }

        #region Scroller
        public bool PageLeftEnabled
        {
            get
            {
                return (bool)GetValue(PageLeftEnabledProperty);
            }

            set
            {
                this.SetValue(PageLeftEnabledProperty, value);
            }
        }
        public bool PageRightEnabled
        {
            get
            {
                return (bool)GetValue(PageRightEnabledProperty);
            }

            set
            {
                this.SetValue(PageRightEnabledProperty, value);
            }
        }

        private void UpdatePagingButtonState()
        {
            this.PageLeftEnabled = ScrollViewer.HorizontalOffset > ScrollErrorMargin;
            this.PageRightEnabled = ScrollViewer.HorizontalOffset < ScrollViewer.ScrollableWidth - ScrollErrorMargin;
        }
        private void PageRightButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + PixelScrollByAmount);
        }
        private void PageLeftButtonClick(object sender, RoutedEventArgs e)
        {
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset - PixelScrollByAmount);
        }
        #endregion

        #region YouPluginInterfaceMethods
        public string getName()
        {
            return this.Name;
        }

        public string getAppName()
        {
            return "You_AirPaint";
        }

        public Page getPage()
        {
            return this;
        }

        public KinectRequirements getKinectRequirements()
        {
            return new KinectRequirements(true, false, false);
        }

        public KinectRegion getRegion()
        {
            return YouKeyboardRegion;
        }

        public bool getIsFirstPage()
        {
            return false;
        }
        #endregion

        private void Button_GripEvent(object sender, HandPointerEventArgs e)
        {
            textBoxRefresh();
            YouNavigation.requestFrameChange(this, "YouAirPaint");
        }
    }
}
