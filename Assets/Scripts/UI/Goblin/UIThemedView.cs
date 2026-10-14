using UnityEngine;

namespace Untitled.UI
{
    public abstract class UIThemedView : MonoBehaviour
    {
        [SerializeField] private UIThemeContext context;
        private UIThemeContext subscribedContext;
        protected virtual void OnEnable()
        {
            subscribedContext = context != null ? context : GetComponentInParent<UIThemeContext>();
            if (subscribedContext == null) return;
            subscribedContext.Changed += ApplyTheme;
            if (subscribedContext.Current != null) ApplyTheme(subscribedContext.Current);
        }
        protected virtual void OnDisable()
        {
            if (subscribedContext != null) subscribedContext.Changed -= ApplyTheme;
            subscribedContext = null;
        }
        public abstract void ApplyTheme(UIThemeDefinition theme);
        public void SetVisible(bool visible) => gameObject.SetActive(visible);
    }
}
