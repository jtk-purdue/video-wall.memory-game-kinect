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
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Kinect Runtime
        private Runtime _Kinect;

        private void Application_Startup(object sender, StartupEventArgs e)
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
            
        void Application_Exit(object sender, ExitEventArgs e)
        {
            uninitializeKinect();
        }

        private void Initialize()
        {
            if (_Kinect == null)
                return;

            _Kinect.Initialize(RuntimeOptions.UseSkeletalTracking);
            //_Kinect.SkeletonFrameReady += SkeletonsReady;

            _Kinect.SkeletonEngine.TransformSmooth = true;
            var parameters = new TransformSmoothParameters
            {
                Smoothing = 0.70f,
                Correction = 0.1f,
                Prediction = 0.05f,
                JitterRadius = 0.14f,
                MaxDeviationRadius = 0.08f
            };
            _Kinect.SkeletonEngine.SmoothParameters = parameters;
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

        private void uninitializeKinect()
        {
            if (_Kinect != null)
            {
                //_Kinect.SkeletonFrameReady -= SkeletonsReady;
                _Kinect.Uninitialize();
                _Kinect = null;
            }
        }
    }
}
