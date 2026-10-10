using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Light Shard (CONCEPT 1). It has NO transform-driven renderer: every frame the model matrix is composed ONLY from
    /// the hand-written MatrixMath builders and the crystal is submitted with Graphics.DrawMesh.
    ///   M = T(pos + bob) * Ry(spin) * Rx(tilt) * S(pulse)
    /// Pickup uses the sphere-sphere distance test from RayIntersect.
    /// </summary>
    public class Shard : MonoBehaviour
    {
        public const float PickupRadius = 0.9f;
        public Mesh mesh;
        public Material material;
        public Vector3 basePos;
        public Matrix4x4 CurrentMatrix { get; private set; }
        public bool Collected { get; private set; }
        float phase;

        void Start() { phase = Random.value * 6.28f; }

        void Update()
        {
            if (Collected) return;
            float t = Time.time + phase;
            float bob = Mathf.Sin(t * 2f) * 0.35f;
            float pulse = 1f + 0.18f * Mathf.Sin(t * 4f);
            Matrix4x4 m = MatrixMath.Translate(basePos + new Vector3(0, bob, 0))
                        * MatrixMath.RotateY(t * 90f)
                        * MatrixMath.RotateX(18f * Mathf.Sin(t))
                        * MatrixMath.Scale(new Vector3(pulse, pulse, pulse));
            CurrentMatrix = m;
            transform.position = MatrixMath.MultiplyPoint(m, Vector3.zero);   // keeps the object findable for radar / pickup
            Graphics.DrawMesh(mesh, m, material, 0);

            var orb = WorldBuilder.I != null ? WorldBuilder.I.Orb : null;
            if (orb != null && GameManager.I != null && GameManager.I.State == GameManager.GameState.Playing
                && RayIntersect.SphereSphere(orb.position, OrbController.Radius, transform.position, PickupRadius))
                Collect();
        }

        void Collect()
        {
            Collected = true;
            ParticleFactory.Burst(transform.position, new Color(1f, 0.95f, 0.4f), new Color(1f, 0.3f, 0.8f), 60, 8f, 0.45f, 1.0f);
            GameManager.I.CollectShard();
            Destroy(gameObject);
        }
    }
}
