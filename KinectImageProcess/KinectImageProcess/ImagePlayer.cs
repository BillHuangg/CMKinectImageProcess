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
        public ImagePlayer(MainWindow window, string filePath, string fileName, int startNum, int imageCount)
        {
            _windowUI = window;
            _fileName = fileName;
            _filePath = filePath;
            _startNum = startNum;
            _imageCount = imageCount;
            currentImageNum = startNum;
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
            Uri uri = new Uri(@"pack://siteoforigin:,,,/"+_filePath+"/"+_fileName+"_"+currentImageNum+".png", UriKind.RelativeOrAbsolute);
            _windowUI.PNGPlayerElement.Source = new BitmapImage(uri);
            currentImageNum++;

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

    }
}
