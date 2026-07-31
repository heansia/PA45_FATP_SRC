using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCT.Utilities
{
    public static class ProcessHelper
    {
        public static bool ProcessStart(string fileName, string args, ref string output, ref string error, int outtime)
        {
            bool rlt = false;

            using (Process process = new Process())
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,  // 输出重定向
                    RedirectStandardError = true,   // 错误重定向（你要的）
                    CreateNoWindow = true
                };

                process.StartInfo = psi;
                StaticProcessStart?.Invoke(new ProcessStartEventArgs(psi.Arguments));

                try
                {
                    if (process.Start())
                    {
                        // 必须异步读取，否则会死锁！
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();

                        // 等待进程结束
                        if (outtime == 0) { process.WaitForExit(); }
                        else
                        {
                            bool exited = process.WaitForExit(outtime);
                            if (!exited)
                            {
                                process.Kill();
                                throw new TimeoutException("命令执行超时");
                            }
                        }

                        rlt = true;
                    }
                }
                catch
                {
                    rlt = false;
                    throw;
                }
            }

            return rlt;
        }

        public static bool CmdStart(string args, ref string output, ref string error, int outtime)
        {
            bool rlt = false;
            using (Process process = new Process())
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = GetCmdPath(),
                    Arguments = $"/c {args}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,  // 输出重定向
                    RedirectStandardError = true,   // 错误重定向（你要的）
                    CreateNoWindow = true
                };

                StaticProcessStart?.Invoke(new ProcessStartEventArgs(psi.Arguments));

                process.StartInfo = psi;

                try
                {
                    if (process.Start())
                    {
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();

                        if (outtime == 0) { process.WaitForExit(); }
                        else
                        {
                            bool exited = process.WaitForExit(outtime);
                            if (!exited)
                            {
                                process.Kill();
                                throw new TimeoutException("命令执行超时");
                            }
                        }

                        rlt = true;
                    }
                }
                catch
                {
                    rlt = false;
                    throw;
                }
            }

            return rlt;
        }

        public static string GetCmdPath()
        {
            string path = Environment.GetEnvironmentVariable("ComSpec");
            return string.IsNullOrEmpty(path) ? @"C:\Windows\System32\cmd.exe" : path;
        }

        public static event ProcessStartEventHandler StaticProcessStart;

    }
}
