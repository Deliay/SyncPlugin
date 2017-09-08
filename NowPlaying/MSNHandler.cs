﻿using Sync.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NowPlaying
{
    public struct StatusChangeEvent : IBaseEvent
    {
        public OSUStatus CurrentStatus { get; private set; }
        public StatusChangeEvent(OSUStatus status)
        {
            CurrentStatus = status;
        }
    }

    public class NowPlayingEvents : BaseEventDispatcher
    {
        public static readonly NowPlayingEvents Instance = new NowPlayingEvents();
        private NowPlayingEvents()
        {
            EventDispatcher.Instance.RegisterNewDispatcher(GetType());
        }
    }

    [Obsolete("Instead with standrad event dispatcher", true)]
    public interface IOSUStatus
    {
        string artist { get; set; }
        string title { get; set; }
        string diff { get; set; }
        string status { get; set; }
        string prefix { get; set; }
        string mode { get; set; }

    }

    public struct OSUStatus
    {
        public string artist { get; set; }
        public string title { get; set; }
        public string diff { get; set; }
        public string status { get; set; }
        public string prefix { get; set; }
        public string mode { get; set; }

        public static implicit operator OSUStatus(string[] arr)
        {
            try
            {
                string[] result = null;
                if (arr.Length == 2)
                {
                    result = arr[0].Replace("\\0", "\\").Split(new[] { '\\' } ,StringSplitOptions.RemoveEmptyEntries);
                    if(result.Length < 6)
                    {
                        return new OSUStatus();
                    }
                    OSUStatus obj = new OSUStatus
                    {
                        prefix = result[0],
                        status = result[2].Split(' ')[0],
                        artist = result[4],
                        title = result[3],
                        mode = result[5]
                    };
                    if(result.Length == 7)
                    {
                        obj.diff = result[6];
                    }
                    return obj;

                }
                else
                {
                    return new OSUStatus();
                }
            }
            catch (Exception e)
            {
                Sync.Tools.IO.CurrentIO.WriteColor("错误:" + e.Message, ConsoleColor.Red);
                return new OSUStatus();
            }
        }
    }

    [Obsolete("Instead with standrad event dispatcher", true)]
    public interface IMSNHandler
    {
        void registerCallback(Func<IOSUStatus, Task<bool>> callback);
    }

    public class MSNHandler
    {
        #region WIN32API Import
        private const string CONST_CLASS_NAME = "MsnMsgrUIManager";
        public delegate IntPtr WNDPROC(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WNDCLASS
        {
            public int style = 0;
            public WNDPROC lpfnWndProc = null;
            public int cbClsExtra = 0;
            public int cbWndExtra = 0;
            public IntPtr hInstance = IntPtr.Zero;
            public IntPtr hIcon = IntPtr.Zero;
            public IntPtr hCursor = IntPtr.Zero;
            public IntPtr hbrBackground = IntPtr.Zero;
            public string lpszMenuName = null;
            public string lpszClassName = null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct COPYDATASTRUCT
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr RegisterClass(WNDCLASS wc);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateWindowEx(int dwExStyle, string lpszClassName, string lpszWindowName, int style, int x, int y, int width, int height, IntPtr hWndParent, IntPtr hMenu, IntPtr hInst, IntPtr lpParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        #endregion

        private IntPtr m_hWnd;
        private WNDCLASS lpWndClass;
        private List<Func<OSUStatus, Task<bool>>> callbacks;
        private Thread t;

        public void Load()
        {
            callbacks = new List<Func<OSUStatus, Task<bool>>>();
            t = new Thread(CreateMSNWindow);
            t.SetApartmentState(ApartmentState.STA);
            t.IsBackground = true;
            t.Name = "ActiveXThread";
        }

        [Obsolete("Instead with standrad event dispatcher", true)]
        public void registerCallback(Func<IOSUStatus, Task<bool>> callback)
        {
            //callbacks.Add(callback);
        }

        public void StartHandler()
        {
            t.Start();
        }

        #region WIN32Form Implement
        [STAThread]
        private void CreateMSNWindow()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            lpWndClass = new WNDCLASS();
            lpWndClass.lpszClassName = CONST_CLASS_NAME;
            lpWndClass.lpfnWndProc = new WNDPROC(WndProc);

            if (RegisterClass(lpWndClass).ToInt32() == 0 && Marshal.GetLastWin32Error() != 1410)
            {
                Sync.Tools.IO.CurrentIO.WriteColor("无法注册MSN类", ConsoleColor.Red);
                return;
            }
            m_hWnd = CreateWindowEx(0, CONST_CLASS_NAME, string.Empty, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            if(m_hWnd.ToInt32() > 0)
            {
                Sync.Tools.IO.CurrentIO.WriteColor("MSN类注册成功！", ConsoleColor.Green);
            }

            Application.Run();
            return;
        }

        private bool DestoryMSNWindow()
        {
            if(m_hWnd.ToInt32() > 0)
            {
                return DestroyWindow(m_hWnd);
            }
            return true;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if(msg == 74)
            {
                COPYDATASTRUCT cb = (COPYDATASTRUCT)Marshal.PtrToStructure(lParam, typeof(COPYDATASTRUCT));
                string[] info = Marshal.PtrToStringUni(cb.lpData, cb.cbData / 2).Split("\0".ToCharArray(), StringSplitOptions.None);
                OSUStatus stats = info;
                NowPlayingEvents.Instance.RaiseEventAsync(new StatusChangeEvent(stats));
                //callbacks.ForEach(p => p(stats).Start());
            }

            return DefWindowProcW(hWnd, msg, wParam, lParam);
        }
        #endregion
    }
}
