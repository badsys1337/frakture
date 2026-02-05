using System;
using System.Runtime.InteropServices;

namespace TriggerLAG.Core
{
    public static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool SetDllDirectory(string lpPathName);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        public static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, int tblClass, int reserved);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int AF_INET = 2;
        public const int UDP_TABLE_OWNER_PID = 1;

        [StructLayout(LayoutKind.Sequential)]
        public struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] localPort;
            public int owningPid;
        }
    }
}