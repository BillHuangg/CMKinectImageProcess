using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
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
        private static int intervalTime = 100;    //1秒截10帧, 
        private static int shotSceond = 3;
        private int TotalFrameNum = shotSceond*1000 / intervalTime;    //共截2s, 共20帧 

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

        private double screenHeight;
        private double screenWidth;

        //original 120*160
        private double INIT_IMAGE_HEIGHT = 240;
        private double INIT_IMAGE_WIDTH = 320;
        private double IMAGE_HEIGHT_MARGIN;

        private double heightAdjustValue;

        private byte[] playerImage;

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

            screenHeight = SystemParameters.PrimaryScreenHeight;
            screenWidth = SystemParameters.PrimaryScreenWidth;

            Start();
        }
        ~VideoShot()
        {
            Stop();
        }
        public void Start()
        {
            judgeScreenResolution();
            SetUpTimer();
        }
        private void judgeScreenResolution()
        {
            if (screenHeight == 1200)
            {
                IMAGE_HEIGHT_MARGIN = 120;
            }
            else
            {
                screenHeight = 800;
                IMAGE_HEIGHT_MARGIN = 5;
            }
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
                if (_windowUI.ImageLayer.Children.Count <= 10)
                {
                    generateImageOnRow(1.3,3.5,1,170,4);
                }
                else if (_windowUI.ImageLayer.Children.Count <= 18)
                {
                    generateImageOnRow(0.8, 10, 9, 160, 3);
                }
                else if (_windowUI.ImageLayer.Children.Count <= 26)
                {
                    generateImageOnRow(0.6, 20, 17, 150, 2);
                }
                else if (_windowUI.ImageLayer.Children.Count <= 33)
                {
                    generateImageOnRow(0.45, 30, 24, 140, 1);
                }
                else if (_windowUI.ImageLayer.Children.Count <= 39)
                {
                    generateImageOnRow(0.45, 40, 30, 130, 0);
                }
            }
            //
            else
            {
                
                SetImageShot(_kinectDevice, parentProcesser.DepthPixelData, parentProcesser.ColorPixelData);//, _colorFrame, _depthFrame);
                currentFrame++;
            }
        }

        private void generateImageOnRow(double imageOriginalScale,double imageHeightMargin,System.Int32 startCount,
            double imageGap, int zIndex)
        {
            Image temp = generateImageSource(INIT_IMAGE_HEIGHT * imageOriginalScale, INIT_IMAGE_WIDTH * imageOriginalScale);
            Canvas.SetTop(temp, screenHeight - IMAGE_HEIGHT_MARGIN * imageHeightMargin + (double)generateHeightAdjustValue());
            Canvas.SetLeft(temp, ((_windowUI.ImageLayer.Children.Count - startCount)) * imageGap);
            Canvas.SetZIndex(temp, zIndex);
            _windowUI.ImageLayer.Children.Add(temp);
            ImagePlayer ip = new ImagePlayer(_windowUI, "ImageScreenShot", videoName, 0, TotalFrameNum, true, 0.25f, intervalTime, temp);
            ip.Play();
            parentTimer.Dispose();
        }

        private Image generateImageSource(double imageHeight, double imageWidth)
        {
            Image temp = new Image();
            temp.Stretch = Stretch.Fill;
            temp.Height = imageHeight;
            temp.Width = imageWidth;
            //temp.Source = BitmapImage.Create(colorFrameWidth, colorFrameHeight, 96, 96,
            //                                         PixelFormats.Bgra32, null, playerImage,
            //                                         colorFrameStride);
            return temp;
        }

        private int generateHeightAdjustValue()
        {
            Random rTemp;
            rTemp = new Random();
            return rTemp.Next(-20, 20);
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

                string fileName = "ImageScreenShot/" + videoName + "_" + PadLeft(currentFrame) + ".png";
                //TODO: 提高效率
                //1
                System.Drawing.Bitmap b = new System.Drawing.Bitmap(640, 480);
                var bits = b.LockBits(new System.Drawing.Rectangle(0, 0, 640, 480), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                Marshal.Copy(enhPixelData, 0, bits.Scan0, 640 * 480 * 4);
                b.UnlockBits(bits);
                // save
                b.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);


                //2
                //System.IO.File.WriteAllBytes(fileName, enhPixelData);

                //3
                //FileStream pFileStream = null;
                //pFileStream = new FileStream(fileName, FileMode.Create);

                //pFileStream.Write(enhPixelData, 0, enhPixelData.Length);
                //pFileStream.Close();

                //4
                //System.IO.Stream ms = new System.IO.MemoryStream(enhPixelData);
                ////ms.Position = 0;
                //System.Drawing.Image image = System.Drawing.Image.FromStream(ms, false);
                //image.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                //ms.Close();

                //5
                //MemoryStream ms1 = new MemoryStream(enhPixelData);
                //System.Drawing.Bitmap bm = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms1);
                
                //ms1.Close();
                //bm.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
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
