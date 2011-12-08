﻿using System;
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
    /// Interaction logic for CardSelect.xaml
    /// </summary>
    public partial class CardSelect : Window
    {
        #region Class Variables

        // Array of animal image paths
        public string[] animals;

        // Array of fruit image paths
        public string[] fruits;

        // Array of cars image paths
        public string[] cars;

        // Index of image to pick
        public int animalNumber;
        public int fruitNumber;
        public int carNumber;

        // Kinect Runtime
        private Runtime _Kinect;

        // HoverButton Controls
        private static double _topBoundary;
        private static double _bottomBoundary;
        private static double _leftBoundary;
        private static double _rightBoundary;
        private static double _itemLeft;
        private static double _itemTop;

        #endregion

        public CardSelect()
        {
            InitializeComponent();

            animals = new string[] { "/Images/Animals/tiger.png", 
                                        "/Images/Animals/lion.png",
                                        "/Images/Animals/crocodile.png",
                                        "/Images/Animals/bear.png",
                                        "/Images/Animals/zebra.png",
                                        "/Images/Animals/snowleopard.png",
                                        "/Images/Animals/elephant.png",
                                        "/Images/Animals/cheetah.png" };

            fruits = new string[] { "/Images/Fruits/blueberry.png", 
                                        "/Images/Fruits/apple.png",
                                        "/Images/Fruits/grape.png",
                                        "/Images/Fruits/orange.png",
                                        "/Images/Fruits/passion.png",
                                        "/Images/Fruits/strawberry.png",
                                        "/Images/Fruits/watermelon.png",
                                        "/Images/Fruits/kiwi.png" };

            cars = new string[] { "/Images/Cars/car0.png", 
                                        "/Images/Cars/car1.png",
                                        "/Images/Cars/car2.png",
                                        "/Images/Cars/car3.png",
                                        "/Images/Cars/car4.png",
                                        "/Images/Cars/car5.png",
                                        "/Images/Cars/car6.png",
                                        "/Images/Cars/car7.png" };

            // Initialize random background image
            Random randomNumber = new Random();
            animalNumber = randomNumber.Next(100000) % 8;
            fruitNumber = randomNumber.Next(100000) % 8;
            carNumber = randomNumber.Next(100000) % 8;

            Deck0.Source = new BitmapImage(new Uri(animals[animalNumber], UriKind.RelativeOrAbsolute));
            Deck1.Source = new BitmapImage(new Uri(fruits[fruitNumber], UriKind.RelativeOrAbsolute));
            Deck2.Source = new BitmapImage(new Uri(cars[carNumber], UriKind.RelativeOrAbsolute));
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
        }

        private void Window_Closed(object sender, RoutedEventArgs e)
        {
            
            Console.WriteLine("CARD SELECT CLOSED\n");
        }

        private void Initialize()
        {
            if (_Kinect == null)
                return;

          //  _Kinect.Initialize(RuntimeOptions.UseSkeletalTracking);
            _Kinect.SkeletonFrameReady += SkeletonsReady;

           // _Kinect.SkeletonEngine.TransformSmooth = true;
           // var parameters = new TransformSmoothParameters
           // {
             //   Smoothing = 0.70f,
              //  Correction = 0.1f,
               // Prediction = 0.05f,
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

                CheckButton(Button0, rightEllipse);
                CheckButton(Button1, rightEllipse);
                CheckButton(Button2, rightEllipse);
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

            if (_itemLeft < _leftBoundary|| _rightBoundary < _itemLeft)
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

        private void uninitializeKinect()
        {
            if (_Kinect != null)
            {
                _Kinect.SkeletonFrameReady -= SkeletonsReady;
           //     _Kinect.Uninitialize();
            //    _Kinect = null;
            }
        }

        private void Button0_Click(object sender, RoutedEventArgs e)
        {       
            App.Current.Properties["DeckNumber"] = 0;
            uninitializeKinect();
            this.Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Properties["DeckNumber"] = 1;
            uninitializeKinect();
            this.Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Properties["DeckNumber"] = 2;
            uninitializeKinect();
            this.Close();
        }
    }
}
