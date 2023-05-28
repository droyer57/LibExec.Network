using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace LibExec.MonoGame;

public sealed class Input
{
    private static KeyboardState _keyboard;
    private static KeyboardState _oldKeyboard;
    private static MouseState _mouse;
    private static MouseState _oldMouse;

    internal Input()
    {
    }

    public static Vector2 MousePosition
    {
        get => new(_mouse.X, _mouse.Y);
        set => Mouse.SetPosition((int)value.X, (int)value.Y);
    }

    public static Vector2 MouseDirection { get; private set; }
    public static int MouseScrollDelta { get; private set; }

    public static Vector2 Axis
    {
        get
        {
            var axis = Vector2.Zero;
            if (IsKey(Keys.D)) axis.X = 1;
            if (IsKey(Keys.A)) axis.X = -1;
            if (IsKey(Keys.W)) axis.Y = -1;
            if (IsKey(Keys.S)) axis.Y = 1;

            return axis;
        }
    }

    internal void BeginUpdate()
    {
        _keyboard = Keyboard.GetState();
        _mouse = Mouse.GetState();

        MouseDirection = _mouse.Position.ToVector2() - _oldMouse.Position.ToVector2();
        MouseScrollDelta = _mouse.ScrollWheelValue - _oldMouse.ScrollWheelValue;

        if (Screen.IsMouseLock)
        {
            MousePosition = Screen.Center;
            UpdateMouse();
        }
    }

    internal void EndUpdate()
    {
        _oldKeyboard = _keyboard;
        _oldMouse = _mouse;
    }

    public static bool IsKey(Keys key)
    {
        return _keyboard.IsKeyDown(key);
    }

    public static bool IsKeyDown(Keys key)
    {
        return _keyboard.IsKeyDown(key) && _oldKeyboard.IsKeyUp(key);
    }

    public static bool IsKeyUp(Keys key)
    {
        return _keyboard.IsKeyUp(key) && _oldKeyboard.IsKeyDown(key);
    }

    public static bool IsMouseButton(MouseButton mouseButton)
    {
        return mouseButton switch
        {
            MouseButton.Left => _mouse.LeftButton == ButtonState.Pressed,
            MouseButton.Right => _mouse.RightButton == ButtonState.Pressed,
            MouseButton.Middle => _mouse.MiddleButton == ButtonState.Pressed,
            _ => false
        };
    }

    public static bool IsMouseButtonDown(MouseButton mouseButton)
    {
        return mouseButton switch
        {
            MouseButton.Left => _mouse.LeftButton == ButtonState.Pressed &&
                                _oldMouse.LeftButton == ButtonState.Released,
            MouseButton.Right => _mouse.RightButton == ButtonState.Pressed &&
                                 _oldMouse.RightButton == ButtonState.Released,
            MouseButton.Middle => _mouse.MiddleButton == ButtonState.Pressed &&
                                  _oldMouse.MiddleButton == ButtonState.Released,
            _ => false
        };
    }

    public static bool IsMouseButtonUp(MouseButton mouseButton)
    {
        return mouseButton switch
        {
            MouseButton.Left => _mouse.LeftButton == ButtonState.Released &&
                                _oldMouse.LeftButton == ButtonState.Pressed,
            MouseButton.Right => _mouse.RightButton == ButtonState.Released &&
                                 _oldMouse.RightButton == ButtonState.Pressed,
            MouseButton.Middle => _mouse.MiddleButton == ButtonState.Released &&
                                  _oldMouse.MiddleButton == ButtonState.Pressed,
            _ => false
        };
    }

    private static void UpdateMouse()
    {
        _mouse = Mouse.GetState();
        _oldMouse = _mouse;
    }
}