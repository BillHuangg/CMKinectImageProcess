using System;
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
using System.Diagnostics;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Kinect;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Collections;

namespace KinectImageProcess
{
    class ColorImageProcesser
    {
        private Timer parentTimer;
        delegate void UpdateTimer();
        private int intervalTime = 10000;    //3s 

        private MainWindow _windowUI;

        private KinectSensor _kinectDevice;
        private short[] depthPixelData;
        private byte[] colorPixelData;

        //
        //private DepthImageFrame _depthFrame;
        //private ColorImageFrame _colorFrame;
        //


        private int depthFrameWidth;
        private int depthFrameHeight;

        private int colorFrameWidth;
        private int colorFrameHeight;

        private int depthFrameStride;
        private int colorFrameStride;

        private DepthImageFormat depthImageFormat;
        private ColorImageFormat colorImageFormat;

        private int BytesPerPixel = 4;

        private int VideoShotCount = 0;

        public ColorImageProcesser(MainWindow window, KinectSensor kinectDevice,
            int depthDataLength, int colorDataLength,
            int dWidht, int dHeight,
            int cWidth, int cHeight,
            DepthImageFormat dImageFormat, ColorImageFormat cImageFormat)
        {
            _windowUI = window;
            _kinectDevice = kinectDevice;

            depthPixelData = new short[depthDataLength];
            colorPixelData = new byte[colorDataLength];

            depthFrameWidth = dWidht;
            depthFrameHeight = dHeight;

            colorFrameWidth = cWidth;
            colorFrameHeight = cHeight;

            depthFrameStride = depthFrameWidth * BytesPerPixel;
            colorFrameStride = colorFrameWidth * BytesPerPixel;

            depthImageFormat = dImageFormat;
            colorImageFormat = cImageFormat;

        }
        public void SetImageFrame(short[] depthFrameData, byte[] colorFrameData)
        {
            depthPixelData = depthFrameData;
            colorPixelData = colorFrameData;

        }
        public void Start()
        {
            SetUpTimer();
        }
        private void SetUpTimer()
        {
            parentTimer = new Timer(new TimerCallback(OnTimedEvent));
            //每秒执行一次
            parentTimer.Change(0, intervalTime);
        }

        private void OnTimedEvent(object state)
        {
            _windowUI.Dispatcher.BeginInvoke(new UpdateTimer(Update));
        }

        private void Update()
        {
            
            SetImageShot(_kinectDevice);//, _colorFrame, _depthFrame);
        }

        private void SetImageShot(KinectSensor kinectDevice)//, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
        {
            if (kinectDevice != null)// && depthFrame != null && colorFrame != null)
            {
                int depthPixelIndex = 0;
                int playerIndex = 0;
                int colorPixelIndex = 0;
                ColorImagePoint colorPoint;
                byte[] playerImage = new byte[depthFrameHeight * depthFrameStride];
                int playerImageIndex = 0;

                bool isSomeoneHere = false;

                for (int depthY = 0; depthY < depthFrameHeight; depthY++)
                {
                    for (int depthX = 0; depthX < colorFrameWidth; depthX++, playerImageIndex += BytesPerPixel)
                    {
                        depthPixelIndex = depthX + (depthY * colorFrameWidth);
                        playerIndex = depthPixelData[depthPixelIndex] & DepthImageFrame.PlayerIndexBitmask;

                        if (playerIndex != 0)
                        {

                            //
                            isSomeoneHere = true;
                            break;
                            //
                            //colorPoint = kinectDevice.MapDepthToColorImagePoint(depthImageFormat, depthX, depthY, this.depthPixelData[depthPixelIndex], colorImageFormat);
                            //colorPixelIndex = (colorPoint.X * BytesPerPixel) + (colorPoint.Y * colorFrameStride);

                            //playerImage[playerImageIndex] = colorPixelData[colorPixelIndex];         //Blue    
                            //playerImage[playerImageIndex + 1] = colorPixelData[colorPixelIndex + 1];     //Green
                            //playerImage[playerImageIndex + 2] = colorPixelData[colorPixelIndex + 2];     //Red
                            //playerImage[playerImageIndex + 3] = 0xFF;                                          //Alpha
                        }
                    }
                }

                //
                if (isSomeoneHere)
                {
                    VideoShotCount++;
                    VideoShot videoShot = new VideoShot(this, _windowUI, VideoShotCount,
                        _kinectDevice,
                    depthFrameWidth, depthFrameHeight,
                    colorFrameWidth, colorFrameHeight,
                    depthImageFormat, colorImageFormat);

                    //Image temp = new Image();
                    //temp.Stretch = Stretch.Fill;
                    //temp.Height = 120;
                    //temp.Width = 160;

                    //temp.Source = BitmapImage.Create(colorFrameWidth, colorFrameHeight, 96, 96,
                    //                                         PixelFormats.Bgra32, null, playerImage,
                    //                                         colorFrameStride);
                    //Canvas.SetTop(temp, 600);
                    //Canvas.SetLeft(temp, (_windowUI.ImageLayer.Children.Count - 1) * 170);
                    //_windowUI.ImageLayer.Children.Add(temp);
                }
            }
        }

        public void Stop()
        {
            parentTimer.Dispose();
        }




        public short[] DepthPixelData
        {
            get
            {
                return depthPixelData;
            }
        }
        public byte[] ColorPixelData
        {
            get
            {
                return colorPixelData;
            }
        }
    }
}
