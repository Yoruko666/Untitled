using TMPro;
using UnityEngine;

namespace Untitled
{
    public class BubbleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;

        public void Show(string value)
        {
            if (text != null) text.text = value;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
