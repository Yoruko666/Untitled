using UnityEngine;
using UnityEngine.UI;

namespace Untitled
{
    public class MenuPanel : PanelBase
    {
        [SerializeField] private Button BtnResume;
        [SerializeField] private Untitled.UI.UIPauseMenu pauseView;
        public void ConfigureView(Untitled.UI.UIPauseMenu view) => pauseView = view;

        protected override void OnInit()
        {
            if (pauseView != null) pauseView.ResumeRequested.AddListener(OnResumeClicked);
            else if (BtnResume != null) BtnResume.onClick.AddListener(OnResumeClicked);
        }

        private void OnDestroy()
        {
            if (pauseView != null) pauseView.ResumeRequested.RemoveListener(OnResumeClicked);
            if (BtnResume != null) BtnResume.onClick.RemoveListener(OnResumeClicked);
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
