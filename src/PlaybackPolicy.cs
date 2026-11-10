// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;
internal enum AudioPurpose { Reminder, Preview }
internal static class PlaybackPolicy {
 internal static bool ShouldStopForInput(bool playing, AudioPurpose purpose, ulong baseline, ulong current)
  => playing && purpose == AudioPurpose.Reminder && current != baseline;
}

