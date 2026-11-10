// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
using System.Runtime.InteropServices;
namespace IdleReminder;
internal sealed class ForegroundInputPermission {
 int? ownLevel;bool ownRead;uint previousPid;int? previousLevel;
 internal InputAccessState Read(){
  if(!ownRead){ownLevel=Level((uint)Environment.ProcessId);ownRead=true;}
  GetWindowThreadProcessId(GetForegroundWindow(),out uint pid);
  if(pid==0)return InputAccessState.Unavailable;
  if(pid==(uint)Environment.ProcessId)return DeviceHeaderPolicy.Access(ownLevel,ownLevel);
  if(pid!=previousPid){previousPid=pid;previousLevel=Level(pid);}
  return DeviceHeaderPolicy.Access(ownLevel,previousLevel);
 }
 static int? Level(uint pid){
  IntPtr process=OpenProcess(0x1000,false,pid),token=IntPtr.Zero,buffer=IntPtr.Zero;
  if(process==IntPtr.Zero)return null;
  try{
   if(!OpenProcessToken(process,8,out token))return null;
   GetTokenInformation(token,25,IntPtr.Zero,0,out int size);
   if(size<=0||size>65536)return null;buffer=Marshal.AllocHGlobal(size);
   if(!GetTokenInformation(token,25,buffer,size,out size))return null;
   var sid=Marshal.ReadIntPtr(buffer);uint count=Marshal.ReadByte(GetSidSubAuthorityCount(sid));
   return count>0?Marshal.ReadInt32(GetSidSubAuthority(sid,count-1)):null;
  }finally{if(buffer!=IntPtr.Zero)Marshal.FreeHGlobal(buffer);if(token!=IntPtr.Zero)CloseHandle(token);CloseHandle(process);}
 }
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("kernel32.dll")]static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("advapi32.dll")]static extern bool OpenProcessToken(IntPtr process,uint access,out IntPtr token);
 [DllImport("advapi32.dll")]static extern bool GetTokenInformation(IntPtr token,int type,IntPtr data,int size,out int needed);
 [DllImport("advapi32.dll")]static extern IntPtr GetSidSubAuthorityCount(IntPtr sid);
 [DllImport("advapi32.dll")]static extern IntPtr GetSidSubAuthority(IntPtr sid,uint index);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
}
