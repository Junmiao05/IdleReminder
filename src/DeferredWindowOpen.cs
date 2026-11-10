// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
// Defer showing a window until the current tray/menu event has finished.
internal sealed class DeferredWindowOpen:IDisposable {
 readonly Control dispatcher;readonly Action open;bool queued,disposed;
 internal DeferredWindowOpen(Control dispatcher,Action open){this.dispatcher=dispatcher;this.open=open;}
 internal void Request(){
  if(disposed||queued||dispatcher.IsDisposed)return;
  queued=true;
  try{dispatcher.BeginInvoke((Action)(()=>{try{if(!disposed&&!dispatcher.IsDisposed)open();}finally{queued=false;}}));}
  catch(InvalidOperationException){queued=false;if(!disposed&&!dispatcher.IsDisposed)throw;}
 }
 public void Dispose(){disposed=true;}
}
