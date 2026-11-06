using UnityEngine;

public enum MobileDirection
{
    Up,
    Down,
    Left,
    Right
}

public class MobileInput : MonoBehaviour
{
    private static bool up;
    private static bool down;
    private static bool left;
    private static bool right;
    private static bool jumpPressed;

    public static Vector2 MoveInput
    {
        get
        {
            float x = (right ? 1f : 0f) - (left ? 1f : 0f);
            float y = (up ? 1f : 0f) - (down ? 1f : 0f);

            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }
    }

    public static void SetDirection(
        MobileDirection direction, bool pressed)
    {
        switch (direction)
        {
            case MobileDirection.Up: up = pressed; break;
            case MobileDirection.Down: down = pressed; break;
            case MobileDirection.Left: left = pressed; break;
            case MobileDirection.Right: right = pressed; break;
        }
    }

    public static void PressJump()
    {
        jumpPressed = true;
    }

    public static bool ConsumeJump()
    {
        bool result = jumpPressed;
        jumpPressed = false;
        return result;
    }

    public void ResetInput()
    {
        up = down = left = right = false;
        jumpPressed = false;
    }
}