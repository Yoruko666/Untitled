using UnityEngine;

namespace Untitled
{
    public sealed class ShopRaycastInteractor : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private ShopFirstPersonController player;
        [SerializeField, Min(0.1f)] private float reach = 2.6f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;

        private InputActions inputActions;
        private Font font;
        private GUIStyle labelStyle;
        private float feedbackUntil;

        public RaycastInteractable CurrentTarget { get; private set; }
        public string LastFeedback { get; private set; } = string.Empty;

        private void Awake()
        {
            inputActions = new InputActions();
            font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
        }

        private void OnEnable() => inputActions.Shop.Interact.Enable();

        private void OnDisable()
        {
            inputActions?.Shop.Interact.Disable();
            CurrentTarget = null;
            LastFeedback = string.Empty;
        }

        private void OnDestroy()
        {
            inputActions?.Dispose();
            if (font != null) Destroy(font);
        }

        private void Update()
        {
            CurrentTarget = null;
            if (player == null || viewCamera == null || !player.HasControl) return;
            Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            // Only the first solid hit is eligible: walls and furniture occlude interaction.
            if (Physics.Raycast(ray, out RaycastHit hit, reach, interactionMask, QueryTriggerInteraction.Ignore))
            {
                var candidate = hit.collider.GetComponentInParent<RaycastInteractable>();
                if (candidate != null && candidate.isActiveAndEnabled) CurrentTarget = candidate;
            }
            if (CurrentTarget != null && inputActions.Shop.Interact.WasPressedThisFrame())
            {
                LastFeedback = CurrentTarget.Interact();
                feedbackUntil = Time.unscaledTime + 3f;
            }
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = font, fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true
                };
                labelStyle.normal.textColor = new Color(0.96f, 0.93f, 0.84f);
            }
            DrawLabel(new Rect(16, 16, 490, 34), "WASD 移动  ·  鼠标观察  ·  E 交互  ·  Esc 鼠标锁定");
            if (player == null || !player.HasControl)
            {
                DrawLabel(new Rect(Screen.width / 2f - 190, 66, 380, 42), "鼠标已释放 · 聚焦游戏后按 Esc 继续");
                return;
            }
            Color previous = GUI.color;
            GUI.color = CurrentTarget != null ? new Color(0.72f, 0.88f, 0.54f) : Color.white;
            GUI.DrawTexture(new Rect(Screen.width / 2f - 6, Screen.height / 2f - 1, 12, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width / 2f - 1, Screen.height / 2f - 6, 2, 12), Texture2D.whiteTexture);
            GUI.color = previous;
            if (CurrentTarget != null)
                DrawLabel(new Rect(Screen.width / 2f - 210, Screen.height / 2f + 28, 420, 44), "[E] " + CurrentTarget.Prompt);
            if (Time.unscaledTime < feedbackUntil)
                DrawLabel(new Rect(Screen.width / 2f - 280, Screen.height - 100, 560, 66), LastFeedback);
        }

        private void DrawLabel(Rect rect, string text)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.12f, 0.18f, 0.13f, 0.88f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(rect, text, labelStyle);
        }
    }
}
