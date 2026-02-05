using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TriggerLAG
{
    public class GlobalMouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        
        
        public const int MOUSE_X1 = 1;
        public const int MOUSE_X2 = 2;
        public const int MOUSE_LEFT = 3;
        public const int MOUSE_RIGHT = 4;
        public const int MOUSE_MIDDLE = 5;

        private readonly LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private Action<int, bool>? _onButtonAction; 
        public event Action<int, bool>? MouseEvent;

        public GlobalMouseHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        public void SetCallback(Action<int, bool> onButtonAction)
        {
            _onButtonAction = onButtonAction;
            MouseEvent += onButtonAction;
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule!)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = (int)wParam;
                int buttonCode = 0;
                bool isDown = false;

                if (msg == WM_LBUTTONDOWN) { buttonCode = MOUSE_LEFT; isDown = true; }
                else if (msg == WM_LBUTTONUP) { buttonCode = MOUSE_LEFT; isDown = false; }
                else if (msg == WM_RBUTTONDOWN) { buttonCode = MOUSE_RIGHT; isDown = true; }
                else if (msg == WM_RBUTTONUP) { buttonCode = MOUSE_RIGHT; isDown = false; }
                else if (msg == WM_MBUTTONDOWN) { buttonCode = MOUSE_MIDDLE; isDown = true; }
                else if (msg == WM_MBUTTONUP) { buttonCode = MOUSE_MIDDLE; isDown = false; }
                else if (msg == WM_XBUTTONDOWN)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    int xButton = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    buttonCode = xButton; 
                    isDown = true;
                }
                else if (msg == WM_XBUTTONUP)
                {
                    MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    int xButton = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
                    buttonCode = xButton; 
                    isDown = false;
                }

                if (buttonCode != 0)
                {
                    MouseEvent?.Invoke(buttonCode, isDown);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
