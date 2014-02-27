using System;
//using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

namespace KinectImageProcess
{
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor;
        private WriteableBitmap screenImageBitmap;
        private Int32Rect _GreenScreenImageRect;
        private int ScreenImageStride;
        private short[] depthPixelData;
        private byte[] colorPixelData;
        private bool _DoUsePolling;

        private double screenHeight;
        private double screenWidth;

        private ColorImageProcesser imageProcesser;

        public MainWindow()
        {
            InitializeComponent();

            this._DoUsePolling = true;
            if (this._DoUsePolling)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
            else
            {
                KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                this.KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);
            }
            screenHeight = this.Height;
            screenWidth = this.Width;
            PNGPlayerElement.Height = screenHeight;
            PNGPlayerElement.Width = screenWidth;
            ImagePlayer ip = new ImagePlayer(this, "smoke_alpha", "smoke_alpha", 100, 50, 50,PNGPlayerElement);
            ip.Play();
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }


        private void KinectDevice_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
                {
                    ColorImageProcessing(this.kinectSensor, colorFrame, depthFrame);
                }
            }
        }


        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            DiscoverKinect();
             
            if (this.KinectDevice != null)
            {
                try
                {
                 
                    using (ColorImageFrame colorFrame = this.KinectDevice.ColorStream.OpenNextFrame(1000))
                    {
                        using (DepthImageFrame depthFrame = this.KinectDevice.DepthStream.OpenNextFrame(1000))
                        {
                            ColorImageProcessing(this.KinectDevice, colorFrame, depthFrame);
                            
                        }
                    }
                }
                catch (Exception)
                {
                    //Do nothing, because the likely result is that the Kinect has been unplugged.     
                }
            }
        }


        private void ColorImageProcessing(KinectSensor kinectDevice, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (kinectDevice != null && depthFrame != null && colorFrame != null)
            {
                int depthPixelIndex;
                int playerIndex;
                int colorPixelIndex;
                ColorImagePoint colorPoint;
                int colorStride = colorFrame.BytesPerPixel * colorFrame.Width;
                int bytesPerPixel = 4;
                byte[] playerImage = new byte[depthFrame.Height * this.ScreenImageStride];
                int playerImageIndex = 0;


                depthFrame.CopyPixelDataTo(this.depthPixelData);
                colorFrame.CopyPixelDataTo(this.colorPixelData);


                //get data 
                imageProcesser.SetImageFrame(depthPixelData, colorPixelData);


                //debug view
                for (int depthY = 0; depthY < depthFrame.Height; depthY++)
                {
                    for (int depthX = 0; depthX < depthFrame.Width; depthX++, playerImageIndex += bytesPerPixel)
                    {
                        depthPixelIndex = depthX + (depthY * depthFrame.Width);
                        playerIndex = this.depthPixelData[depthPixelIndex] & DepthImageFrame.PlayerIndexBitmask;

                        if (playerIndex != 0)
                        {
                            colorPoint = kinectDevice.MapDepthToColorImagePoint(depthFrame.Format, depthX, depthY, this.depthPixelData[depthPixelIndex], colorFrame.Format);
                            colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) + (colorPoint.Y * colorStride);

                            playerImage[playerImageIndex] = this.colorPixelData[colorPixelIndex];         //Blue    
                            playerImage[playerImageIndex + 1] = this.colorPixelData[colorPixelIndex + 1];     //Green
                            playerImage[playerImageIndex + 2] = this.colorPixelData[colorPixelIndex + 2];     //Red
                            playerImage[playerImageIndex + 3] = 0xFF;                                          //Alpha
                        }
                    }
                }
                this.screenImageBitmap.WritePixels(this._GreenScreenImageRect, playerImage, this.ScreenImageStride, 0);


            }
        }

        private void DiscoverKinect()
        {
            if (this.kinectSensor != null && this.kinectSensor.Status != KinectStatus.Connected)
            {
                UninitializeKinectSensor(this.kinectSensor);
                this.kinectSensor = null;
            }


            if (this.kinectSensor == null)
            {
                this.kinectSensor = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status == KinectStatus.Connected);


                if (this.kinectSensor != null)
                {
                    InitializeKinectSensor(this.kinectSensor);
                }
            }
        }


        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.DepthStream.Range = DepthRange.Default;

                sensor.SkeletonStream.Enable();
                sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);


                DepthImageStream depthStream = sensor.DepthStream;
                this.screenImageBitmap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Bgra32  , null);
                this._GreenScreenImageRect = new Int32Rect(0, 0, (int)Math.Ceiling(this.screenImageBitmap.Width), (int)Math.Ceiling(this.screenImageBitmap.Height));
                this.ScreenImageStride = depthStream.FrameWidth * 4;
                this.GreenScreenImage.Source = this.screenImageBitmap;

                this.depthPixelData = new short[this.kinectSensor.DepthStream.FramePixelDataLength];
                this.colorPixelData = new byte[this.kinectSensor.ColorStream.FramePixelDataLength];
                if (!this._DoUsePolling)
                {
                    sensor.AllFramesReady += KinectDevice_AllFramesReady;
                }

                sensor.Start();

                //init the image processer
                imageProcesser = new ColorImageProcesser(this, sensor, 
                    this.kinectSensor.DepthStream.FramePixelDataLength, this.kinectSensor.ColorStream.FramePixelDataLength,
                    depthStream.FrameWidth, depthStream.FrameHeight,
                    sensor.ColorStream.FrameWidth, sensor.ColorStream.FrameHeight,
                    sensor.DepthStream.Format,sensor.ColorStream.Format);
                imageProcesser.Start();
            }
        }


        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorStream.Disable();
                sensor.DepthStream.Disable();
                sensor.SkeletonStream.Disable();
                sensor.AllFramesReady -= KinectDevice_AllFramesReady;
            }
        }


        public KinectSensor KinectDevice
        {
            get { return this.kinectSensor; }
            set
            {
                if (this.kinectSensor != value)
                {
                    //Uninitialize
                    if (this.kinectSensor != null)
                    {
                        UninitializeKinectSensor(this.kinectSensor);
                        this.kinectSensor = null;
                    }

                    this.kinectSensor = value;

                    //Initialize
                    if (this.kinectSensor != null)
                    {
                        if (this.kinectSensor.Status == KinectStatus.Connected)
                        {
                            InitializeKinectSensor(this.kinectSensor);
                        }
                    }
                }
            }
        }
    }
}
