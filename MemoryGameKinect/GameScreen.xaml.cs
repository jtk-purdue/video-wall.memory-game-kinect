using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Coding4Fun.Kinect.Wpf;
using Coding4Fun.Kinect.Wpf.Controls;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Runtime.InteropServices;

namespace MemoryGameKinect
{
    /// <summary>
    /// Interaction logic for GameScreen.xaml
    /// </summary>
    public partial class GameScreen : Window
    {
        #region Class Variables

        // Stores the screens
        public Screen[] screens;

        // Array of screen references
        public string[] references;

        // Array of screen types
        public string[] types;

        // Array of buttons
        public HoverButton[] buttons;

        // Array of Images
        public Image[] images;

        // Array of backgrounds
        public string[] backgrounds;

        // Stores which two screens are currently on - if these are equal we have a match
        public ArrayList screensOn = new ArrayList();

        // Counts the number of screens that have been matched - if it equals the total number of screens, the game is over
        public int screensMatched;

        // Total number of screens in the game
        public int gameSize;

        // Current background
        int backgroundNumber;

        //ArrayList of all Active Buttons
        public ArrayList activeButtons = new ArrayList();

        // Kinect Runtime
        private Runtime _Kinect;      

        // HoverButton Controls
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;

        // Background worker
        BackgroundWorker _backgroundWorker = new BackgroundWorker();

        // Hover lock to control hovering on objects when not allowed to do so 
        public static bool HoverLock = false;

        // Winner
        public bool hasWon = false;

        // storyBoards for FadeOut
        public ArrayList storyBoardsFadeOut = new ArrayList();

        // storyBoards for FadeIn
        public ArrayList storyBoardsFadeIn = new ArrayList();

        // ArrayList of fadeOutAnimations
        public ArrayList fadeOutAnimations = new ArrayList();

        // ArrayList of fadeInAnimations
        public ArrayList fadeInAnimations = new ArrayList();

        // Positions for fade animations
        public int fadePos1, fadePos2;

        // Stopwatch to keep track of the time
        Stopwatch stopwatch;

        Random randomNumber = new Random();

        internal enum ErrorCondition
        {
            None,
            NoPower,
            NoKinect,
            NoSpeech,
            NotReady,
            KinectAppConflict,
        }

        #endregion

