using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Keyboard polling via the new Input System. The Unity 6 Universal 3D
// template runs with the Input System package active, where the legacy
// UnityEngine.Input API throws InvalidOperationException, so every
// gameplay key read goes through here.
public static class KeyPoll
{
    static KeyControl Control(KeyCode c)
    {
        var kb = Keyboard.current;
        if (kb == null) return null;
        switch (c)
        {
            case KeyCode.W: return kb.wKey;
            case KeyCode.S: return kb.sKey;
            case KeyCode.A: return kb.aKey;
            case KeyCode.D: return kb.dKey;
            case KeyCode.R: return kb.rKey;
            case KeyCode.Space: return kb.spaceKey;
            case KeyCode.UpArrow: return kb.upArrowKey;
            case KeyCode.DownArrow: return kb.downArrowKey;
            case KeyCode.LeftArrow: return kb.leftArrowKey;
            case KeyCode.RightArrow: return kb.rightArrowKey;
            case KeyCode.Return: return kb.enterKey;
            case KeyCode.KeypadEnter: return kb.numpadEnterKey;
            default: return null;
        }
    }

    public static bool Held(KeyCode c)
    {
        var k = Control(c);
        return k != null && k.isPressed;
    }

    public static bool Down(KeyCode c)
    {
        var k = Control(c);
        return k != null && k.wasPressedThisFrame;
    }
}
