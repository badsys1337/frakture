using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using TriggerLAG.Core;

namespace TriggerLAG;

public class NetworkEngine
{
    public event Action<bool>? StatusChanged;
    public event Action<string>? ErrorOccurred;

    private volatile bool _running = false;
    public bool IsRunning => _running;

    private IntPtr _handle = IntPtr.Zero;
    private Task? _workerTask;
    private Task? _senderTask;
    private CancellationTokenSource? _cts;
    
    private BlockingCollection<LagPacket> _lagQueue = new(10000); 

    public volatile int Intensity = 50;
    public volatile bool IsDropMode = false;
    public volatile bool IsStaticLag = false;
    public volatile bool IsBulletStacking = false; 
    public volatile bool IsManualMode = false;
    public volatile bool IsSnapshotTrap = false;
    public volatile bool IsVelocitySpoof = false;
    public volatile bool IsPhasePeak = false;
    public volatile bool IsAdaptiveLag = false;

    public volatile bool IsInvisiblePeek = false; 
    public volatile bool IsDelayPeek = false;
    public volatile int DelayPeekDuration = 500;
    
    private long _snapshotBaseTick = 0;
    private long _adaptivePhaseEndTick = 0;
    private volatile bool _adaptiveLagPhase = false;
    private readonly object _adaptiveLock = new();
    
    public bool Inbound { get; set; } = false;
    public bool Outbound { get; set; } = true;
    public bool FortniteOnly { get; set; } = false;
    public bool HighPriority { get; set; } = false;

    private struct LagPacket
    {
        public byte[] Data;
        public uint Length;
        public byte[] Addr;
        public long ReleaseTick;
    }

    public void Start()
    {
        if (_running) return;

        string filter = BuildFilter(out bool errorOccurred);
        if (errorOccurred) return;

        if (string.IsNullOrEmpty(filter))
        {
            ErrorOccurred?.Invoke("Select at least one direction.");
            return;
        }

        try
        {
            short priority = (short)(HighPriority ? 3000 : 0); 

            string baseDir = GetExecutableDirectory();
            string sysName, dllPath, sysPath, initError;
            
            if (!TryPrepareWinDivert(baseDir, out sysName, out dllPath, out sysPath, out initError))
            {
                ErrorOccurred?.Invoke(initError);
                return;
            }

            _handle = WinDivert.WinDivertOpen(filter, WinDivert.WINDIVERT_LAYER_NETWORK, priority, 0);

            if ((_handle == IntPtr.Zero || _handle == new IntPtr(-1)) && Marshal.GetLastWin32Error() == 87 && priority > 1000)
            {
                priority = 1000;
                _handle = WinDivert.WinDivertOpen(filter, WinDivert.WINDIVERT_LAYER_NETWORK, priority, 0);
            }

            if (_handle == IntPtr.Zero || _handle == new IntPtr(-1))
            {
                int err = Marshal.GetLastWin32Error();
                string msg = $"Failed to open WinDivert (Error: {err}).";
                if (err == 5) msg += "\nAccess Denied. Run as Admin.";
                ErrorOccurred?.Invoke(msg);
                return;
            }

            _running = true;
            _snapshotBaseTick = 0;
            
            _lagQueue = new BlockingCollection<LagPacket>(10000);
            
            _cts = new CancellationTokenSource();
            StatusChanged?.Invoke(true);

            _workerTask = Task.Factory.StartNew(() => WorkerLoop(_cts.Token), TaskCreationOptions.LongRunning);
            _senderTask = Task.Factory.StartNew(() => SenderLoop(_cts.Token), TaskCreationOptions.LongRunning);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("Start Error", ex);
            ErrorOccurred?.Invoke($"Error: {ex.Message}");
            Stop();
        }
    }

