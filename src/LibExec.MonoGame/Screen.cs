// ReSharper disable PossibleLossOfFraction

using Microsoft.Xna.Framework;

namespace LibExec.MonoGame;

public sealed class Screen
{
    internal Screen()
    {
    }

    public static int Width { get; private set; }
    public static int Height { get; private set; }

    public static Vector2 Center => new(Width / 2, Height / 2);
    public static Vector2 Size => new(Width, Height);

    public static bool IsMouseLock { get; set; }

    public static bool IsMouseVisible
    {
        get => Window.GetMouseVisible();
        set => Window.SetMouseVisible(value);
    }

    public static void SetSize(int width, int height)
    {
        Width = width;
        Height = height;

        Window.Graphics.PreferredBackBufferWidth = width;
        Window.Graphics.PreferredBackBufferHeight = height;
        Window.Graphics.ApplyChanges();
    }

    internal void UpdateSize(int width, int height)
    {
        Width = width;
        Height = height;
    }
}