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
using System.Windows.Shapes;

namespace WPF_Demo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CircularProgressBar userVolumeProgress = (CircularProgressBar)userVolume.Children[1];
            CircularProgressBar othersVolumeProgress = (CircularProgressBar)othersVolume.Children[1];
            userVolumeProgress.Percentage = othersVolumeProgress.Percentage = 25;
        }
        private bool _isPlaying = true;
        private bool _isPressed = false;
        private IInputElement _scrollBarSource = null;
        private Canvas _templateCanvas = null;

        private void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Enable moving mouse to change the value.
            _isPressed = true;
            _scrollBarSource = (IInputElement)e.Source;
        }

        private void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //Disable moving mouse to change the value.
            _isPressed = false;
            _scrollBarSource = null;
        }

        private void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isPressed)
            {
               
                CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
                Grid barParentGrid = (Grid)bar.Parent;
                CircularProgressBar progressBar = (CircularProgressBar)barParentGrid.Children[1];

                double angle = GetAngleR(Mouse.GetPosition(_scrollBarSource), bar.Radius + bar.StrokeThickness * 2);
                progressBar.Percentage = (100) * angle / (2 * Math.PI);
                
            }
        }

        private void Ellipse_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isPressed = true;

            CircularProgressBar bar = (CircularProgressBar)_scrollBarSource;
            Grid barParentGrid = (Grid)bar.Parent;
            CircularProgressBar progressBar = (CircularProgressBar)barParentGrid.Children[1];
            double angle = GetAngleR(Mouse.GetPosition(_scrollBarSource), bar.Radius + bar.StrokeThickness * 2);
            progressBar.Percentage = (100) * angle / (2 * Math.PI);
        }

        private void playStopImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (!_isPlaying)
            {
                playStopImage.Source = new BitmapImage(new Uri(@"Resources/Play.png", UriKind.RelativeOrAbsolute));
                _isPlaying = true;
            }
            else
            {
                playStopImage.Source = new BitmapImage(new Uri(@"Resources/Pause.png", UriKind.RelativeOrAbsolute));
                _isPlaying = false;
                
            }
        }

        public static double GetAngleR(Point pos, double radius)
        {
            //Calculate out the distance(r) between the center and the position
            Point center = new Point(radius, radius);
            double xDiff = center.X - pos.X;
            double yDiff = center.Y - pos.Y;
            double r = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);

            //Calculate the angle
            double angle = Math.Acos((center.Y - pos.Y) / r);
            if (pos.X < radius)
                angle = 2 * Math.PI - angle;
            if (Double.IsNaN(angle))
                return 0.0;
            else
                return angle;
        }

        /// <summary>
        /// Drag the window when the mouse button is down anywhere on the grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void myGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.Source is Grid)
            {
                Grid sourceGrid = (Grid)e.Source;
                if (sourceGrid.Name == "myGrid")
                    if (e.ChangedButton == MouseButton.Left)
                        this.DragMove();
            }
        }
    }

    ////The converter used to convert the value to the rotation angle.
    //public class ValueAngleConverter : IMultiValueConverter
    //{
    //    #region IMultiValueConverter Members

    //    public object Convert(object[] values, Type targetType, object parameter,
    //                  System.Globalization.CultureInfo culture)
    //    {
    //        double value = (double)values[0];
    //        double minimum = (double)values[1];
    //        double maximum = (double)values[2];

    //        return MyHelper.GetAngle(value, maximum, minimum);
    //    }

    //    public object[] ConvertBack(object value, Type[] targetTypes, object parameter,
    //          System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}

    ////Convert the value to text.
    //public class ValueTextConverter : IValueConverter
    //{

    //    #region IValueConverter Members

    //    public object Convert(object value, Type targetType, object parameter,
    //              System.Globalization.CultureInfo culture)
    //    {
    //        double v = (double)value;
    //        return String.Format("{0:F2}", v);
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter,
    //        System.Globalization.CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    #endregion
    //}
}
