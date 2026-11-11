using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Lectura de mouse/touch compatible con el Input System nuevo (el proyecto usa
/// activeInputHandler = 1) y con el Input Manager clasico como respaldo.
/// </summary>
public static class PointerInput
{
    public static bool Down;
    public static bool DownThisFrame;
    public static bool UpThisFrame;
    public static Vector2 Screen;

    static bool wasDown;

    public static void Poll()
    {
        bool downNow = false;
        bool pressedNow = false;
        bool releasedNow = false;
        Vector2 pos = Screen;

#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            pos = mouse.position.ReadValue();
            downNow = mouse.leftButton.isPressed;
            pressedNow = mouse.leftButton.wasPressedThisFrame;
            releasedNow = mouse.leftButton.wasReleasedThisFrame;
        }
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
        {
            pos = touch.primaryTouch.position.ReadValue();
            downNow = true;
            pressedNow = touch.primaryTouch.press.wasPressedThisFrame;
            releasedNow = touch.primaryTouch.press.wasReleasedThisFrame;
        }
#else
        pos = Input.mousePosition;
        downNow = Input.GetMouseButton(0);
        pressedNow = Input.GetMouseButtonDown(0);
        releasedNow = Input.GetMouseButtonUp(0);
#endif

        Screen = pos;
        Down = downNow;
        DownThisFrame = pressedNow || (downNow && !wasDown);
        UpThisFrame = releasedNow || (!downNow && wasDown);
        wasDown = downNow;
    }

    public static bool KeyPressed(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        switch (key)
        {
            case Key.R: return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            case Key.Space: return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            case Key.Escape: return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        return false;
#else
        switch (key)
        {
            case Key.R: return Input.GetKeyDown(KeyCode.R);
            case Key.Space: return Input.GetKeyDown(KeyCode.Space);
            case Key.Escape: return Input.GetKeyDown(KeyCode.Escape);
        }
        return false;
#endif
    }

    public enum Key { R, Space, Escape }
}
