using System.Collections.Generic;
using Muhanok.Application;
using Muhanok.Domain;
using UnityEngine.InputSystem;

namespace Muhanok.Infrastructure
{
    public sealed class KeyboardGameInput : IGameInput
    {
        private readonly Queue<MotionCommand> buffer = new Queue<MotionCommand>(6);
        public static bool StartPressed => (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame
            || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame))
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
        // Operator keys for the exhibition: R recalibrates the camera pose, Ctrl+F10 clears today's ranking.
        public static bool RecalibratePressed => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool ClearRankingPressed => Keyboard.current != null && Keyboard.current.ctrlKey.isPressed && Keyboard.current.f10Key.wasPressedThisFrame;
        // Korean names need the IME while the setup text fields are open.
        public static void SetTextInput(bool on) { if (Keyboard.current != null) Keyboard.current.SetIMEEnabled(on); }
        public void Poll()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame) buffer.Enqueue(MotionCommand.Left);
            if (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame) buffer.Enqueue(MotionCommand.Right);
            if (k.spaceKey.wasPressedThisFrame) buffer.Enqueue(MotionCommand.Jump);
            if (k.sKey.isPressed || k.downArrowKey.isPressed) buffer.Enqueue(MotionCommand.Crouch);
            if (k.wKey.isPressed || k.upArrowKey.isPressed) buffer.Enqueue(MotionCommand.HighKnee);
        }
        public bool TryRead(out MotionCommand command) { command = MotionCommand.Center; if (buffer.Count == 0) return false; command = buffer.Dequeue(); return true; }
        public void Clear() { buffer.Clear(); }
    }
}
