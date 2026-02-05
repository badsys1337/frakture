using System;
using System.Runtime.InteropServices;

namespace TriggerLAG
{
    public static class WinDivert
    {
        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr WinDivertOpen(string filter, int layer, short priority, ulong flags);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool WinDivertRecv(IntPtr handle, byte[] packet, uint packetLen, out uint readLen, byte[] addr);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool WinDivertSend(IntPtr handle, byte[] packet, uint packetLen, out uint sendLen, byte[] addr);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool WinDivertClose(IntPtr handle);

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        public static extern bool WinDivertSetParam(IntPtr handle, int param, ulong value);
        
        public const int WINDIVERT_LAYER_NETWORK = 0;
        public const int WINDIVERT_PARAM_QUEUE_LENGTH = 0;
        public const int WINDIVERT_PARAM_QUEUE_TIME = 1;

        [DllImport("WinDivert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern uint WinDivertHelperCalcChecksums(byte[] packet, uint packetLen, byte[] addr, ulong flags);
        
        
        public static bool IsOutbound(byte[] addr)
        {
            if (addr == null || addr.Length < 80) return false;
            
            
            
            
            return (addr[10] & 0x02) != 0;
        }
    }
}
