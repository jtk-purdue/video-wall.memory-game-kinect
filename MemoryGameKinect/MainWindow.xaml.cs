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
using System.Windows.Media.Effects;

namespace MemoryGameKinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region Class Variables

        // Kinect Runtime
        private Runtime _Kinect;

        // HoverButton Controls
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;

        public static bool HoverLock = false;

        public bool logoOn = true;

        // Deck number to be used for game
        // 0 = Animals (Default) // 1 = Fruits // 2 = Cars
        public int deckNumber = 0;

        public bool firstLaunch = true;

        #endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            checkKinect();
            App.Current.Properties["DeckNumber"] = 0;
            firstLaunch = false;
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }

        private void checkKinect()
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
                {

                    Initialize();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Initialize()
        {
            if (_Kinect == null)
                return;

            //_Kinect.Initialize(RuntimeOptions.UseSkeletalTracking);
            _Kinect.SkeletonFrameReady += SkeletonsReady;

            //_Kinect.SkeletonEngine.TransformSmooth = true;
            //var parameters = new TransformSmoothParameters
            //{
                //Smoothing = 0.70f,
                //Correction = 0.1f,
                //Prediction = 0.05f,
                //JitterRadius = 0.14f,
                //MaxDeviationRadius = 0.08f
            //};
            //_Kinect.SkeletonEngine.SmoothParameters = parameters;
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
                SetEllipsePosition(leftEllipse, skeleton.Joints[JointID.HandLeft]);
                SetEllipsePosition(rightEllipse, skeleton.Joints[JointID.HandRight]);

                CheckButton(logoButton, rightEllipse);
                CheckButton(cardButton, rightEllipse);
                Console.WriteLine("Updating");
                //CheckButton(highScoresButton, rightEllipse);

                if (logoOn == false)
                {
                    CheckButton(newButton, rightEllipse);
                    CheckButton(multiButton, rightEllipse);
                    CheckButton(quitButton, rightEllipse);
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
            if (IsItemMidpointInContainer(button, thumbStick))
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

            if (_itemLeft < _leftBoundary - 7 || _rightBoundary + 120 < _itemLeft)
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

        private void newGame_Click(object sender, RoutedEventArgs e)
        {
            uninitializeKinect();
            GameScreen gs = new GameScreen();
            gs.Show();
            //this.Close();
            
        }

        private void uninitializeKinect()
        {
            if (_Kinect != null)
            {
                _Kinect.SkeletonFrameReady -= SkeletonsReady;
                //_Kinect.Uninitialize();
               // _Kinect = null;
            }
        }

        private void multiGame_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Multiplayer - Coming Soon.");
        }

        private void quitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void logoButton_Click(object sender, RoutedEventArgs e)
        {
            logoButton.Visibility = System.Windows.Visibility.Hidden;
            logoOn = false;

            newButton.Visibility = System.Windows.Visibility.Visible;
            multiButton.Visibility = System.Windows.Visibility.Visible;
            quitButton.Visibility = System.Windows.Visibility.Visible;

        }

        private void card_Click(object sender, RoutedEventArgs e)
        {
            //Blur background Window
            var blur = new BlurEffect();
            var current = this.Background;
            blur.Radius = 15;
            this.Background = new SolidColorBrush(Colors.DarkGray);
            this.Effect = blur;

            uninitializeKinect();

            CardSelect cs = new CardSelect();
            cs.Show();
        }

        private void highScoresButton_Click(object sender, RoutedEventArgs e)
        {
            //Clicked on high scores
        }

        private void Window_GotFocus(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("DRANK MY SMOOTHIE AND GAINED FOCUS\n");
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Console.WriteLine("ACTIVATED\n");
           
            if (firstLaunch == false)
            {
                var current = this.Background;
                this.Effect = null;
                this.Background = current;

               // checkKinect();
            }
            _Kinect.SkeletonFrameReady += SkeletonsReady;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Console.WriteLine("DEACTIVATED\n");
            uninitializeKinect();
        }
    }
}
