using System;
using System.Diagnostics;
using System.Windows.Threading;

namespace AutoRest.Services
{
    public enum PowerAction
    {
        Shutdown = 0,
        Reboot = 1
    }

    public enum ScheduleMode
    {
        Countdown = 0,
        Recurring = 1
    }

    public class TaskTickEventArgs : EventArgs
    {
        public TimeSpan RemainingTime { get; set; }
        public double ProgressFraction { get; set; } // 0.0 ~ 1.0 (已流逝比例)
        public DateTime TargetTime { get; set; }
    }

    /// <summary>
    /// net40 兼容：EventHandler&lt;T&gt; 在 4.0 要求 T 继承 EventArgs，
    /// 因此预警事件使用自定义参数类型，而不是 EventHandler&lt;TimeSpan&gt;。
    /// </summary>
    public class WarningEventArgs : EventArgs
    {
        public TimeSpan Remaining { get; set; }

        public WarningEventArgs(TimeSpan remaining)
        {
            Remaining = remaining;
        }
    }

    public class PowerSchedulerService
    {
        private readonly DispatcherTimer _timer;
        private bool _hasWarned = false;

        public bool IsRunning { get; private set; } = false;
        public PowerAction CurrentAction { get; private set; } = PowerAction.Shutdown;
        public ScheduleMode CurrentMode { get; private set; } = ScheduleMode.Countdown;
        public int IntervalDays { get; private set; } = 1;
        public bool ForceClose { get; set; } = false;
        public bool ShowWarning { get; set; } = true;

        public DateTime StartTime { get; private set; }
        public DateTime TargetTime { get; private set; }
        public TimeSpan TotalDuration { get; private set; }

        public event EventHandler<TaskTickEventArgs> Tick;
        public event EventHandler<WarningEventArgs> WarningNeeded; // 剩余时间 <= 60s
        public event EventHandler TaskCompleted;
        public event EventHandler TaskCancelled;

        public PowerSchedulerService()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
        }

        public void StartCountdown(PowerAction action, int hours, int minutes, int seconds, bool forceClose, bool showWarning)
        {
            CurrentAction = action;
            CurrentMode = ScheduleMode.Countdown;
            IntervalDays = 1;
            ForceClose = forceClose;
            ShowWarning = showWarning;
            _hasWarned = false;

            var span = new TimeSpan(hours, minutes, seconds);
            if (span.TotalSeconds <= 0)
            {
                span = TimeSpan.FromSeconds(10); // 最小防呆保护
            }

            StartTime = DateTime.Now;
            TotalDuration = span;
            TargetTime = StartTime.Add(span);

            IsRunning = true;
            _timer.Start();
            NotifyTick();
        }

        public void StartRecurring(PowerAction action, int intervalDays, int hours, int minutes, int seconds, bool forceClose, bool showWarning)
        {
            CurrentAction = action;
            CurrentMode = ScheduleMode.Recurring;
            IntervalDays = Math.Max(1, intervalDays);
            ForceClose = forceClose;
            ShowWarning = showWarning;
            _hasWarned = false;

            StartTime = DateTime.Now;
            var targetToday = DateTime.Today.Add(new TimeSpan(hours, minutes, seconds));

            if (targetToday <= StartTime)
            {
                // 今天的时间已过，首次触发为指定天数后
                TargetTime = targetToday.AddDays(IntervalDays);
            }
            else
            {
                // 今天的时间还未到，首次即为今天
                TargetTime = targetToday;
            }

            TotalDuration = TargetTime - StartTime;
            if (TotalDuration.TotalSeconds <= 0) TotalDuration = TimeSpan.FromSeconds(60);

            IsRunning = true;
            _timer.Start();
            NotifyTick();
        }

        public void Stop()
        {
            if (IsRunning)
            {
                _timer.Stop();
                IsRunning = false;
                TaskCancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Postpone(int minutes)
        {
            TargetTime = TargetTime.AddMinutes(minutes);
            _hasWarned = false; // 允许再次预警
            NotifyTick();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (!IsRunning) return;

            var remaining = TargetTime - DateTime.Now;

            // 60秒前置顶预警 (仅提醒一次)
            if (ShowWarning && !_hasWarned && remaining.TotalSeconds <= 60 && remaining.TotalSeconds > 0)
            {
                _hasWarned = true;
                var warnHandler = WarningNeeded;
                if (warnHandler != null)
                {
                    warnHandler(this, new WarningEventArgs(remaining));
                }
            }

            // 倒计时结束
            if (remaining.TotalSeconds <= 0)
            {
                _timer.Stop();
                IsRunning = false;
                TaskCompleted?.Invoke(this, EventArgs.Empty);
                ExecutePowerCommand();

                // 若为周期计划，自动顺延准备下一次
                if (CurrentMode == ScheduleMode.Recurring)
                {
                    TargetTime = TargetTime.AddDays(IntervalDays);
                    // 记录到下次启动（由主 ViewModel 落盘）
                }
                return;
            }

            NotifyTick();
        }

        private void NotifyTick()
        {
            var remaining = TargetTime - DateTime.Now;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            double progress = 0;
            if (TotalDuration.TotalSeconds > 0)
            {
                double elapsed = (DateTime.Now - StartTime).TotalSeconds;
                progress = Math.Min(1.0, Math.Max(0.0, elapsed / TotalDuration.TotalSeconds));
            }

            Tick?.Invoke(this, new TaskTickEventArgs
            {
                RemainingTime = remaining,
                ProgressFraction = progress,
                TargetTime = TargetTime
            });
        }

        private void ExecutePowerCommand()
        {
            try
            {
                string actionArg = CurrentAction == PowerAction.Reboot ? "/r" : "/s";
                string forceArg = ForceClose ? " /f" : "";
                string args = $"{actionArg}{forceArg} /t 0";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "shutdown.exe",
                    Arguments = args,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Execute shutdown failed: " + ex.Message);
            }
        }
    }
}
