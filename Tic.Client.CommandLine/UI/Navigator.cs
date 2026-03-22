namespace Tic.Client.CommandLine.UI;

internal static class Navigator
{
    internal static int Up(int selectedIndex) =>
        Math.Max(0, selectedIndex - 1);

    internal static int Down(int selectedIndex, int count) =>
        Math.Min(count - 1, selectedIndex + 1);
}