        // Constructor
        public GameScreen()
        {
            InitializeComponent();

            switch((int)App.Current.Properties["DeckNumber"])
            {
                case 0:
                            // Create references
                            references = new string[] { "/Images/Animals/tiger.png", 
                                                        "/Images/Animals/lion.png",
                                                        "/Images/Animals/crocodile.png",
                                                        "/Images/Animals/bear.png",
                                                        "/Images/Animals/zebra.png",
                                                        "/Images/Animals/snowleopard.png",
                                                        "/Images/Animals/elephant.png",
                                                        "/Images/Animals/cheetah.png" };

                            // Create types
                            types = new string[] { "Tiger", "Lion", "Crocodile", "Bear", "Zebra", "SnowLeopard", "Elephant", "Cheetah" };
                            break;

                case 1:
                            // Create references
                            references = new string[] { "/Images/Fruits/blueberry.png", 
                                                        "/Images/Fruits/apple.png",
                                                        "/Images/Fruits/grape.png",
                                                        "/Images/Fruits/passion.png",
                                                        "/Images/Fruits/orange.png",
                                                        "/Images/Fruits/kiwi.png",
                                                        "/Images/Fruits/strawberry.png",
                                                        "/Images/Fruits/watermelon.png" };

                            // Create types
                            types = new string[] { "Blueberry", "Apple", "Grape", "Passion", "Orange", "Kiwi", "Strawberry", "Watermelon" };
                            break;

                case 2:
                            // Create references
                            references = new string[] { "/Images/Cars/car0.png", 
                                                        "/Images/Cars/car1.png",
                                                        "/Images/Cars/car2.png",
                                                        "/Images/Cars/car3.png",
                                                        "/Images/Cars/car4.png",
                                                        "/Images/Cars/car5.png",
                                                        "/Images/Cars/car6.png",
                                                        "/Images/Cars/car7.png" };

                            // Create types
                            types = new string[] { "Car0", "Car1", "Car2", "Car3", "Car4", "Car5", "Car6", "Car7" };
                            break;
            }

            // Set gameSize to 16 screens
            gameSize = 16;

            // Add all buttons
            buttons = new HoverButton[] { Screen0, Screen1, Screen2, Screen3, Screen4, Screen5, Screen6, Screen7,
                                          Screen8, Screen9, Screen10, Screen11, Screen12, Screen13, Screen14, Screen15 };

            // Add all images
            images = new Image[] { Image0, Image1, Image2, Image3, Image4, Image5, Image6, Image7,
                                          Image8, Image9, Image10, Image11, Image12, Image13, Image14, Image15 };

            backgrounds = new string[] { "Jobs", "Gates", "Zuckerburg", "Ritchie", "Messi", "Spears", "Jolie", "RDJ", "Ledger" };

            // Add screens to an array
            screens = new Screen[gameSize];

            // Initialize screens
            for (int i = 0; i < gameSize; i++)
            {
                screens[i] = new Screen(references[i % 8], types[i % 8], i, null);
            }

            // Shuffle screens
            screens = shuffleCards(screens);

            // Initialize random background image
            backgroundNumber = randomNumber.Next(100000) % 9;

            // Add all buttons as active
            for (int i = 0; i < gameSize; i++)
            {
                activeButtons.Add(screens[i]);
            }

            // Set screens matched to 0
            screensMatched = 0;

            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;

            #region Storyboard and Animations Initialization

            for (int i = 0; i < gameSize; i++)
            {
                storyBoardsFadeOut.Add(new Storyboard());
                storyBoardsFadeIn.Add(new Storyboard());

                fadeOutAnimations.Add(new DoubleAnimation());
                fadeInAnimations.Add(new DoubleAnimation());

                ((DoubleAnimation)fadeOutAnimations[i]).From = 1.0;
                ((DoubleAnimation)fadeOutAnimations[i]).To = 0.0;
                ((DoubleAnimation)fadeOutAnimations[i]).Duration = new Duration(TimeSpan.FromSeconds(2));
                ((DoubleAnimation)fadeOutAnimations[i]).Completed += new EventHandler(FadeOutCompleted);
                ((Storyboard)storyBoardsFadeOut[i]).Children.Add((DoubleAnimation)fadeOutAnimations[i]);
                Storyboard.SetTargetName((DoubleAnimation)fadeOutAnimations[i], screens[i].image.Name);
                Storyboard.SetTargetProperty((DoubleAnimation)fadeOutAnimations[i], new PropertyPath(Image.OpacityProperty));

                ((DoubleAnimation)fadeInAnimations[i]).From = 0.0;
                ((DoubleAnimation)fadeInAnimations[i]).To = 1.0;
                ((DoubleAnimation)fadeInAnimations[i]).Duration = new Duration(TimeSpan.FromSeconds(2));
                ((DoubleAnimation)fadeInAnimations[i]).Completed += new EventHandler(FadeInCompleted);
                ((Storyboard)storyBoardsFadeIn[i]).Children.Add((DoubleAnimation)fadeInAnimations[i]);
                Storyboard.SetTargetName((DoubleAnimation)fadeInAnimations[i], screens[i].image.Name);
                Storyboard.SetTargetProperty((DoubleAnimation)fadeInAnimations[i], new PropertyPath(Image.OpacityProperty));
            }

            #endregion

            stopwatch = new Stopwatch();
        }

