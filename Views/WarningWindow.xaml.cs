using System;
using System.Windows;
using System.Windows.Threading;

namespace AutoRest.Views
{
    public partial class WarningWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private int _secondsRemaining;

        public event Action PostponeRequested;
        public event Action CancelRequested;

        public WarningWindow(string actionName, int initialSeconds = 60)
        {
            InitializeComponent();

            TxtWarningTitle.Text = $"系统即将在 {initialSeconds} 秒后执行{actionName}";
            _secondsRemaining = initialSeconds;
            TxtCountdownSeconds.Text = _secondsRemaining.ToString();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) =>
            {
                _secondsRemaining--;
                if (_secondsRemaining >= 0)
                {
                    TxtCountdownSeconds.Text = _secondsRemaining.ToString();
                }
                else
                {
                    _timer.Stop();
                    Close();
                }
            };
            _timer.Start();

            // 支持鼠标拖拽移动
            MouseDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };
        }

        private void OnPostponeClicked(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            PostponeRequested?.Invoke();
            Close();
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            CancelRequested?.Invoke();
            Close();
        }
    }
}
