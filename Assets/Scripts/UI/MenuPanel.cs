using UnityEngine;
using UnityEngine.UI;

namespace Untitled
{
    public class MenuPanel : PanelBase
    {
        [SerializeField] private Button BtnResume;

        protected override void OnInit()
        {
            if (BtnResume != null) BtnResume.onClick.AddListener(OnResumeClicked);
        }

        private void OnResumeClicked()
        {
            Resume();
        }

        private void Resume()
        {
            UIManager.Instance.Close(UIPanel.Menu);
        }
    }
}
