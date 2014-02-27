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
using System.Runtime.InteropServices;
namespace KinectImageProcess
{
    class VideoShot
    {
        private Timer parentTimer;
        delegate void UpdateTimer();
        private int intervalTime = 100;    //1秒截10帧, 
        private int TotalFrameNum = 20;    //共截2s, 共20帧 

        private MainWindow _windowUI;
        private KinectSensor _kinectDevice;
        private ColorImageProcesser parentProcesser;


        private string videoName;

        private int currentFrame = 0;

        private int depthFrameWidth;
        private int depthFrameHeight;

        private int colorFrameWidth;
        private int colorFrameHeight;

        private int depthFrameStride;
        private int colorFrameStride;

        private DepthImageFormat depthImageFormat;
        private ColorImageFormat colorImageFormat;

        private int BytesPerPixel = 4;

        public VideoShot(ColorImageProcesser processer, MainWindow window, int videoNum,
            KinectSensor kinectDevice,
            int dWidht, int dHeight,
            int cWidth, int cHeight,
            DepthImageFormat dImageFormat, ColorImageFormat cImageFormat)
        {
            parentProcesser = processer;
            videoName = PadLeft(videoNum);
            _windowUI = window;
            _kinectDevice = kinectDevice;


            depthFrameWidth = dWidht;
            depthFrameHeight = dHeight;

            colorFrameWidth = cWidth;
            colorFrameHeight = cHeight;

            depthFrameStride = depthFrameWidth * BytesPerPixel;
            colorFrameStride = colorFrameWidth * BytesPerPixel;

            depthImageFormat = dImageFormat;
            colorImageFormat = cImageFormat;

            Start();
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

            //帧数完整后即创建 ImagePlayer,并播放
            if (currentFrame > TotalFrameNum - 1)
            {
                ///set image player
                Image temp = new Image();
                temp.Stretch = Stretch.Fill;


                temp.Height = 160;
                temp.Width = 213;

                Canvas.SetTop(temp, 600);
                Canvas.SetLeft(temp, (_windowUI.ImageLayer.Children.Count - 1) * 170);


                _windowUI.ImageLayer.Children.Add(temp);
                ImagePlayer ip = new ImagePlayer(_windowUI, "ImageScreenShot", videoName, 0, 20, 10, temp);
                ip.Play();
                parentTimer.Dispose();
            }
            //
            else
            {
                
                SetImageShot(_kinectDevice, parentProcesser.DepthPixelData, parentProcesser.ColorPixelData);//, _colorFrame, _depthFrame);
                currentFrame++;
            }
        }
        
        //处理数据并保存
        private void SetImageShot(KinectSensor kinectDevice, short[] depthPixelData, byte[] colorPixelData)//, ColorImageFrame colorFrame, DepthImageFrame depthFrame)
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

                        //
                        colorPoint = kinectDevice.MapDepthToColorImagePoint(depthImageFormat, depthX, depthY, depthPixelData[depthPixelIndex], colorImageFormat);
                        colorPixelIndex = (colorPoint.X * BytesPerPixel) + (colorPoint.Y * colorFrameStride);

                        playerImage[playerImageIndex] = colorPixelData[colorPixelIndex];         //Blue    
                        playerImage[playerImageIndex + 1] = colorPixelData[colorPixelIndex + 1];     //Green
                        playerImage[playerImageIndex + 2] = colorPixelData[colorPixelIndex + 2];     //Red
                        playerImage[playerImageIndex + 3] = 0xFF;                                          //Alpha
                    }
                }
            }

            //
            if (isSomeoneHere)
            {
                SaveFunction(playerImage);
            }

        }
        private void SaveFunction(byte[] enhPixelData)
        {

            try
            {
                //TODO: 提高效率

                System.Drawing.Bitmap b = new System.Drawing.Bitmap(640, 480);
                var bits = b.LockBits(new System.Drawing.Rectangle(0, 0, 640, 480), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(enhPixelData, 0, bits.Scan0, 640 * 480 * 4);
                b.UnlockBits(bits);
                // save
                b.Save("ImageScreenShot/" + videoName + "_" + PadLeft(currentFrame) + ".png", System.Drawing.Imaging.ImageFormat.Png);
            }
            catch (Exception e)
            {

            }

        }
        public void Stop()
        {
            parentTimer.Dispose();
        }



        private string PadLeft(int num)
        {
            string result = "";
            if (num <= 9)
            {
                result = "00" + num;
            }
            else if (num > 9 && num <= 99)
            {
                result = "0" + num;
            }
            else if(num>99&&num<=999)
            {
                result = ""+num;
            }
            return result;


        }

    }
}
