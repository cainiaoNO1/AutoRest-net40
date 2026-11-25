using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AutoRest.Controls
{
    public partial class TimeWheelControl : UserControl
    {
        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register("Hours", typeof(int), typeof(TimeWheelControl),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimePropertyChanged));

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register("Minutes", typeof(int), typeof(TimeWheelControl),
                new FrameworkPropertyMetadata(30, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimePropertyChanged));

        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register("Seconds", typeof(int), typeof(TimeWheelControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTimePropertyChanged));

        public static readonly RoutedEvent TimeChangedEvent =
            EventManager.RegisterRoutedEvent("TimeChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimeWheelControl));

        public event RoutedEventHandler TimeChanged
        {
            add { AddHandler(TimeChangedEvent, value); }
            remove { RemoveHandler(TimeChangedEvent, value); }
        }

        public int Hours
        {
            get { return (int)GetValue(HoursProperty); }
            set { SetValue(HoursProperty, value); }
        }

        public int Minutes
        {
            get { return (int)GetValue(MinutesProperty); }
            set { SetValue(MinutesProperty, value); }
        }

        public int Seconds
        {
            get { return (int)GetValue(SecondsProperty); }
            set { SetValue(SecondsProperty, value); }
        }

        public TimeWheelControl()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateDisplay();
        }

        private static void OnTimePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TimeWheelControl;
            if (control != null)
            {
                control.UpdateDisplay();
                control.RaiseEvent(new RoutedEventArgs(TimeChangedEvent));
            }
        }

        private void UpdateDisplay()
        {
            int h = Normalize(Hours, 24);
            int m = Normalize(Minutes, 60);
            int s = Normalize(Seconds, 60);

            TxtHourPrev.Text = Normalize(h - 1, 24).ToString("D2");
            TxtHourCurr.Text = h.ToString("D2");
            TxtHourNext.Text = Normalize(h + 1, 24).ToString("D2");

            TxtMinPrev.Text = Normalize(m - 1, 60).ToString("D2");
            TxtMinCurr.Text = m.ToString("D2");
            TxtMinNext.Text = Normalize(m + 1, 60).ToString("D2");

            TxtSecPrev.Text = Normalize(s - 1, 60).ToString("D2");
            TxtSecCurr.Text = s.ToString("D2");
            TxtSecNext.Text = Normalize(s + 1, 60).ToString("D2");
        }

        private static int Normalize(int val, int max)
        {
            int res = val % max;
            if (res < 0) res += max;
            return res;
        }

        // ================= 小时交互 =================
        private void OnHoursMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                Hours = Normalize(Hours + 1, 24);
            else if (e.Delta < 0)
                Hours = Normalize(Hours - 1, 24);
            e.Handled = true;
        }

        private void OnHourPrevClicked(object sender, MouseButtonEventArgs e)
        {
            Hours = Normalize(Hours - 1, 24);
        }

        private void OnHourNextClicked(object sender, MouseButtonEventArgs e)
        {
            Hours = Normalize(Hours + 1, 24);
        }

        // ================= 分钟交互 =================
        private void OnMinutesMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                Minutes = Normalize(Minutes + 1, 60);
            else if (e.Delta < 0)
                Minutes = Normalize(Minutes - 1, 60);
            e.Handled = true;
        }

        private void OnMinPrevClicked(object sender, MouseButtonEventArgs e)
        {
            Minutes = Normalize(Minutes - 1, 60);
        }

        private void OnMinNextClicked(object sender, MouseButtonEventArgs e)
        {
            Minutes = Normalize(Minutes + 1, 60);
        }

        // ================= 秒钟交互 =================
        private void OnSecondsMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                Seconds = Normalize(Seconds + 1, 60);
            else if (e.Delta < 0)
                Seconds = Normalize(Seconds - 1, 60);
            e.Handled = true;
        }

        private void OnSecPrevClicked(object sender, MouseButtonEventArgs e)
        {
            Seconds = Normalize(Seconds - 1, 60);
        }

        private void OnSecNextClicked(object sender, MouseButtonEventArgs e)
        {
            Seconds = Normalize(Seconds + 1, 60);
        }
    }
}
