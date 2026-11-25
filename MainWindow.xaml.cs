using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AutoRest.ViewModels;

namespace AutoRest
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            if (_viewModel.MinimizeOnClose)
            {
                Hide();
            }
            else
            {
                System.Windows.Application.Current.Shutdown();
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_viewModel != null && _viewModel.MinimizeOnClose)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnClosing(e);
        }

        public void BringToFront()
        {
            if (!IsVisible)
            {
                Show();
            }
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }
    }
}
