// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
public static class SlideHelperProtocol {
 public static bool ValidName(string name)=>name.Length==48&&name.StartsWith("IdleReminderPpt-",StringComparison.Ordinal)&&name[16..].All(Uri.IsHexDigit);
 public static string Encode(int? count)=>count is >=0 and <=10000?count.Value.ToString(System.Globalization.CultureInfo.InvariantCulture):"?";
 public static int? Decode(string text)=>text.Length<=5&&int.TryParse(text,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out int value)&&value is >=0 and <=10000?value:null;
}
