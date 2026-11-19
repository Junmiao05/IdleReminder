// SPDX-License-Identifier: GPL-3.0-only
// Copyright (C) 2026 君喵
namespace IdleReminder;

internal sealed class ThemedTabControl : TabControl
{
    private const ControlStyles CustomPaint = ControlStyles.UserPaint |
        ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
        ControlStyles.ResizeRedraw;
    private const int HorizontalScrollMessage = 0x0114;
    private const TextFormatFlags TabTextFlags = TextFormatFlags.HorizontalCenter |
        TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix |
        TextFormatFlags.NoPadding | TextFormatFlags.PreserveGraphicsClipping;
    private Color surround = SystemColors.Control;
    private Color selectedBackground = SystemColors.Window;
    private Color selectedText = SystemColors.WindowText;
    private Color unselectedBackground = SystemColors.Control;
    private Color unselectedText = SystemColors.ControlText;

    internal void SetColors(IReadOnlyDictionary<string, string> colors)
    {
        if (!colors.TryGetValue("TabSelectedBackground", out var selectedColor))
        {
            SetStyle(CustomPaint, false);
            DrawMode = TabDrawMode.Normal;
            Invalidate(true);
            return;
        }

        surround = ColorTranslator.FromHtml(colors["Window"]);
        selectedBackground = ColorTranslator.FromHtml(selectedColor);
        selectedText = ColorTranslator.FromHtml(colors["TabSelectedText"]);
        unselectedBackground = ColorTranslator.FromHtml(colors["TabUnselectedBackground"]);
        unselectedText = ColorTranslator.FromHtml(colors["TabUnselectedText"]);
        SetStyle(CustomPaint, true);
        DrawMode = TabDrawMode.OwnerDrawFixed;
        Invalidate(true);
    }

    protected override void OnSelectedIndexChanged(EventArgs e)
    {
        base.OnSelectedIndexChanged(e);
        if (DrawMode == TabDrawMode.OwnerDrawFixed)
            Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        ApplyNativeFont(() => base.OnHandleCreated(e));
    }

    protected override void OnFontChanged(EventArgs e)
    {
        ApplyNativeFont(() => base.OnFontChanged(e));
    }

    private void ApplyNativeFont(Action apply)
    {
        // WinForms skips setting the HWND font for user-painted controls.
        // The native tab strip still measures item bounds and needs that font.
        bool customPaint = GetStyle(ControlStyles.UserPaint);
        SetStyle(ControlStyles.UserPaint, false);
        try
        {
            apply();
        }
        finally
        {
            SetStyle(ControlStyles.UserPaint, customPaint);
        }
    }

    protected override void WndProc(ref Message message)
    {
        base.WndProc(ref message);
        if (message.Msg == HorizontalScrollMessage && GetStyle(ControlStyles.UserPaint))
            Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // The native page window paints its own background. Paint the surround in OnPaint.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var state = e.Graphics.Save();
        if (SelectedTab is { } page)
            e.Graphics.ExcludeClip(page.Bounds);
        using (var brush = new SolidBrush(surround))
            e.Graphics.FillRectangle(brush, ClientRectangle);
        for (int index = 0; index < TabCount; index++)
            DrawTab(e.Graphics, index, GetTabRect(index));
        e.Graphics.Restore(state);
        base.OnPaint(e);
    }

    private void DrawTab(Graphics graphics, int index, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || !bounds.IntersectsWith(ClientRectangle))
            return;
        bool selected = index == SelectedIndex;
        Color background = selected ? selectedBackground : unselectedBackground;
        Color foreground = selected ? selectedText : unselectedText;
        using var brush = new SolidBrush(background);
        graphics.FillRectangle(brush, bounds);
        TextRenderer.DrawText(graphics, TabPages[index].Text, Font, bounds, foreground, TabTextFlags);

        if (selected && Focused && ShowFocusCues)
            ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(bounds, -2, -2),
                foreground, background);
    }
}
