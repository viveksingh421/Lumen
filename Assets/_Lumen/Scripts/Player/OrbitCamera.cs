using UnityEngine;

namespace Lumen
{
    /// <summary>Third-person orbit camera: mouse yaw/pitch around the orb, kept above the terrain.</summary>
    public class OrbitCamera : MonoBehaviour
    {
        public Transform target;
        public float distance = 9f, height = 1.2f, sensitivity = 0.12f;
        public float Yaw, Pitch = 24f;
        public bool Follow = true;

        void LateUpdate()
        {
            if (!Follow || target == null) return;
            var gm = GameManager.I;
            if (gm != null && gm.State == GameManager.GameState.Playing && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 d = LumenInput.LookDelta();
                Yaw += d.x * sensitivity;
                Pitch = Mathf.Clamp(Pitch - d.y * sensitivity, -4f, 72f);
            }
            Place(false);
        }

        public void Place(bool snap)
        {
            if (target == null) return;
            Quaternion rot = Quaternion.Euler(Pitch, Yaw, 0f);
            Vector3 focus = target.position + Vector3.up * height;
            Vector3 desired = focus - rot * Vector3.forward * distance;
            float minY = TerrainGenerator.HeightAt(desired.x, desired.z) + 1.2f;
            if (desired.y < minY) desired.y = minY;
            float k = snap ? 1f : 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
            transform.rotation = QuaternionMath.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
