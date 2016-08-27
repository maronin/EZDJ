﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EZDJ
{
    /// <summary>
    /// Interaction logic for CircularProgressBar.xaml
    /// </summary>
    public partial class CircularProgressBar : UserControl
    {
        public Point startPoint;
        public Point backgroundEndPoint;
        public Size backgroundSize;
        public CircularProgressBar()
        {

            InitializeComponent();

            set_background_color(BackgroundColor);
            set_Color(new SolidColorBrush(Colors.Red));
            set_tick(26);

            RenderArc();
            RenderBackgroundArc();

        }


        //in seconds
        public double timeProgress
        {
            get { return (double)GetValue(timeProgressProperty);  }
            set { SetValue(timeProgressProperty, value);  }
        }

        public int Radius
        {
            get { return (int)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public Brush BackgroundColor {
            get { return (Brush)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        public Brush SegmentColor
        {
            get { return (Brush)GetValue(SegmentColorProperty); }
            set { SetValue(SegmentColorProperty, value); }
        }

        public int StrokeThickness
        {            
            get { return (int)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public static readonly DependencyProperty timeProgressProperty =
            DependencyProperty.Register("timeProgress", typeof(double), typeof(CircularProgressBar), new PropertyMetadata(65d, new PropertyChangedCallback(onTimeProgressChanged)));

        // Using a DependencyProperty as the backing store for Percentage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register("Percentage", typeof(double), typeof(CircularProgressBar), new PropertyMetadata(65d, new PropertyChangedCallback(OnPercentageChanged)));

        // Using a DependencyProperty as the backing store for StrokeThickness.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(int), typeof(CircularProgressBar), new PropertyMetadata(25, new PropertyChangedCallback(OnThicknessChanged)));

        // Using a DependencyProperty as the backing store for SegmentColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SegmentColorProperty =
            DependencyProperty.Register("SegmentColor", typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(new SolidColorBrush(Colors.Red), new PropertyChangedCallback(OnColorChanged)));

        // Using a DependencyProperty as the backing store for BackgroundColor.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register("BackgroundColor", typeof(Brush), typeof(CircularProgressBar), new PropertyMetadata(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF878889")), new PropertyChangedCallback(OnBackgroundColorChanged)));


        // Using a DependencyProperty as the backing store for Radius.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RadiusProperty =
            DependencyProperty.Register("Radius", typeof(int), typeof(CircularProgressBar), new PropertyMetadata(100, new PropertyChangedCallback(OnPropertyChanged)));

        // Using a DependencyProperty as the backing store for Angle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AngleProperty =
            DependencyProperty.Register("Angle", typeof(double), typeof(CircularProgressBar), new PropertyMetadata(120d, new PropertyChangedCallback(OnPropertyChanged)));


        private static void OnColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            circle.set_Color((SolidColorBrush)args.NewValue);
        }
        private static void OnBackgroundColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            circle.set_background_color((SolidColorBrush)args.NewValue);
        }

        private static void onTimeProgressChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {

        }


        private static void OnThicknessChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            circle.set_tick((int)args.NewValue);
            circle.RenderArc();
            circle.RenderBackgroundArc();
        }

        private static void OnPercentageChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            
            if (circle.Percentage > 100) circle.Percentage = 100;
            circle.Angle = (circle.Percentage * 360) / 100;
        }

        private static void OnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            CircularProgressBar circle = sender as CircularProgressBar;
            
            circle.RenderArc();
            circle.RenderBackgroundArc();
        }

        public void set_tick(int n)
        {
            pathRoot.StrokeThickness = n;
            backGroundPathRoot.StrokeThickness = n - 0.1;
        }

        public void set_Color(Brush n)
        {
            pathRoot.Stroke = n;
        }

        public void set_background_color(Brush n)
        {
            backGroundPathRoot.Stroke = n;
        }

        private void RenderBackgroundArc()
        {
            startPoint = new Point(Radius, 0);

            backGroundPathRoot.Width = Radius * 2 + StrokeThickness;
            backGroundPathRoot.Height = Radius * 2 + StrokeThickness;
            backGroundPathRoot.Margin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);

            backgroundEndPoint = ComputeCartesianCoordinate(360, Radius);
            backgroundEndPoint.X += Radius - 0.01;
            backgroundEndPoint.Y += Radius;

            pathBackgroundFigure.StartPoint = startPoint;
            arcBackgroundSegment.Point = backgroundEndPoint;
            arcBackgroundSegment.Size = new Size(Radius, Radius);
            arcBackgroundSegment.IsLargeArc = true;

        }

        public void RenderArc()
        {
            Point startPoint = new Point(Radius, 0);
            Point endPoint = ComputeCartesianCoordinate(Angle, Radius);
            endPoint.X += Radius;
            endPoint.Y += Radius;

            pathRoot.Width = Radius * 2 + StrokeThickness;
            pathRoot.Height = Radius * 2 + StrokeThickness;
            pathRoot.Margin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);



            bool largeArc = Angle > 180.0;

            Size outerArcSize = new Size(Radius, Radius);

            pathFigure.StartPoint = startPoint;

            if (startPoint.X == Math.Round(endPoint.X) && startPoint.Y == Math.Round(endPoint.Y))
                endPoint.X -= 0.01;

            arcSegment.Point = endPoint;
            arcSegment.Size = outerArcSize;
            arcSegment.IsLargeArc = largeArc;
        }

        private Point ComputeCartesianCoordinate(double angle, double radius)
        {
            // convert to radians
            double angleRad = (Math.PI / 180.0) * (angle - 90);

            double x = radius * Math.Cos(angleRad);
            double y = radius * Math.Sin(angleRad);

            return new Point(x, y);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            double newRadius = Math.Min(e.NewSize.Width, e.NewSize.Height);
            newRadius = (newRadius / 2) - StrokeThickness * 2;
            if (newRadius > 0)
                Radius = (int)newRadius;            
        }
    }
}
