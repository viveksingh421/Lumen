using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Exit portal. Locked (small dim ring) until all shards are collected, then a KeyframeAnimator opens it:
    /// scale/rotation keys interpolated with linear / ease-in-out / Hermite curves and quaternion slerp (CONCEPT 10).
    /// The ripple + swirl is done in PortalWave.shader (vertex displacement + fragment pattern).
    /// </summary>
    public class Portal : MonoBehaviour
    {
        public Mesh disc;
        public Material material;
        public Vector3 center;
        public float facingYaw;
        public bool IsOpen { get; private set; }
        public const float EnterRadius = 2.4f;

        KeyframeAnimator openAnim, idleAnim;
        float openTime = -1f;
        ParticleSystem swirl;

        public Vector3 Normal => Quaternion.Euler(0, facingYaw, 0) * Vector3.back;

        void Start()
        {
            openAnim = new KeyframeAnimator();
            openAnim.Keys.Add(new KeyframeAnimator.Key(0.0f, 0.35f, new Vector3(0, 0, 0), Easing.Mode.Hermite));
            openAnim.Keys.Add(new KeyframeAnimator.Key(0.5f, 0.8f, new Vector3(0, 0, 120), Easing.Mode.EaseInOut));
            openAnim.Keys.Add(new KeyframeAnimator.Key(1.0f, 1.35f, new Vector3(0, 0, 240), Easing.Mode.EaseInOut));
            openAnim.Keys.Add(new KeyframeAnimator.Key(1.6f, 1.0f, new Vector3(0, 0, 340), Easing.Mode.Linear));

            idleAnim = new KeyframeAnimator { Loop = true };
            idleAnim.Keys.Add(new KeyframeAnimator.Key(0f, 1.0f, new Vector3(0, 0, 0), Easing.Mode.Hermite));
            idleAnim.Keys.Add(new KeyframeAnimator.Key(1f, 1.08f, new Vector3(0, 0, 100), Easing.Mode.Hermite));
            idleAnim.Keys.Add(new KeyframeAnimator.Key(2f, 0.96f, new Vector3(0, 0, 200), Easing.Mode.Hermite));
            idleAnim.Keys.Add(new KeyframeAnimator.Key(3f, 1.0f, new Vector3(0, 0, 300), Easing.Mode.Hermite));
            idleAnim.Keys.Add(new KeyframeAnimator.Key(3.6f, 1.0f, new Vector3(0, 0, 359), Easing.Mode.Linear));
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true; openTime = 0f;
            swirl = ParticleFactory.PortalSwirl(transform, center, Normal);
            GameManager.I.Toast("PORTAL OPEN - reach the portal!", 3f);
        }

        void Update()
        {
            Vector3 scale; Quaternion rot;
            float open;
            if (!IsOpen)
            {
                scale = Vector3.one * 0.35f; rot = Quaternion.Euler(0, 0, Time.time * 20f); open = 0.25f;
            }
            else
            {
                openTime += Time.deltaTime;
                if (openTime < openAnim.Duration) { openAnim.Evaluate(openTime, out scale, out rot); open = Mathf.Clamp01(0.25f + openTime / openAnim.Duration); }
                else { idleAnim.Evaluate(openTime - openAnim.Duration, out scale, out rot); open = 1f; }
            }
            material.SetFloat("_Open", open);

            // M = T * Ry(facing) * R_anim * S_anim * S(radius)
            Matrix4x4 m = MatrixMath.Translate(center) * MatrixMath.RotateY(facingYaw)
                        * Matrix4x4.Rotate(rot) * MatrixMath.Scale(scale * 2.6f);
            Graphics.DrawMesh(disc, m, material, 0);

            var orb = WorldBuilder.I.Orb;
            if (IsOpen && orb != null && GameManager.I.State == GameManager.GameState.Playing
                && RayIntersect.SphereSphere(orb.position, OrbController.Radius, center, EnterRadius))
                GameManager.I.WinGame();
        }
    }
}
