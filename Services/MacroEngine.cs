using System;
using System.Threading;
using System.Threading.Tasks;
using TriggerLAG.Core;

namespace TriggerLAG;

public class MacroEngine
{
    public int MacroEditKey { get; set; }
    public int MacroConfirmKey { get; set; }
    public int MacroEditKeyMouse { get; set; }
    public int MacroConfirmKeyMouse { get; set; }
    public int MacroDelay { get; set; } = 30;
    public int MacroDuration { get; set; } = 30;
    public int DoubleEditMode { get; set; } = 1; 

    public int ItemCollectBindKey { get; set; }
    public int ItemCollectBindMouseBtn { get; set; }
    public int ItemCollectMode { get; set; } = 1; 

    private volatile bool _doubleEditRunning = false;
    private CancellationTokenSource? _doubleEditCts;

    private volatile bool _itemCollectRunning = false;
    private CancellationTokenSource? _itemCollectCts;

    public bool IsDoubleEditRunning => _doubleEditRunning;
    public bool IsItemCollectRunning => _itemCollectRunning;

    public void StartDoubleEditLoop()
    {
        if (_doubleEditRunning) return;

        bool hasEdit = MacroEditKey != 0 || MacroEditKeyMouse != 0;
        bool hasConfirm = MacroConfirmKey != 0 || MacroConfirmKeyMouse != 0;
        if (!hasEdit || !hasConfirm) return;

        _doubleEditRunning = true;
        _doubleEditCts = new CancellationTokenSource();
        var token = _doubleEditCts.Token;

        Task.Run(async () => {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (MacroEditKey != 0) InputSimulator.PressKey(MacroEditKey, MacroDuration);
                    else if (MacroEditKeyMouse != 0) InputSimulator.ClickMouse(MacroEditKeyMouse, MacroDuration);

                    if (token.IsCancellationRequested) break;
                    await Task.Delay(Math.Max(1, MacroDelay), token);
                    if (token.IsCancellationRequested) break;

                    if (MacroConfirmKey != 0) InputSimulator.PressKey(MacroConfirmKey, MacroDuration);
                    else if (MacroConfirmKeyMouse != 0) InputSimulator.ClickMouse(MacroConfirmKeyMouse, MacroDuration);

                    if (token.IsCancellationRequested) break;
                    await Task.Delay(50, token); 
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("Exception in Double Edit Loop", ex);
            }
            finally
            {
                _doubleEditRunning = false;
            }
        }, token);
    }

    public void StopDoubleEditLoop()
    {
        if (_doubleEditCts != null)
        {
            _doubleEditCts.Cancel();
            _doubleEditCts.Dispose();
            _doubleEditCts = null;
        }
        _doubleEditRunning = false;
    }

    public void StartItemCollectLoop()
    {
        if (_itemCollectRunning) return;

        bool hasBind = ItemCollectBindKey != 0 || ItemCollectBindMouseBtn != 0;
        if (!hasBind) return;

        _itemCollectRunning = true;
        _itemCollectCts = new CancellationTokenSource();
        var token = _itemCollectCts.Token;

        Task.Run(async () => {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (ItemCollectBindKey != 0) InputSimulator.PressKey(ItemCollectBindKey, 30);
                    else if (ItemCollectBindMouseBtn != 0) InputSimulator.ClickMouse(ItemCollectBindMouseBtn, 30);

                    if (token.IsCancellationRequested) break;
                    await Task.Delay(10, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("Exception in Item Collect Loop", ex);
            }
            finally
            {
                _itemCollectRunning = false;
            }
        }, token);
    }

    public void StopItemCollectLoop()
    {
        if (_itemCollectCts != null)
        {
            _itemCollectCts.Cancel();
            _itemCollectCts.Dispose();
            _itemCollectCts = null;
        }
        _itemCollectRunning = false;
    }
}