using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LibExec.MonoGame;

public sealed class Resources
{
    internal Resources()
    {
        WhiteTexture = new Texture2D(Window.GraphicsDevice, 1, 1);
        WhiteTexture.SetData(new[] { Color.White });
    }

    public static Texture2D WhiteTexture { get; private set; } = null!;

    public static T Load<T>(string path)
    {
        return Window.GetContent().Load<T>(path);
    }

    public static Texture2D CreateTexture2D(int width, int height)
    {
        var texture = new Texture2D(Window.GraphicsDevice, width, height);
        var total = width * height;
        var data = new Color[total];
        for (var i = 0; i < total; i++)
            data[i] = Color.White;
        texture.SetData(data);
        return texture;
    }
}