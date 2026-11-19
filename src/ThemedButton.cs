// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;

internal sealed class ThemedButton : Button
{
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Enabled || FlatStyle != FlatStyle.Flat || UseVisualStyleBackColor)
        {
            base.OnPaint(e);
            return;
        }

        // The WinForms disabled-button renderer substitutes its own text color.
        using var background = new SolidBrush(BackColor);
        e.Graphics.FillRectangle(background, ClientRectangle);
        using var border = new Pen(FlatAppearance.BorderColor);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
        if (!UseMnemonic) flags |= TextFormatFlags.NoPrefix;
        else if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
        TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, ForeColor, flags);
    }
}
