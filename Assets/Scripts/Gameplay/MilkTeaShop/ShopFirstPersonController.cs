using UnityEngine;

namespace Untitled
{
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class ShopFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField, Min(0f)] private float moveSpeed = 2.8f;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float pitchLimit = 80f;

        private CharacterController controller;
        private InputActions inputActions;
        private float verticalSpeed;
        private float pitch;
        private bool cursorLockedIntent;
        private bool hasFocus = true;
        private bool uiBlocked, restoreCursor;
        private int ignoreCursorToggleFrame = -1;
        public bool UIBlocked => uiBlocked;

        public bool HasControl => !uiBlocked && hasFocus && cursorLockedIntent && Cursor.lockState == CursorLockMode.Locked;
        public Transform Eye => eye;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            inputActions = new InputActions();
            if (eye == null)
            {
                Debug.LogError("Shop player needs an eye Transform.", this);
                enabled = false;
                return;
            }
            pitch = Mathf.DeltaAngle(0f, eye.localEulerAngles.x);
        }

        private void OnEnable()
        {
            inputActions.Shop.Move.Enable();
            inputActions.Shop.Look.Enable();
            inputActions.Shop.ToggleCursor.Enable();
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            inputActions?.Shop.Move.Disable();
            inputActions?.Shop.Look.Disable();
            inputActions?.Shop.ToggleCursor.Disable();
            verticalSpeed = 0f;
            SetCursorLocked(false);
        }

        private void OnDestroy() => inputActions?.Dispose();

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused) SetCursorLocked(false);
        }

        public void SetUIBlocked(bool blocked)
        {
            if (uiBlocked == blocked) return;
            if (blocked) { restoreCursor = cursorLockedIntent; uiBlocked = true; SetCursorLocked(false); }
            else { uiBlocked = false; SetCursorLocked(restoreCursor && hasFocus); }
            ignoreCursorToggleFrame = Time.frameCount;
        }

        public void SetCursorLocked(bool locked)
        {
            cursorLockedIntent = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void Update()
        {
            if (!uiBlocked && Time.frameCount != ignoreCursorToggleFrame && inputActions.Shop.ToggleCursor.WasPressedThisFrame()) SetCursorLocked(!cursorLockedIntent);
            if (!controller.enabled || eye == null) return;

            Vector2 move = Vector2.zero;
            if (HasControl)
            {
                Vector2 look = inputActions.Shop.Look.ReadValue<Vector2>() * mouseSensitivity;
                transform.Rotate(0f, look.x, 0f);
                pitch = Mathf.Clamp(pitch - look.y, -pitchLimit, pitchLimit);
                eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                move = Vector2.ClampMagnitude(inputActions.Shop.Move.ReadValue<Vector2>(), 1f);
            }
            verticalSpeed = controller.isGrounded && verticalSpeed < 0f
                ? -2f : Mathf.Max(verticalSpeed + Physics.gravity.y * Time.deltaTime, -30f);
            Vector3 velocity = (transform.right * move.x + transform.forward * move.y) * moveSpeed;
            velocity.y = verticalSpeed;
            CollisionFlags flags = controller.Move(velocity * Time.deltaTime);
            if ((flags & CollisionFlags.Below) != 0) verticalSpeed = -2f;
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
        }
    }
}
