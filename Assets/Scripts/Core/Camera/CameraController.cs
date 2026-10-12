using UnityEngine;

namespace Untitled
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float viewAngle = 45f;
        [SerializeField] private float yawAngle;
        [SerializeField] private float distance = 7.07f;
        [SerializeField] private float damping = 8f;
        [SerializeField] private Vector3 focusOffset;

        public Transform Target => target;

        public float ViewAngle => viewAngle;

        public float Distance => distance;

        public float YawAngle => yawAngle;

        public Vector3 FocusOffset => focusOffset;

        private Vector3 Offset
        {
            get
            {
                return focusOffset + Quaternion.Euler(viewAngle, yawAngle, 0f) * Vector3.back * distance;
            }
        }

        private void Awake()
        {
            if (target == null)
            {
                target = FindTarget();
            }

            if (target == null)
            {
                Debug.LogWarning("[CameraController] Camera target is null", this);
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + Offset;
            float t = 1f - Mathf.Exp(-damping * Time.deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, desiredPosition, t), Quaternion.Euler(viewAngle, yawAngle, 0f));
        }

        /// <summary>设置跟随目标。</summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>设置俯视角。</summary>
        public void SetViewAngle(float angle)
        {
            viewAngle = Mathf.Clamp(angle, 0f, 89f);
        }

        /// <summary>设置相机到目标的距离。</summary>
        public void SetDistance(float value)
        {
            distance = Mathf.Max(0.1f, value);
        }

        /// <summary>设置绕跟随焦点的水平旋转角。</summary>
        public void SetYawAngle(float angle)
        {
            yawAngle = angle;
        }

        /// <summary>设置焦点相对跟随目标的世界坐标偏移。</summary>
        public void SetFocusOffset(Vector3 offset)
        {
            focusOffset = offset;
        }

        private Transform FindTarget()
        {
            PlayerEntity player = FindObjectOfType<PlayerEntity>();
            return player != null ? player.transform : null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + Offset;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(desiredPosition, 0.3f);
            Gizmos.DrawLine(target.position, desiredPosition);
        }
#endif
    }
}
