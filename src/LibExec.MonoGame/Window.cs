using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace LibExec.MonoGame;

public abstract class Window : Game
{
    private const string ContentDirectory = "Content";
    private static Window _instance = null!;
    private readonly Input _input = new();
    private readonly Screen _screen = new();

    private readonly Time _time = new();

    protected Window()
    {
        _instance = this;

        Graphics = new GraphicsDeviceManager(this);
        Graphics.GraphicsProfile = GraphicsProfile.HiDef;
        Graphics.PreparingDeviceSettings += (_, args) =>
        {
            Graphics.PreferMultiSampling = true;
            args.GraphicsDeviceInformation.PresentationParameters.MultiSampleCount = 16;
        };

        Content.RootDirectory = ContentDirectory;
        IsMouseVisible = true;
        Window.AllowUserResizing = true;

        Window.ClientSizeChanged += WindowOnClientSizeChanged;

        Screen.SetSize(800, 600);

        // ReSharper disable ObjectCreationAsStatement
        new Resources();
        // ReSharper restore ObjectCreationAsStatement

        ImGuiRenderer = new ImGuiRenderer(this);
    }

    public ImGuiRenderer ImGuiRenderer { get; }
    internal static GraphicsDeviceManager Graphics { get; private set; } = null!;
    public new static GraphicsDevice GraphicsDevice => Graphics.GraphicsDevice;
    internal static SpriteBatch SpriteBatch { get; private set; } = null!;

    protected sealed override void Initialize()
    {
        ImGuiRenderer.RebuildFontAtlas();

        base.Initialize();
    }

    protected sealed override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);

        Start();
    }

    protected sealed override void Update(GameTime gameTime)
    {
        _input.BeginUpdate();
        _time.Update(gameTime);

        Update();

        _input.EndUpdate();

        base.Update(gameTime);
    }

    protected sealed override void Draw(GameTime gameTime)
    {
        _time.Update(gameTime);

        GraphicsDevice.Clear(Color.Black);

        ImGuiRenderer.BeforeLayout(gameTime);
        Draw();
        ImGuiRenderer.AfterLayout();

        base.Draw(gameTime);
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
    }

    protected virtual void Draw()
    {
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ImGuiRenderer.Dispose();
    }

    private void WindowOnClientSizeChanged(object? sender, EventArgs e)
    {
        var bounds = GraphicsDevice.Viewport.Bounds;
        _screen.UpdateSize(bounds.Width, bounds.Height);
    }

    internal static ContentManager GetContent()
    {
        return _instance.Content;
    }

    internal static bool GetMouseVisible()
    {
        return _instance.IsMouseVisible;
    }

    internal static void SetMouseVisible(bool value)
    {
        _instance.IsMouseVisible = value;
    }
}