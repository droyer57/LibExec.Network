using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LibExec.MonoGame;

public static class Renderer
{
    private static SpriteBatch SpriteBatch => Window.SpriteBatch;

    public static void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred, BlendState? blendState = null,
        SamplerState? samplerState = null, DepthStencilState? depthStencilState = null,
        RasterizerState? rasterizerState = null, Effect? effect = null, Matrix? transformMatrix = null)
    {
        SpriteBatch.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect,
            transformMatrix);
    }

    public static void End()
    {
        SpriteBatch.End();
    }

    public static void DrawRectangle(Rectangle rectangle, Color? color = null)
    {
        SpriteBatch.Draw(Resources.WhiteTexture, rectangle, color ?? Color.White);
    }
}