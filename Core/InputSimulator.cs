using System;
using System.Runtime.InteropServices;

namespace TriggerLAG
{
    public static class InputSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_MOUSE = 0;
        private const int INPUT_KEYBOARD = 1;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;

        public static void PressKey(int vkCode, int duration = 30)
        {
            var inputDown = new INPUT();
            inputDown.type = INPUT_KEYBOARD;
            inputDown.u.ki.wVk = (ushort)vkCode;
            inputDown.u.ki.dwFlags = 0; 

            var inputUp = new INPUT();
            inputUp.type = INPUT_KEYBOARD;
            inputUp.u.ki.wVk = (ushort)vkCode;
            inputUp.u.ki.dwFlags = KEYEVENTF_KEYUP;

            INPUT[] inputsDown = { inputDown };
            INPUT[] inputsUp = { inputUp };

            SendInput(1, inputsDown, Marshal.SizeOf(typeof(INPUT)));
            System.Threading.Thread.Sleep(duration); 
            SendInput(1, inputsUp, Marshal.SizeOf(typeof(INPUT)));
        }

        public static void ClickMouse(int buttonCode, int duration = 30)
        {
            
            
            uint downFlag = 0;
            uint upFlag = 0;
            uint mouseData = 0;

            switch (buttonCode)
            {
                case 3: 
                    downFlag = MOUSEEVENTF_LEFTDOWN;
                    upFlag = MOUSEEVENTF_LEFTUP;
                    break;
                case 4: 
                    downFlag = MOUSEEVENTF_RIGHTDOWN;
                    upFlag = MOUSEEVENTF_RIGHTUP;
                    break;
                case 5: 
                    downFlag = MOUSEEVENTF_MIDDLEDOWN;
                    upFlag = MOUSEEVENTF_MIDDLEUP;
                    break;
                case 1: 
                    downFlag = MOUSEEVENTF_XDOWN;
                    upFlag = MOUSEEVENTF_XUP;
                    mouseData = 1;
                    break;
                case 2: 
                    downFlag = MOUSEEVENTF_XDOWN;
                    upFlag = MOUSEEVENTF_XUP;
                    mouseData = 2;
                    break;
                default:
                    return;
            }

            var inputDown = new INPUT();
            inputDown.type = INPUT_MOUSE;
            inputDown.u.mi.dwFlags = downFlag;
            inputDown.u.mi.mouseData = mouseData;

            var inputUp = new INPUT();
            inputUp.type = INPUT_MOUSE;
            inputUp.u.mi.dwFlags = upFlag;
            inputUp.u.mi.mouseData = mouseData;

            INPUT[] inputsDown = { inputDown };
            INPUT[] inputsUp = { inputUp };

            SendInput(1, inputsDown, Marshal.SizeOf(typeof(INPUT)));
            System.Threading.Thread.Sleep(duration); 
            SendInput(1, inputsUp, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
