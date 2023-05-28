using Microsoft.Xna.Framework;

namespace LibExec.MonoGame;

public sealed class Time
{
    internal Time()
    {
    }

    public static GameTime GameTime { get; private set; } = null!;
    public static float DeltaTime { get; private set; }
    public static float TotalTime { get; private set; }

    internal void Update(GameTime gameTime)
    {
        GameTime = gameTime;
        DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        TotalTime = (float)gameTime.TotalGameTime.TotalSeconds;
    }
}