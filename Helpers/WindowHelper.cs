using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MES.Solution.Helpers
{
    public static class WindowHelper
    {
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;
        private const int WS_MINIMIZEBOX = 0x20000;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public static void RemoveMinimizeMaximizeButtons(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            int style = GetWindowLong(hwnd, GWL_STYLE);
            style &= ~(WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            SetWindowLong(hwnd, GWL_STYLE, style);
        }
    }
}