using System;
using System.IO;
using System.Text;

namespace AutoRest.Services
{
    /// <summary>
    /// 极简崩溃诊断日志：把启动步骤与异常完整堆栈写入 crash.log。
    /// 写入位置优先 exe 同目录（免安装绿色场景），失败则退回 %AppData%\AutoRest。
    /// </summary>
    public static class CrashLog
    {
        private static readonly object SyncRoot = new object();

        public static string LogPath
        {
            get
            {
                string exeDir = null;
                try
                {
                    exeDir = Path.GetDirectoryName(typeof(CrashLog).Assembly.Location);
                }
                catch
                {
                }

                if (!string.IsNullOrEmpty(exeDir))
                {
                    return Path.Combine(exeDir, "crash.log");
                }

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "AutoRest", "crash.log");
            }
        }

        public static void Write(string step)
        {
            Write(step, null);
        }

        public static void Write(string step, Exception ex)
        {
            try
            {
                lock (SyncRoot)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("------------------------------------------------------------");
                    sb.AppendLine("时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    sb.AppendLine("步骤: " + step);

                    if (ex != null)
                    {
                        int depth = 0;
                        Exception cur = ex;
                        while (cur != null && depth < 6)
                        {
                            sb.AppendLine("异常[" + depth + "]: " + cur.GetType().FullName);
                            sb.AppendLine("消息: " + cur.Message);
                            sb.AppendLine("堆栈: " + cur.StackTrace);
                            cur = cur.InnerException;
                            depth++;
                        }
                    }

                    string path = LogPath;
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // 日志自身失败绝不再抛异常
            }
        }

        public static string DescribeEnvironment()
        {
            var sb = new StringBuilder();
            try
            {
                sb.Append("OS=" + Environment.OSVersion);
                sb.Append(" | CLR=" + Environment.Version);
                sb.Append(" | 64位进程=" + Environment.Is64BitProcess);
                sb.Append(" | 64位系统=" + Environment.Is64BitOperatingSystem);
            }
            catch
            {
            }
            return sb.ToString();
        }
    }
}
