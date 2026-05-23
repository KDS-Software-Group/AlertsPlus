using System.Runtime.InteropServices;

namespace AlertsPlus
{
    // very confusing file that checks for a window name and then sends message when matched
    // used for restarting instances of the app when already open
    internal static class NativeMethods
    {
        public const uint WM_SHOWINSTANCE = 0x8001;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}