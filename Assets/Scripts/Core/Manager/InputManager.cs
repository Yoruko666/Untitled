using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Untitled
{
    public class InputManager : ManagerBase<InputManager>
    {
        private InputActions inputActions;

        public Vector2 MoveInput;

        public override void Initialize()
        {
            base.Initialize();
            inputActions = new InputActions();
            EnableInput();
            inputActions.Player.Move.performed += OnPlayerMove;
            inputActions.Player.Move.canceled += OnPlayerMoveCanceled;
            inputActions.Player.ESC.performed += OnEscPerformed;
            inputActions.Player.Interact.performed += OnInteractPerformed;
            inputActions.Player.Scroll.performed += OnScrollPerformed;
        }

        public void EnableInput()
        {
            inputActions.Player.Enable();
        }

        public void DisableInput()
        {
            inputActions.Player.Disable();
        }

        public void Dispose()
        {
            if (inputActions == null) return;
            inputActions.Disable();
            inputActions.Player.Move.performed -= OnPlayerMove;
            inputActions.Player.Move.canceled -= OnPlayerMoveCanceled;
            inputActions.Player.ESC.performed -= OnEscPerformed;
            inputActions.Player.Interact.performed -= OnInteractPerformed;
            inputActions.Player.Scroll.performed -= OnScrollPerformed;
            inputActions.Dispose(); inputActions = null; MoveInput = Vector2.zero;
        }

        private void OnPlayerMove(InputAction.CallbackContext ctx)
        {
            MoveInput = ctx.ReadValue<Vector2>();
        }

        private void OnPlayerMoveCanceled(InputAction.CallbackContext ctx)
        {
            MoveInput = Vector2.zero;
        }

        private void OnEscPerformed(InputAction.CallbackContext ctx)
        {
            EventCenter.Invoke(GameEventType.ToggleESC);
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            EventCenter.Invoke(GameEventType.Interact);
        }

        private void OnScrollPerformed(InputAction.CallbackContext ctx)
        {
            float value = ctx.ReadValue<Vector2>().y;
            if (Mathf.Approximately(value, 0f)) return;

            EventCenter.Invoke(GameEventType.ScrollSelect, value > 0f ? -1 : 1);
        }
    }
}
