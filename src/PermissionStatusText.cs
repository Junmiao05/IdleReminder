// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class PermissionStatusText {
 public static string Get(bool? admin,bool device)=>admin switch {
  true=>"当前已以管理员权限运行，无需重复申请。",
  false=>"当前未以管理员权限运行。"+(device?"\n设备计时在高权限软件中可能无法接收输入并回退Windows计时；如需提权，请再次点击“保存设置”，关闭保存提示后确认UAC。":"\nWindows输入计时无需管理员权限；选择设备计时并保存时可申请。"),
  null=>"当前管理员权限状态无法确认；请核对运行权限，勿仅凭未看到UAC判断。"
 };
}
