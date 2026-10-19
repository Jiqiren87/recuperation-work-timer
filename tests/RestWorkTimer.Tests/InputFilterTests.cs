using RestWorkTimer.Platform;
using Xunit;

namespace RestWorkTimer.Tests;

public sealed class InputFilterTests
{
    [Theory]
    [InlineData(0x10u, true)] // LLKHF_INJECTED
    [InlineData(0x02u, true)] // LLKHF_LOWER_IL_INJECTED
    [InlineData(0x12u, true)]
    [InlineData(0x01u, false)] // LLKHF_EXTENDED is still a real key
    [InlineData(0x00u, false)]
    public void KeyboardInjectedFlagsAreRejected(uint flags, bool expected)
    {
        Assert.Equal(expected, InputFilter.IsInjectedKeyboard(flags));
    }

    [Theory]
    [InlineData(0x01u, true)] // LLMHF_INJECTED
    [InlineData(0x02u, true)] // LLMHF_LOWER_IL_INJECTED
    [InlineData(0x03u, true)]
    [InlineData(0x00u, false)]
    public void MouseInjectedFlagsAreRejected(uint flags, bool expected)
    {
        Assert.Equal(expected, InputFilter.IsInjectedMouse(flags));
    }
}