    private void UpdateAdaptivePhase(long now)
    {
        lock (_adaptiveLock)
        {
            if (now < _adaptivePhaseEndTick) return;
            if (_adaptiveLagPhase)
            {
                int offMs = Random.Shared.Next(180, 420);
                _adaptiveLagPhase = false;
                _adaptivePhaseEndTick = now + offMs;
            }
            else
            {
                int onBase = Math.Clamp(Intensity * 8, 600, 2200);
                int jitter = onBase / 6;
                int onMs = onBase + Random.Shared.Next(-jitter, jitter + 1);
                _adaptiveLagPhase = true;
                _adaptivePhaseEndTick = now + onMs;
            }
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;

        _cts?.Cancel();
        _lagQueue?.CompleteAdding();

        try 
        {
            var tasks = new[] { _workerTask, _senderTask }.Where(t => t != null).Cast<Task>().ToArray();
            if (tasks.Length > 0) Task.WaitAll(tasks, 250);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("Error waiting for tasks in Stop()", ex);
        }

        if (_handle != IntPtr.Zero && _handle != new IntPtr(-1))
        {
            WinDivert.WinDivertClose(_handle);
            _handle = IntPtr.Zero;
        }

        while (_lagQueue != null && _lagQueue.TryTake(out _)) { }
        
        StatusChanged?.Invoke(false);
    }

    public void Toggle()
    {
        if (_running) Stop();
        else Start();
    }

    private void WorkerLoop(CancellationToken token)
    {
        try
        {
            byte[] packet = new byte[65535];
            byte[] addr = new byte[100];
            var rnd = new Random();

            while (_running && !token.IsCancellationRequested)
            {
                if (!WinDivert.WinDivertRecv(_handle, packet, (uint)packet.Length, out uint readLen, addr))
                {
                    Thread.Sleep(1);
                    continue;
                }

                int value = Intensity;
                bool outbound = WinDivert.IsOutbound(addr);
                long now = Environment.TickCount64;
                
                if (IsDropMode)
                {
                    if (rnd.Next(100) >= value)
                    {
                        SendPacket(_handle, packet, readLen, addr);
                    }
                    continue;
                }

                bool shouldLag = false;
                long releaseTick = 0;

                if (IsStaticLag)
                {
                    shouldLag = true;
                    releaseTick = now + value;
                }
                else if (IsManualMode || IsBulletStacking || IsPhasePeak) 
                {
                    shouldLag = true;
                    releaseTick = long.MaxValue; 
                }
                else if (IsInvisiblePeek || IsDelayPeek)
                {
                    if (!outbound)
                    {
                        shouldLag = true;
                        releaseTick = long.MaxValue; 
                    }
                    else
                    {
                        shouldLag = false; 
                    }
                }
                else if (IsSnapshotTrap)
                {
                    if (!outbound)
                    {
                        long window = 200;
                        if (_snapshotBaseTick == 0) _snapshotBaseTick = now;
                        releaseTick = _snapshotBaseTick + ((now - _snapshotBaseTick) / window + 1) * window;
                        shouldLag = true;
                    }
                }
                else if (IsVelocitySpoof)
                {
                    if (outbound)
                    {
                        releaseTick = now + Random.Shared.Next(10, 150);
                        shouldLag = true;
                    }
                }
                else if (IsAdaptiveLag)
                {
                    UpdateAdaptivePhase(now);
                    if (_adaptiveLagPhase)
                    {
                        releaseTick = _adaptivePhaseEndTick;
                        shouldLag = true;
                    }
                }

                if (shouldLag && !_lagQueue.IsAddingCompleted)
                {
                    byte[] pData = new byte[readLen];
                    byte[] aData = new byte[addr.Length];
                    Array.Copy(packet, pData, readLen);
                    Array.Copy(addr, aData, addr.Length);

                    if (!_lagQueue.TryAdd(new LagPacket
                    {
                        Data = pData,
                        Length = readLen,
                        Addr = aData,
                        ReleaseTick = releaseTick
                    }))
                    {
                        SendPacket(_handle, packet, readLen, addr);
                    }
                }
                else
                {
                    SendPacket(_handle, packet, readLen, addr);
                }
            }
        }
        catch (Exception ex) 
        { 
            if (!token.IsCancellationRequested)
                ErrorLogger.Log("WorkerLoop Exception", ex); 
        }
    }

    private void SenderLoop(CancellationToken token)
    {
        try
        {
            foreach (var item in _lagQueue.GetConsumingEnumerable(token))
            {
                long now = Environment.TickCount64;
                long releaseTick = item.ReleaseTick;

                if (releaseTick == long.MaxValue)
                {
                    while (_running && !token.IsCancellationRequested)
                    {
                        bool stillHolding = false;
                        
                        if (IsManualMode) stillHolding = true;
                        if (IsBulletStacking) stillHolding = true; 
                        if (IsPhasePeak) stillHolding = true;
                        
                        bool isOutbound = WinDivert.IsOutbound(item.Addr);
                        if ((IsInvisiblePeek || IsDelayPeek) && !isOutbound) stillHolding = true;
                        if (IsAdaptiveLag && _adaptiveLagPhase) stillHolding = true;

                        if (!stillHolding) break;
                        
                        Thread.Sleep(10); 
                    }
                }
                else if (releaseTick > now)
                {
                    int wait = (int)(releaseTick - now);
                    if (wait > 2000) wait = 2000; 
                    if (wait > 0)
                    {
                            Thread.Sleep(wait);
                    }
                }

                if (_handle != IntPtr.Zero)
                {
                    SendPacket(_handle, item.Data, item.Length, item.Addr);
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { ErrorLogger.Log("SenderLoop Exception", ex); }
    }

    private void SendPacket(IntPtr handle, byte[] packet, uint len, byte[] addr)
    {
        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
        WinDivert.WinDivertHelperCalcChecksums(packet, len, addr, 0);
        WinDivert.WinDivertSend(handle, packet, len, out _, addr);
    }

    private string BuildFilter(out bool errorOccurred)
    {
        errorOccurred = false;
        string directionFilter = "";

        if (Inbound && Outbound) directionFilter = "true";
        else if (Inbound) directionFilter = "inbound";
        else if (Outbound) directionFilter = "outbound";
        else return "";

        if (FortniteOnly)
        {
            try
            {
                bool processFound;
                var ports = GetFortnitePorts(out processFound);
                if (ports.Count == 0)
                {
                    if (!processFound) 
                    {
                        ErrorOccurred?.Invoke("Fortnite not found. Launch the game first.");
                        errorOccurred = true;
                        return "";
                    }
                    errorOccurred = true; 
                    return "";
                }

                var portConditions = ports.Select(p => $"(udp.SrcPort == {p} or udp.DstPort == {p})");
                string portFilter = string.Join(" or ", portConditions);
                return $"({portFilter}) and ({directionFilter})";
            }
            catch
            {
                errorOccurred = true;
                return "";
            }
        }

        return directionFilter;
    }

    private static string GetExecutableDirectory()
    {
        try { return Path.GetDirectoryName(Environment.ProcessPath) ?? Directory.GetCurrentDirectory(); }
        catch { return Directory.GetCurrentDirectory(); }
    }

    private static bool TryPrepareWinDivert(string baseDir, out string sysName, out string dllPath, out string sysPath, out string error)
    {
        sysName = Environment.Is64BitOperatingSystem ? "WinDivert64.sys" : "WinDivert32.sys";
        dllPath = Path.Combine(baseDir, "WinDivert.dll");
        sysPath = Path.Combine(baseDir, sysName);
        error = "";

        if (!File.Exists(dllPath)) { error = "WinDivert.dll missing"; return false; }
        if (!File.Exists(sysPath))
        {
            var candidate = Directory.GetFiles(baseDir, "WinDivert*.sys").FirstOrDefault();
            if (candidate != null) try { File.Copy(candidate, sysPath, true); } catch (Exception ex) { ErrorLogger.Log("Failed to copy WinDivert driver", ex); }
        }
        if (!File.Exists(sysPath)) { error = "WinDivert driver missing"; return false; }

        try { NativeMethods.SetDllDirectory(baseDir); } catch (Exception ex) { ErrorLogger.Log("Failed to set DLL directory", ex); }
        try { NativeLibrary.Load(dllPath); } catch (Exception ex) { ErrorLogger.Log($"Failed to load {dllPath}", ex); return false; }
        return true;
    }

    private List<int> GetFortnitePorts(out bool processFound)
    {
        processFound = false;
        var ports = new List<int>();
        var processes = Process.GetProcessesByName("FortniteClient-Win64-Shipping");

        if (processes.Length == 0) return ports;
        processFound = true;

        try
        {
            var udpTable = GetUdpTable();
            foreach (var proc in processes)
            {
                int pid = proc.Id;
                foreach (var row in udpTable)
                {
                    if (row.owningPid == pid)
                    {
                        int port = (row.localPort[0] << 8) + row.localPort[1];
                        if (!ports.Contains(port)) ports.Add(port);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.Log("Error in GetFortnitePorts", ex);
        }
        return ports;
    }

    private List<NativeMethods.MIB_UDPROW_OWNER_PID> GetUdpTable()
    {
        int buffSize = 0;
        NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref buffSize, false, NativeMethods.AF_INET, NativeMethods.UDP_TABLE_OWNER_PID, 0);
        IntPtr buffer = Marshal.AllocHGlobal(buffSize);
        try
        {
            uint ret = NativeMethods.GetExtendedUdpTable(buffer, ref buffSize, false, NativeMethods.AF_INET, NativeMethods.UDP_TABLE_OWNER_PID, 0);
            if (ret != 0) return new List<NativeMethods.MIB_UDPROW_OWNER_PID>();

            int numEntries = Marshal.ReadInt32(buffer);
            IntPtr tablePtr = IntPtr.Add(buffer, 4);
            var table = new List<NativeMethods.MIB_UDPROW_OWNER_PID>();
            int rowSize = Marshal.SizeOf(typeof(NativeMethods.MIB_UDPROW_OWNER_PID));

            for (int i = 0; i < numEntries; i++)
            {
                table.Add(Marshal.PtrToStructure<NativeMethods.MIB_UDPROW_OWNER_PID>(tablePtr));
                tablePtr = IntPtr.Add(tablePtr, rowSize);
            }
            return table;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }


}