        // Used to shuffle the screens
        public Screen[] shuffleCards(Screen[] screens)
        {
            Screen temp;
            int x = 0, y = 0;

            for (int i = 0; i < gameSize; i++)
            {
                // Compute positions to swap
                x = i;
                y = randomNumber.Next(0, 15);

                // Swap position x and y in array and update their position markers
                temp = screens[x];
                screens[x] = screens[y];
                screens[y] = temp;

                // Assign screen numbers
                screens[x].screenNumber = x;
                screens[y].screenNumber = y;

                // Assign buttons to screens
                screens[x].button = buttons[x];
                screens[y].button = buttons[y];

                // Assign images
                images[x].Source = new BitmapImage(new Uri(screens[x].reference, UriKind.RelativeOrAbsolute));
                images[y].Source = new BitmapImage(new Uri(screens[y].reference, UriKind.RelativeOrAbsolute));
                screens[x].image = images[x];
                screens[y].image = images[y];

                // Set image visibility
                screens[x].image.Visibility = System.Windows.Visibility.Hidden;
                screens[y].image.Visibility = System.Windows.Visibility.Hidden;
            }
            return screens;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //listen to any status change for Kinects
                Runtime.Kinects.StatusChanged += Kinects_StatusChanged;

                //loop through all the Kinects attached to this PC, and start the first that is connected without an error.
                foreach (Runtime kinect in Runtime.Kinects)
                {
                    if (kinect.Status == KinectStatus.Connected)
                    {
                        _Kinect = kinect;
                        break;
                    }
                }

                if (Runtime.Kinects.Count == 0)
                    MessageBox.Show("No Kinect found");
                else
                    Initialize();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Start the stopwatch
            stopwatch.Start();

            Console.WriteLine("The deck number is " + App.Current.Properties["DeckNumber"]);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
          
        }

        private void Initialize()
        {
            if (_Kinect == null)
                return;

            Console.WriteLine("Added Listener in Game Screen\n\n\n\n");
            //_Kinect.Initialize(RuntimeOptions.UseSkeletalTracking);
            _Kinect.SkeletonFrameReady += SkeletonsReady;

            //_Kinect.SkeletonEngine.TransformSmooth = true;
             
            //var parameters = new TransformSmoothParameters
            //{
            //    Smoothing = 0.70f,
              //  Correction = 0.1f,
               // Prediction = 0.05f,
             // JitterRadius = 0.14f,
            // MaxDeviationRadius = 0.08f
            //};
          //  _Kinect.SkeletonEngine.SmoothParameters = parameters;
        }

        void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (_Kinect == null)
                    {
                        _Kinect = e.KinectRuntime;
                        Initialize();
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (_Kinect == e.KinectRuntime)
                    {
                        //Clean();
                        MessageBox.Show("Kinect was disconnected");
                    }
                    break;
                case KinectStatus.NotReady:
                    break;
                case KinectStatus.NotPowered:
                    if (_Kinect == e.KinectRuntime)
                    {
                        //Clean();
                        MessageBox.Show("Kinect is no more powered");
                    }
                    break;
                default:
                    MessageBox.Show("Unhandled Status: " + e.Status);
                    break;
            }
        }

        void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame allSkeletons = e.SkeletonFrame;

            //KinectSDK TODO: This nullcheck shouldn't be required. 
            //Unfortunately, this version of the Kinect Runtime will continue to fire some skeletonFrameReady events after the Kinect USB is unplugged.
            if (allSkeletons == null)
            {
                return;
            }

            SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();

       

