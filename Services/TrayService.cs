using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AutoRest.Services
{
    public class TrayService : IDisposable
    {
        private NotifyIcon _notifyIcon;
        private ContextMenuStrip _contextMenu;
        private ToolStripMenuItem _statusMenuItem;
        private ToolStripMenuItem _cancelMenuItem;

        public event Action ShowWindowRequested;
        public event Action QuickOneHourRequested;
        public event Action CancelTaskRequested;
        public event Action ExitAppRequested;

        public TrayService()
        {
            InitializeTray();
        }

        private void InitializeTray()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "AutoRest - 定时关机与重启助手",
                Icon = SafeCreateIcon(),
                Visible = true
            };

            _contextMenu = new ContextMenuStrip();

            var titleItem = new ToolStripMenuItem("AutoRest 桌面助手")
            {
                Enabled = false,
                Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold)
            };
            _contextMenu.Items.Add(titleItem);
            _contextMenu.Items.Add(new ToolStripSeparator());

            var showItem = new ToolStripMenuItem("显示主窗口", null, (s, e) => ShowWindowRequested?.Invoke());
            _contextMenu.Items.Add(showItem);

            _statusMenuItem = new ToolStripMenuItem("状态：未配置定时任务") { Enabled = false };
            _contextMenu.Items.Add(_statusMenuItem);

            var quickHourItem = new ToolStripMenuItem("⚡ 延时 1 小时关机", null, (s, e) => QuickOneHourRequested?.Invoke());
            _contextMenu.Items.Add(quickHourItem);

            _cancelMenuItem = new ToolStripMenuItem("■ 取消当前任务", null, (s, e) => CancelTaskRequested?.Invoke())
            {
                Enabled = false
            };
            _contextMenu.Items.Add(_cancelMenuItem);

            _contextMenu.Items.Add(new ToolStripSeparator());
            var exitItem = new ToolStripMenuItem("退出 AutoRest", null, (s, e) => ExitAppRequested?.Invoke());
            _contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => ShowWindowRequested?.Invoke();
        }

        public void UpdateStatus(string statusText, bool hasActiveTask)
        {
            if (_statusMenuItem != null)
            {
                _statusMenuItem.Text = $"状态：{statusText}";
            }
            if (_cancelMenuItem != null)
            {
                _cancelMenuItem.Enabled = hasActiveTask;
            }
            if (_notifyIcon != null)
            {
                string tip = $"AutoRest: {statusText}";
                if (tip.Length > 63) tip = tip.Substring(0, 60) + "...";
                _notifyIcon.Text = tip;
            }
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, message, icon);
        }

        /// <summary>
        /// 托盘图标安全兜底：GDI+ 自绘失败时退回系统自带图标，绝不因图标问题崩溃。
        /// </summary>
        private Icon SafeCreateIcon()
        {
            try
            {
                var icon = CreateDynamicAppIcon();
                if (icon != null)
                {
                    return icon;
                }
            }
            catch (Exception ex)
            {
                CrashLog.Write("自绘托盘图标失败，改用系统默认图标", ex);
            }

            try
            {
                return (Icon)SystemIcons.Application.Clone();
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private Icon CreateDynamicAppIcon()
        {
            // 纯 GDI+ 代码动态绘制一个 32x32 的精致电源暗黑矢量图标
            using (var bitmap = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                // 深暗黑圆角背景底色 (#161922)
                using (var brush = new SolidBrush(Color.FromArgb(22, 25, 34)))
                {
                    g.FillEllipse(brush, 1, 1, 30, 30);
                }

                // 外发光边框 (#2563EB)
                using (var pen = new Pen(Color.FromArgb(37, 99, 235), 1.8f))
                {
                    g.DrawEllipse(pen, 1, 1, 30, 30);
                }

                // 绘制高亮闪电/电源符号
                using (var pen = new Pen(Color.FromArgb(56, 189, 248), 2.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    // 电源开关竖线 (上)
                    g.DrawLine(pen, 16, 7, 16, 15);
                    // 电源圆弧 (开环)
                    g.DrawArc(pen, 8, 9, 16, 16, -60, 300);
                }

                IntPtr hIcon = bitmap.GetHicon();
                using (var temp = Icon.FromHandle(hIcon))
                {
                    return (Icon)temp.Clone();
                }
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            if (_contextMenu != null)
            {
                _contextMenu.Dispose();
                _contextMenu = null;
            }
        }
    }
}
