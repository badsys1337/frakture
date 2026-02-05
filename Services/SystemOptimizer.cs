using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using TriggerLAG.Core;

namespace TriggerLAG;

public class SystemOptimizer
{
    private CancellationTokenSource? _fortniteOptCts;

    public void ApplyTcpRegistryTweak(string valueName, int value)
    {
        try
        {
            using (var interfacesKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces", true))
            {
                if (interfacesKey == null) return;

                foreach (var subKeyName in interfacesKey.GetSubKeyNames())
                {
                    using (var subKey = interfacesKey.OpenSubKey(subKeyName, true))
                    {
                        subKey?.SetValue(valueName, value, RegistryValueKind.DWord);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorLogger.Log($"Failed to apply registry tweak {valueName}", ex);
            throw; 
        }
    }

    public void FlushDns()
    {
        RunCommand("ipconfig", "/flushdns");
    }

    public void ResetWinsock()
    {
        RunCommand("netsh", "winsock reset");
    }

    public void ApplyFortniteQosPolicy(bool enable)
    {
        string policyName = "ClumsyFortnite";
        string appName = "FortniteClient-Win64-Shipping.exe";

        if (enable)
        {
            RunCommand("powershell", $"-Command \"Remove-NetQosPolicy -Name '{policyName}' -Confirm:$false; New-NetQosPolicy -Name '{policyName}' -AppPathNameMatchCondition '{appName}' -NetworkProfile All -ThrottleRateActionNone -DSCPAction 46\"");
        }
        else
        {
            RunCommand("powershell", $"-Command \"Remove-NetQosPolicy -Name '{policyName}' -Confirm:$false\"");
        }
    }

    public void StartFortniteOptimization()
    {
        if (_fortniteOptCts != null) return;

        _fortniteOptCts = new CancellationTokenSource();
        var token = _fortniteOptCts.Token;

        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var processes = Process.GetProcessesByName("FortniteClient-Win64-Shipping");
                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (proc.PriorityClass != ProcessPriorityClass.High)
                                proc.PriorityClass = ProcessPriorityClass.High;

                            if (!proc.PriorityBoostEnabled)
                                proc.PriorityBoostEnabled = true;
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.Log("Failed to set process priority for Fortnite", ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Log("Failed to get Fortnite process", ex);
                }

                try
                {
                    await Task.Delay(5000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, token);
    }

    public void StopFortniteOptimization()
    {
        if (_fortniteOptCts != null)
        {
            _fortniteOptCts.Cancel();
            _fortniteOptCts.Dispose();
            _fortniteOptCts = null;
        }
    }

    private void RunCommand(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas" 
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ErrorLogger.Log($"Command failed: {fileName} {arguments}", ex);
            throw;
        }
    }
}