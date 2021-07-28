using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;

using Rhino;

namespace GH_BC.UI
{
  class WinAPI
  {
    [DllImport("USER32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsIconic(IntPtr hWnd);

    [DllImport("USER32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsZoomed(IntPtr hWnd);

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("USER32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    internal static bool ShowWindow(IntPtr hWnd, bool bShow) => ShowWindow(hWnd, bShow ? 8 : 0);

    [DllImport("USER32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool ShowOwnedPopups(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool fShow);
    internal static bool ShowOwnedPopups(bool fShow) => ShowOwnedPopups(RhinoApp.MainWindowHandle(), fShow);

    [DllImport("USER32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("USER32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EnableWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);
    internal static IntPtr GetEnabledPopup() => GetWindow(RhinoApp.MainWindowHandle(), 6 /*GW_ENABLEDPOPUP*/);

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr BringWindowToTop(IntPtr hWnd);

    [DllImport("USER32", SetLastError = true)]
    internal static extern IntPtr SetFocus(IntPtr hWnd);
  }
}
