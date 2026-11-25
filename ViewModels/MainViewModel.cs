using System;
using System.Windows.Input;
using AutoRest.Services;
using AutoRest.Views;

namespace AutoRest.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly PowerSchedulerService _scheduler;
        private readonly TrayService _trayService;
        private AppConfig _config;

        // 动作选择
        private bool _isShutdownAction = true;
        private bool _forceCloseApps = false;

        // 模式切换
        private bool _isCountdownMode = true;
        private bool _isDaily = true;
        private int _intervalDays = 3;

        // 时间轮数值
        private int _selectedHours = 1;
        private int _selectedMinutes = 30;
        private int _selectedSeconds = 0;
        private string _expectedTriggerText = "预计触发时间：计算中...";

        // 设置抽屉
        private bool _isSettingsOpen = false;
        private bool _autoStartOnBoot = false;
        private bool _startMinimizedToTray = false;
        private bool _minimizeOnClose = true;
        private bool _showWarningBeforeAction = true;

        // 运行态状态
        private bool _isTaskRunning = false;
        private string _remainingTimeString = "00:00:00";
        private double _progressFraction = 0.0;
        private string _activeTaskTargetDesc = string.Empty;
        private string _activeTaskModeBadge = string.Empty;

        // 命令
        public ICommand StartTaskCommand { get; private set; }
        public ICommand CancelTaskCommand { get; private set; }
        public ICommand ToggleSettingsCommand { get; private set; }
        public ICommand StepIntervalCommand { get; private set; }
        public ICommand PresetCommand { get; private set; }

        public bool IsShutdownAction
        {
            get { return _isShutdownAction; }
            set
            {
                if (SetProperty(ref _isShutdownAction, value, "IsShutdownAction"))
                {
                    OnPropertyChanged("IsRebootAction");
                    OnPropertyChanged("ActionName");
                    OnPropertyChanged("ActionButtonText");
                    UpdateExpectedTriggerText();
                }
            }
        }

        public bool IsRebootAction
        {
            get { return !_isShutdownAction; }
            set { IsShutdownAction = !value; }
        }

        public string ActionName { get { return IsShutdownAction ? "关机" : "重启"; } }

        public string ActionButtonText
        {
            get { return IsShutdownAction ? "启动定时关机任务" : "启动定时重启任务"; }
        }

        public bool ForceCloseApps
        {
            get { return _forceCloseApps; }
            set
            {
                if (SetProperty(ref _forceCloseApps, value, "ForceCloseApps"))
                {
                    SaveCurrentConfig();
                }
            }
        }

        public bool IsCountdownMode
        {
            get { return _isCountdownMode; }
            set
            {
                if (SetProperty(ref _isCountdownMode, value, "IsCountdownMode"))
                {
                    OnPropertyChanged("IsScheduleMode");
                    UpdateExpectedTriggerText();
                }
            }
        }

        public bool IsScheduleMode
        {
            get { return !_isCountdownMode; }
            set { IsCountdownMode = !value; }
        }

        public bool IsDaily
        {
            get { return _isDaily; }
            set
            {
                if (SetProperty(ref _isDaily, value, "IsDaily"))
                {
                    OnPropertyChanged("IsCustomInterval");
                    UpdateExpectedTriggerText();
                }
            }
        }

        public bool IsCustomInterval
        {
            get { return !_isDaily; }
            set { IsDaily = !value; }
        }

        public int IntervalDays
        {
            get { return _intervalDays; }
            set
            {
                int clamped = Math.Max(1, Math.Min(30, value));
                if (SetProperty(ref _intervalDays, clamped, "IntervalDays"))
                {
                    UpdateExpectedTriggerText();
                }
            }
        }

        public int SelectedHours
        {
            get { return _selectedHours; }
            set
            {
                if (SetProperty(ref _selectedHours, value, "SelectedHours"))
                {
                    UpdateExpectedTriggerText();
                }
            }
        }

        public int SelectedMinutes
        {
            get { return _selectedMinutes; }
            set
            {
                if (SetProperty(ref _selectedMinutes, value, "SelectedMinutes"))
                {
                    UpdateExpectedTriggerText();
                }
            }
        }

        public int SelectedSeconds
        {
            get { return _selectedSeconds; }
            set
            {
                if (SetProperty(ref _selectedSeconds, value, "SelectedSeconds"))
                {
                    UpdateExpectedTriggerText();
                }
            }
        }

        public string ExpectedTriggerText
        {
            get { return _expectedTriggerText; }
            private set { SetProperty(ref _expectedTriggerText, value, "ExpectedTriggerText"); }
        }

        public bool IsSettingsOpen
        {
            get { return _isSettingsOpen; }
            set { SetProperty(ref _isSettingsOpen, value, "IsSettingsOpen"); }
        }

        public bool AutoStartOnBoot
        {
            get { return _autoStartOnBoot; }
            set
            {
                if (SetProperty(ref _autoStartOnBoot, value, "AutoStartOnBoot"))
                {
                    StartupService.SetAutoStart(value, StartMinimizedToTray);
                    SaveCurrentConfig();
                }
            }
        }

        public bool StartMinimizedToTray
        {
            get { return _startMinimizedToTray; }
            set
            {
                if (SetProperty(ref _startMinimizedToTray, value, "StartMinimizedToTray"))
                {
                    if (AutoStartOnBoot)
                    {
                        StartupService.SetAutoStart(true, value);
                    }
                    SaveCurrentConfig();
                }
            }
        }

        public bool MinimizeOnClose
        {
            get { return _minimizeOnClose; }
            set
            {
                if (SetProperty(ref _minimizeOnClose, value, "MinimizeOnClose"))
                {
                    SaveCurrentConfig();
                }
            }
        }

        public bool ShowWarningBeforeAction
        {
            get { return _showWarningBeforeAction; }
            set
            {
                if (SetProperty(ref _showWarningBeforeAction, value, "ShowWarningBeforeAction"))
                {
                    SaveCurrentConfig();
                }
            }
        }

        public bool IsTaskRunning
        {
            get { return _isTaskRunning; }
            private set
            {
                if (SetProperty(ref _isTaskRunning, value, "IsTaskRunning"))
                {
                    OnPropertyChanged("IsConfigViewVisible");
                }
            }
        }

        public bool IsConfigViewVisible { get { return !_isTaskRunning; } }

        public string RemainingTimeString
        {
            get { return _remainingTimeString; }
            private set { SetProperty(ref _remainingTimeString, value, "RemainingTimeString"); }
        }

        public double ProgressFraction
        {
            get { return _progressFraction; }
            private set { SetProperty(ref _progressFraction, value, "ProgressFraction"); }
        }

        public string ActiveTaskTargetDesc
        {
            get { return _activeTaskTargetDesc; }
            private set { SetProperty(ref _activeTaskTargetDesc, value, "ActiveTaskTargetDesc"); }
        }

        public string ActiveTaskModeBadge
        {
            get { return _activeTaskModeBadge; }
            private set { SetProperty(ref _activeTaskModeBadge, value, "ActiveTaskModeBadge"); }
        }

        public MainViewModel(PowerSchedulerService scheduler, TrayService trayService)
        {
            _scheduler = scheduler;
            _trayService = trayService;
            _config = new AppConfig();

            StartTaskCommand = new RelayCommand(() => StartTask());
            CancelTaskCommand = new RelayCommand(() => CancelTask());
            ToggleSettingsCommand = new RelayCommand(() => { IsSettingsOpen = !IsSettingsOpen; });
            StepIntervalCommand = new RelayCommand(param =>
            {
                int delta;
                if (param != null && int.TryParse(param.ToString(), out delta))
                {
                    IntervalDays += delta;
                }
            });
            PresetCommand = new RelayCommand(param => ApplyPreset(param));

            _scheduler.Tick += OnSchedulerTick;
            _scheduler.WarningNeeded += OnSchedulerWarningNeeded;
            _scheduler.TaskCancelled += OnSchedulerCancelled;

            LoadConfig();
            UpdateExpectedTriggerText();
        }

        private void LoadConfig()
        {
            try
            {
                _config = ConfigService.Load();
            }
            catch
            {
                _config = new AppConfig();
            }

            _autoStartOnBoot = _config.AutoStartOnBoot;
            _startMinimizedToTray = _config.StartMinimizedToTray;
            _minimizeOnClose = _config.MinimizeOnClose;
            _showWarningBeforeAction = _config.ShowWarningBeforeAction;
            _forceCloseApps = _config.ForceCloseApps;

            _isShutdownAction = _config.LastAction == 0;
            _isCountdownMode = _config.LastMode == 0;
            _intervalDays = Math.Max(1, _config.LastIntervalDays);
            _isDaily = _intervalDays == 1;

            _selectedHours = _config.LastHours;
            _selectedMinutes = _config.LastMinutes;
            _selectedSeconds = _config.LastSeconds;

            // 恢复上次未执行完的定时任务
            if (_config.HasActiveTask && !string.IsNullOrEmpty(_config.ActiveTargetTime))
            {
                DateTime savedTarget;
                if (DateTime.TryParse(_config.ActiveTargetTime, out savedTarget)
                    && savedTarget > DateTime.Now)
                {
                    var remaining = savedTarget - DateTime.Now;
                    if (_config.ActiveMode == 0)
                    {
                        _scheduler.StartCountdown(
                            (PowerAction)_config.ActiveAction,
                            (int)remaining.TotalHours,
                            remaining.Minutes,
                            remaining.Seconds,
                            _config.ForceCloseApps,
                            _config.ShowWarningBeforeAction);
                        ActiveTaskModeBadge = "延时倒计时模式";
                    }
                    else
                    {
                        _scheduler.StartRecurring(
                            (PowerAction)_config.ActiveAction,
                            Math.Max(1, _config.ActiveIntervalDays),
                            savedTarget.Hour,
                            savedTarget.Minute,
                            savedTarget.Second,
                            _config.ForceCloseApps,
                            _config.ShowWarningBeforeAction);
                        ActiveTaskModeBadge = _config.ActiveIntervalDays <= 1
                            ? "每天定时计划"
                            : "每隔 " + _config.ActiveIntervalDays + " 天定时计划";
                    }

                    IsTaskRunning = true;
                    ActiveTaskTargetDesc = "预计在 " + _scheduler.TargetTime.ToString("yyyy-MM-dd HH:mm:ss")
                        + " 触发" + ActionName;
                    _trayService.UpdateStatus(ActionName + "任务中 (" + _scheduler.TargetTime.ToString("HH:mm:ss") + ")", true);
                }
            }
        }

        private void SaveCurrentConfig()
        {
            try
            {
                if (_config == null)
                {
                    _config = new AppConfig();
                }

                _config.AutoStartOnBoot = AutoStartOnBoot;
                _config.StartMinimizedToTray = StartMinimizedToTray;
                _config.MinimizeOnClose = MinimizeOnClose;
                _config.ShowWarningBeforeAction = ShowWarningBeforeAction;
                _config.ForceCloseApps = ForceCloseApps;

                _config.LastAction = IsShutdownAction ? 0 : 1;
                _config.LastMode = IsCountdownMode ? 0 : 1;
                _config.LastIntervalDays = IsDaily ? 1 : IntervalDays;
                _config.LastHours = SelectedHours;
                _config.LastMinutes = SelectedMinutes;
                _config.LastSeconds = SelectedSeconds;

                _config.HasActiveTask = IsTaskRunning;
                _config.ActiveTargetTime = IsTaskRunning
                    ? _scheduler.TargetTime.ToString("yyyy-MM-dd HH:mm:ss")
                    : string.Empty;
                _config.ActiveAction = (int)_scheduler.CurrentAction;
                _config.ActiveMode = (int)_scheduler.CurrentMode;
                _config.ActiveIntervalDays = _scheduler.IntervalDays;

                ConfigService.Save(_config);
            }
            catch
            {
            }
        }

        public void UpdateExpectedTriggerText()
        {
            if (IsCountdownMode)
            {
                var target = DateTime.Now.Add(new TimeSpan(SelectedHours, SelectedMinutes, SelectedSeconds));
                ExpectedTriggerText = "约 " + SelectedHours + " 时 " + SelectedMinutes + " 分 "
                    + SelectedSeconds + " 秒后触发 (预计 " + target.ToString("HH:mm:ss") + ")";
            }
            else
            {
                int days = IsDaily ? 1 : IntervalDays;
                var targetTime = DateTime.Today.Add(new TimeSpan(SelectedHours, SelectedMinutes, SelectedSeconds));
                if (targetTime <= DateTime.Now)
                {
                    targetTime = targetTime.AddDays(days);
                }

                string cycle = IsDaily ? "每天" : "每隔 " + days + " 天";
                ExpectedTriggerText = cycle + " " + SelectedHours.ToString("D2") + ":"
                    + SelectedMinutes.ToString("D2") + ":" + SelectedSeconds.ToString("D2")
                    + " 执行 (下次 " + targetTime.ToString("MM-dd HH:mm") + ")";
            }
        }

        private void ApplyPreset(object parameter)
        {
            string key = parameter == null ? string.Empty : parameter.ToString();
            switch (key)
            {
                case "30m":
                    IsCountdownMode = true;
                    SelectedHours = 0;
                    SelectedMinutes = 30;
                    SelectedSeconds = 0;
                    break;
                case "1h":
                    IsCountdownMode = true;
                    SelectedHours = 1;
                    SelectedMinutes = 0;
                    SelectedSeconds = 0;
                    break;
                case "2h":
                    IsCountdownMode = true;
                    SelectedHours = 2;
                    SelectedMinutes = 0;
                    SelectedSeconds = 0;
                    break;
                case "tonight2330":
                    IsScheduleMode = true;
                    IsDaily = true;
                    SelectedHours = 23;
                    SelectedMinutes = 30;
                    SelectedSeconds = 0;
                    break;
            }
        }

        public void StartTask()
        {
            try
            {
                var action = IsShutdownAction ? PowerAction.Shutdown : PowerAction.Reboot;

                if (IsCountdownMode)
                {
                    _scheduler.StartCountdown(action, SelectedHours, SelectedMinutes, SelectedSeconds,
                        ForceCloseApps, ShowWarningBeforeAction);
                    ActiveTaskModeBadge = "延时倒计时模式";
                }
                else
                {
                    int days = IsDaily ? 1 : IntervalDays;
                    _scheduler.StartRecurring(action, days, SelectedHours, SelectedMinutes, SelectedSeconds,
                        ForceCloseApps, ShowWarningBeforeAction);
                    ActiveTaskModeBadge = IsDaily ? "每天定时计划" : "每隔 " + days + " 天定时计划";
                }

                IsTaskRunning = true;
                ActiveTaskTargetDesc = "预计在 " + _scheduler.TargetTime.ToString("yyyy-MM-dd HH:mm:ss")
                    + " 触发" + ActionName;

                _trayService.UpdateStatus(ActionName + "任务中 (" + _scheduler.TargetTime.ToString("HH:mm:ss") + ")", true);
                _trayService.ShowNotification("AutoRest 任务已启动",
                    ActionName + "任务已安排至 " + _scheduler.TargetTime.ToString("MM-dd HH:mm:ss") + " 触发。");

                SaveCurrentConfig();
            }
            catch (Exception ex)
            {
                CrashLog.Write("StartTask 失败", ex);
            }
        }

        public void CancelTask()
        {
            _scheduler.Stop();
            IsTaskRunning = false;
            _trayService.ShowNotification("AutoRest 任务已取消", "当前的定时任务已成功中止。");
            SaveCurrentConfig();
        }

        public void QuickOneHourShutdown()
        {
            IsShutdownAction = true;
            IsCountdownMode = true;
            SelectedHours = 1;
            SelectedMinutes = 0;
            SelectedSeconds = 0;
            StartTask();
        }

        private void OnSchedulerCancelled(object sender, EventArgs e)
        {
            IsTaskRunning = false;
            _trayService.UpdateStatus("待机中 (无任务)", false);
            SaveCurrentConfig();
        }

        private void OnSchedulerTick(object sender, TaskTickEventArgs e)
        {
            RemainingTimeString = string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)e.RemainingTime.TotalHours,
                e.RemainingTime.Minutes,
                e.RemainingTime.Seconds);

            ProgressFraction = e.ProgressFraction;
            _trayService.UpdateStatus(ActionName + "倒计时 " + RemainingTimeString, true);
        }

        private void OnSchedulerWarningNeeded(object sender, WarningEventArgs args)
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                return;
            }

            TimeSpan remaining = args == null ? TimeSpan.FromSeconds(60) : args.Remaining;
            app.Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    int seconds = (int)remaining.TotalSeconds;
                    if (seconds <= 0)
                    {
                        seconds = 60;
                    }

                    var warningWin = new WarningWindow(ActionName, seconds);
                    warningWin.PostponeRequested += delegate
                    {
                        _scheduler.Postpone(10);
                        _trayService.ShowNotification("AutoRest 任务已延期", "任务已推迟 10 分钟。");
                    };
                    warningWin.CancelRequested += delegate { CancelTask(); };
                    warningWin.Show();
                    warningWin.Activate();
                }
                catch (Exception ex)
                {
                    CrashLog.Write("预警窗口打开失败", ex);
                }
            }));
        }
    }
}
