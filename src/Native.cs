// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
using System.Text;
namespace IdleReminder;
internal static class Native {
 [StructLayout(LayoutKind.Sequential)] internal struct Input {public uint Size,Time;}
 [DllImport("user32.dll",SetLastError=true)] static extern bool GetLastInputInfo(ref Input input);
 [DllImport("user32.dll")] static extern IntPtr OpenInputDesktop(uint flags,bool inherit,uint access);
 [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr desktop);
 [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr h,out Rect r);
 [StructLayout(LayoutKind.Sequential)] struct Rect {public int L,T,R,B;}
 internal static uint LastInput(){var i=new Input{Size=8};if(!GetLastInputInfo(ref i))throw new System.ComponentModel.Win32Exception();return i.Time;}
 internal static bool Accessible(){var h=OpenInputDesktop(0,false,0x100);if(h==IntPtr.Zero)return false;CloseDesktop(h);return true;}
 internal static bool Fullscreen(){var h=GetForegroundWindow();if(!GetWindowRect(h,out var r))return true;var b=Screen.FromHandle(h).Bounds;return r.L<=b.Left&&r.T<=b.Top&&r.R>=b.Right&&r.B>=b.Bottom;}
}
