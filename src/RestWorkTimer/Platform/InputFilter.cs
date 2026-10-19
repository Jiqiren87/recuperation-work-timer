namespace RestWorkTimer.Platform;

/// <summary>
/// Flags documented for KBDLLHOOKSTRUCT / MSLLHOOKSTRUCT.
/// Kept as a pure helper so the injected-input rules have unit tests.
/// </summary>
public static class InputFilter
{
    // KBDLLHOOKSTRUCT.flags
    public const uint LLKHF_EXTENDED = 0x01;
    public const uint LLKHF_LOWER_IL_INJECTED = 0x02;
    public const uint LLKHF_INJECTED = 0x10;

    // MSLLHOOKSTRUCT.flags
    public const uint LLMHF_INJECTED = 0x01;
    public const uint LLMHF_LOWER_IL_INJECTED = 0x02;

    public static bool IsInjectedKeyboard(uint flags)
        => (flags & (LLKHF_INJECTED | LLKHF_LOWER_IL_INJECTED)) != 0;

    public static bool IsInjectedMouse(uint flags)
        => (flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0;
}
