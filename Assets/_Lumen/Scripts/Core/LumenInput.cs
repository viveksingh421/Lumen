using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>Thin wrapper over the new Input System (project is set to "Input System Package" only).</summary>
    public static class LumenInput
    {
        public static bool Pressed(Key k) { var kb = Keyboard.current; return kb != null && kb[k].wasPressedThisFrame; }
        public static bool Held(Key k) { var kb = Keyboard.current; return kb != null && kb[k].isPressed; }

        public static Vector2 Move()
        {
            Vector2 m = Vector2.zero;
            if (Held(Key.W) || Held(Key.UpArrow)) m.y += 1;
            if (Held(Key.S) || Held(Key.DownArrow)) m.y -= 1;
            if (Held(Key.D) || Held(Key.RightArrow)) m.x += 1;
            if (Held(Key.A) || Held(Key.LeftArrow)) m.x -= 1;
            return m;
        }

        public static Vector2 LookDelta() => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
        public static bool Fire() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool AnyConfirm() => Pressed(Key.Space) || Pressed(Key.Enter) || Pressed(Key.Escape) || Fire();
    }
}
