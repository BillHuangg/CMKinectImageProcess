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

namespace KinectImageProcess
{

    class ImagePlayer
    {
        private Timer parentTimer;
        delegate void UpdateTimer();
        private int intervalTime = 50;      

        private MainWindow _windowUI;

        private string _filePath;
        private string _fileName;
        private int _startNum;
        private int _imageCount;
        private int currentImageNum;

        private Image imagePlayerElement;
        public ImagePlayer(MainWindow window, string filePath, string fileName, int startNum, int imageCount,int interval=50,Image imageElement=null)
        {
            _windowUI = window;
            _fileName = fileName;
            _filePath = filePath;
            _startNum = startNum;
            _imageCount = imageCount;
            currentImageNum = startNum;
            intervalTime = 50;

            imagePlayerElement=imageElement;
        }
        ~ImagePlayer()
        {
            parentTimer.Dispose();
        }
        public void Play()
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
            try
            {
                Uri uri = new Uri(@"pack://siteoforigin:,,,/" + _filePath + "/" + _fileName + "_" + PadLeft(currentImageNum) + ".png", UriKind.RelativeOrAbsolute);

                //区分使用新建element或是预先于xaml设定好的
                if (imagePlayerElement == null)
                {
                    _windowUI.PNGPlayerElement.Source = new BitmapImage(uri);
                }
                else
                {
                    imagePlayerElement.Source = new BitmapImage(uri);
                }

                currentImageNum++;
            }
            catch(Exception e)
            {}
            //reset
            if (currentImageNum >= _imageCount+_startNum)
            {
                currentImageNum = _startNum;
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
            else if (num > 99 && num <= 999)
            {
                result = "" + num;
            }
            return result;


        }
    }
}
