using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using AutoRest.Services;
using AutoRest.ViewModels;

namespace AutoRest
{
    public partial class App : Application
    {
        private Mutex _mutex;
        private bool _ownsMutex;
        private TrayService _trayService;
        private PowerSchedulerService _scheduler;
        private MainViewModel _mainViewModel;
        private MainWindow _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 先挂异常钩子，任何后续异常都能落盘
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            CrashLog.Write("启动 | " + CrashLog.DescribeEnvironment()
                + " | 参数=" + string.Join(" ", e.Args));

            // 单实例互斥（失败不致命，继续启动）
            try
            {
                _mutex = new Mutex(true, "AutoRest_Desktop_Instance_Mutex", out _ownsMutex);
            }
            catch (Exception ex)
            {
                _ownsMutex = false;
                CrashLog.Write("Mutex 创建失败，忽略单实例限制", ex);
            }

            if (_mutex != null && !_ownsMutex)
            {
                CrashLog.Write("检测到已有实例在运行，退出当前进程");
                Shutdown();
                return;
            }

            try
            {
                CrashLog.Write("步骤1/5 创建系统托盘");
                _trayService = new TrayService();

                CrashLog.Write("步骤2/5 创建调度服务");
                _scheduler = new PowerSchedulerService();

                CrashLog.Write("步骤3/5 创建视图模型");
                _mainViewModel = new MainViewModel(_scheduler, _trayService);

                CrashLog.Write("步骤4/5 创建主窗口 (XAML 解析)");
                _mainWindow = new MainWindow(_mainViewModel);

                CrashLog.Write("步骤5/5 绑定托盘事件");
                BindTrayEvents();

                bool startMinimized = e.Args.Any(arg =>
                        arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
                    || _mainViewModel.StartMinimizedToTray;

                if (!startMinimized)
                {
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    CrashLog.Write("启动成功（显示主窗口）");
                }
                else
                {
                    _trayService.ShowNotification("AutoRest 已后台驻留", "双击托盘图标可唤出主窗口。");
                    CrashLog.Write("启动成功（静默驻留托盘）");
                }
            }
            catch (Exception ex)
            {
                CrashLog.Write("启动失败", ex);
                ShowFatal(ex);
                Shutdown();
            }
        }

        private void BindTrayEvents()
        {
            _trayService.ShowWindowRequested += delegate
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (_mainWindow != null)
                    {
                        _mainWindow.BringToFront();
                    }
                }));
            };

            _trayService.QuickOneHourRequested += delegate
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (_mainViewModel != null)
                    {
                        _mainViewModel.QuickOneHourShutdown();
                    }
                }));
            };

            _trayService.CancelTaskRequested += delegate
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (_mainViewModel != null)
                    {
                        _mainViewModel.CancelTask();
                    }
                }));
            };

            _trayService.ExitAppRequested += delegate
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    Shutdown();
                }));
            };
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            CrashLog.Write("UI 线程未处理异常", e.Exception);
            // 标记已处理，避免 WPF 直接杀进程，让用户能看到提示
            e.Handled = true;
            ShowFatal(e.Exception);
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CrashLog.Write("后台线程未处理异常", e.ExceptionObject as Exception);
        }

        private static void ShowFatal(Exception ex)
        {
            try
            {
                MessageBox.Show(
                    "AutoRest 运行出错，程序将退出。\n\n"
                    + (ex == null ? "未知错误" : ex.GetType().Name + ": " + ex.Message)
                    + "\n\n详细堆栈已写入程序目录下的 crash.log，请把它发给开发者。",
                    "AutoRest",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                if (_scheduler != null)
                {
                    _scheduler.Stop();
                }
            }
            catch
            {
            }

            try
            {
                if (_trayService != null)
                {
                    _trayService.Dispose();
                }
            }
            catch
            {
            }

            // 只有真正持有互斥锁时才释放，否则会抛 ApplicationException
            try
            {
                if (_mutex != null && _ownsMutex)
                {
                    _mutex.ReleaseMutex();
                }
            }
            catch
            {
            }

            try
            {
                if (_mutex != null)
                {
                    _mutex.Dispose();
                }
            }
            catch
            {
            }

            _mutex = null;
            base.OnExit(e);
        }
    }
}