            if (skeleton != null)
            {
                //set position
                Console.WriteLine("Updating");
                SetEllipsePosition(leftEllipse, skeleton.Joints[JointID.HandLeft]);
                SetEllipsePosition(rightEllipse, skeleton.Joints[JointID.HandRight]);

                foreach (Screen i in activeButtons)
                {
                    CheckButton(i.button, rightEllipse);
                }
            }
        }

        private void SetEllipsePosition(FrameworkElement ellipse, Joint joint)
        {
            var scaledJoint = joint.ScaleTo(1920, 1080, .4f, .4f);

            Canvas.SetLeft(ellipse, scaledJoint.Position.X);
            Canvas.SetTop(ellipse, scaledJoint.Position.Y);
        }

        private static void CheckButton(HoverButton button, Ellipse thumbStick)
        {
            if (IsItemMidpointInContainer(button, thumbStick) && HoverLock == false)
            {
                button.Hovering();
            }
            else
            {
                button.Release();
            }
        }

        public static bool IsItemMidpointInContainer(FrameworkElement container, FrameworkElement target)
        {
            FindValues(container, target);

            if (_itemTop < _topBoundary || _bottomBoundary < _itemTop)
            {
                //Midpoint of target is outside of top or bottom
                return false;
            }

            if (_itemLeft < _leftBoundary - 15 || _rightBoundary + 100 < _itemLeft)
            {
                //Midpoint of target is outside of left or right
                return false;
            }

            return true;
        }

        private static void FindValues(FrameworkElement container, FrameworkElement target)
        {
            
            var containerTopLeft = container.PointToScreen(new Point());
            var itemTopLeft = target.PointToScreen(new Point());

            _topBoundary = containerTopLeft.Y;
            _bottomBoundary = _topBoundary + container.ActualHeight;
            _leftBoundary = containerTopLeft.X;
            _rightBoundary = _leftBoundary + container.ActualWidth;

            //use midpoint of item (width or height divided by 2)
            _itemLeft = itemTopLeft.X + (target.ActualWidth / 2);
            _itemTop = itemTopLeft.Y + (target.ActualHeight / 2);
        }
       
        // Check if the two selected screens match
        public void checkMatch()
        {
            Screen temp1 = (Screen)screensOn[0];
            Screen temp2 = (Screen)screensOn[1];

            // If there is a match
            if (temp1.cardType.Equals(temp2.cardType))
                updateAfterMatch();

            else
            {
                // Reset values
                //resetOpenCards();
                _backgroundWorker.RunWorkerAsync();
            }
        }

        public void updateAfterMatch()
        {
            int pos1 = ((Screen)screensOn[0]).screenNumber;
            int pos2 = ((Screen)screensOn[1]).screenNumber;

            fadePos1 = pos1;
            fadePos2 = pos2;

            ((Storyboard)storyBoardsFadeOut[pos1]).Begin(this);
            ((Storyboard)storyBoardsFadeOut[pos2]).Begin(this);

            screensMatched += 2;
            screensOn.Clear();
        }

        public void FadeOutCompleted(object sender, EventArgs e)
        {
            string tempImg1 = "/Images/";
            string tempImg2 = "/Images/";
            tempImg1 += backgrounds[backgroundNumber] + "/" + backgrounds[backgroundNumber] + fadePos1 + ".png";
            tempImg2 += backgrounds[backgroundNumber] + "/" + backgrounds[backgroundNumber] + fadePos2 + ".png";
            screens[fadePos1].image.Source = new BitmapImage(new Uri(tempImg1, UriKind.RelativeOrAbsolute));
            screens[fadePos2].image.Source = new BitmapImage(new Uri(tempImg2, UriKind.RelativeOrAbsolute));

            screens[fadePos1].image.Opacity = 0.0;
            screens[fadePos2].image.Opacity = 0.0;

            ((Storyboard)storyBoardsFadeIn[fadePos1]).Begin(this);
            ((Storyboard)storyBoardsFadeIn[fadePos2]).Begin(this);

        }

        public void FadeInCompleted(object sender, EventArgs e)
        {
            // Delay for 1 second
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();

            // If all screens have been matched
            if (screensMatched == gameSize && hasWon == false)
            {
                // Winner
                Console.WriteLine("Congratulations! Winner!!");
                stopwatch.Stop();
                Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

                // Configure the message box to be displayed
                string messageBoxText = "Time elapsed: " + stopwatch.Elapsed;
                string caption = "Congratulations! Winner!!";
                MessageBoxButton button = MessageBoxButton.OKCancel;
                MessageBoxImage icon = MessageBoxImage.Information;

                MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);
                hasWon = true;

                // Process message box results
                switch (result)
                {
                    case MessageBoxResult.OK:
                        // User pressed Yes button
                        // ..
                        uninitializeKinect();
                        this.Close();
                        break;

                    case MessageBoxResult.Cancel:
                        // User pressed Cancel button
                        // ...
                        uninitializeKinect();
                        this.Close();
                        break;
                }
            }
        }

        private void uninitializeKinect()
        {
            if (_Kinect != null)
            {
                _Kinect.SkeletonFrameReady -= SkeletonsReady;
               // _Kinect.Uninitialize();
               // _Kinect = null;
            }
        }

        public void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            HoverLock = true;

            System.Threading.Thread.Sleep(1500);

            int pos1 = ((Screen)screensOn[0]).screenNumber;
            int pos2 = ((Screen)screensOn[1]).screenNumber;
            System.Threading.Thread thread = new System.Threading.Thread(
                new System.Threading.ThreadStart(
                delegate()
                {
                    screens[pos1].button.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(
                        delegate()
                        {
                            screens[pos1].button.Visibility = System.Windows.Visibility.Visible;
                            screens[pos2].button.Visibility = System.Windows.Visibility.Visible;
                            screens[pos1].image.Visibility = System.Windows.Visibility.Hidden;
                            screens[pos2].image.Visibility = System.Windows.Visibility.Hidden;
                            activeButtons.Add(screens[pos1]);
                            activeButtons.Add(screens[pos2]);
                            screensOn.Clear();
                        }
                    ));
                }
            ));
            thread.Start();
            //thread.Join();
        }

        // Completed Method
        public void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            HoverLock = false;
            if (e.Cancelled)
            {
                //statusText.Text = "Cancelled";
            }
            else if (e.Error != null)
            {
                //statusText.Text = "Exception Thrown";
            }
            else
            {
                //statusText.Text = "Completed";
            }
        }

        #region ButtonListeners

        private void Screen0_Click(object sender, RoutedEventArgs e)
        {
            handleScreen0();
        }

        private void Screen1_Click(object sender, RoutedEventArgs e)
        {
            handleScreen1();
        }

        private void Screen2_Click(object sender, RoutedEventArgs e)
        {
            handleScreen2();
        }

        private void Screen3_Click(object sender, RoutedEventArgs e)
        {
            handleScreen3();
        }

        private void Screen4_Click(object sender, RoutedEventArgs e)
        {
            handleScreen4();
        }

        private void Screen5_Click(object sender, RoutedEventArgs e)
        {
            handleScreen5();
        }

        private void Screen6_Click(object sender, RoutedEventArgs e)
        {
            handleScreen6();
        }

        private void Screen7_Click(object sender, RoutedEventArgs e)
        {
            handleScreen7();
        }

        private void Screen8_Click(object sender, RoutedEventArgs e)
        {
            handleScreen8();
        }

        private void Screen9_Click(object sender, RoutedEventArgs e)
        {
            handleScreen9();
        }

        private void Screen10_Click(object sender, RoutedEventArgs e)
        {
            handleScreen10();
        }

        private void Screen11_Click(object sender, RoutedEventArgs e)
        {
            handleScreen11();
        }

        private void Screen12_Click(object sender, RoutedEventArgs e)
        {
            handleScreen12();
        }

        private void Screen13_Click(object sender, RoutedEventArgs e)
        {
            handleScreen13();
        }

        private void Screen14_Click(object sender, RoutedEventArgs e)
        {
            handleScreen14();
        }

        private void Screen15_Click(object sender, RoutedEventArgs e)
        {
            handleScreen15();
        }

        #endregion

        #region Screen Handlers

        public void handleScreen0()
        {
            if ((activeButtons.Contains(screens[0])) == true)
            {
                screens[0].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[0].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[0]);
                screensOn.Add(screens[0]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen1()
        {
            if ((activeButtons.Contains(screens[1])) == true)
            {
                //Screen1.ActiveImageSource = screens[1].reference;
                screens[1].button.Visibility = System.Windows.Visibility.Hidden;
                screens[1].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[1]);
                screensOn.Add(screens[1]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen2()
        {
            if ((activeButtons.Contains(screens[2])) == true)
            {
                //Screen2.ActiveImageSource = screens[2].reference;
                screens[2].button.Visibility = System.Windows.Visibility.Hidden;
                screens[2].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[2]);
                screensOn.Add(screens[2]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen3()
        {
            if ((activeButtons.Contains(screens[3])) == true)
            {
                //Screen3.ActiveImageSource = screens[3].reference;
                screens[3].button.Visibility = System.Windows.Visibility.Hidden;
                screens[3].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[3]);
                screensOn.Add(screens[3]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen4()
        {
            if ((activeButtons.Contains(screens[4])) == true)
            {
                //Screen4.ActiveImageSource = screens[4].reference;
                screens[4].button.Visibility = System.Windows.Visibility.Hidden;
                screens[4].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[4]);
                screensOn.Add(screens[4]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen5()
        {
            if ((activeButtons.Contains(screens[5])) == true)
            {
                //Screen5.ActiveImageSource = screens[5].reference;
                screens[5].button.Visibility = System.Windows.Visibility.Hidden;
                screens[5].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[5]);
                screensOn.Add(screens[5]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen6()
        {
            if ((activeButtons.Contains(screens[6])) == true)
            {
                //Screen6.ActiveImageSource = screens[6].reference;
                screens[6].button.Visibility = System.Windows.Visibility.Hidden;
                screens[6].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[6]);
                screensOn.Add(screens[6]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen7()
        {
            if ((activeButtons.Contains(screens[7])) == true)
            {
                //Screen7.ActiveImageSource = screens[7].reference;
                screens[7].button.Visibility = System.Windows.Visibility.Hidden;
                screens[7].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[7]);
                screensOn.Add(screens[7]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen8()
        {
            if ((activeButtons.Contains(screens[8])) == true)
            {
                screens[8].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[8].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[8]);
                screensOn.Add(screens[8]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen9()
        {
            if ((activeButtons.Contains(screens[9])) == true)
            {
                screens[9].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[9].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[9]);
                screensOn.Add(screens[9]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen10()
        {
            if ((activeButtons.Contains(screens[10])) == true)
            {
                screens[10].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[10].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[10]);
                screensOn.Add(screens[10]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen11()
        {
            if ((activeButtons.Contains(screens[11])) == true)
            {
                screens[11].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[11].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[11]);
                screensOn.Add(screens[11]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen12()
        {
            if ((activeButtons.Contains(screens[12])) == true)
            {
                screens[12].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[12].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[12]);
                screensOn.Add(screens[12]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen13()
        {
            if ((activeButtons.Contains(screens[13])) == true)
            {
                screens[13].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[13].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[13]);
                screensOn.Add(screens[13]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen14()
        {
            if ((activeButtons.Contains(screens[14])) == true)
            {
                screens[14].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[14].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[14]);
                screensOn.Add(screens[14]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        public void handleScreen15()
        {
            if ((activeButtons.Contains(screens[15])) == true)
            {
                screens[15].button.Visibility = System.Windows.Visibility.Hidden; // screens[0].reference;
                screens[15].image.Visibility = System.Windows.Visibility.Visible;

                //When clicked, removing from activeButtons list
                activeButtons.Remove(screens[15]);
                screensOn.Add(screens[15]);

                if (screensOn.Count == 2)
                    checkMatch();
            }
        }

        #endregion

    }

 
}
