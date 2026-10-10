using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 12 - Light pulse = a ray cast from the orb along the camera's forward direction and tested
    /// against every Sentinel with the hand-written ray-AABB test (broad phase) followed by ray-sphere (narrow phase).
    /// Terrain occlusion is checked with Physics.Raycast (allowed: Physics only for the ground).
    /// </summary>
    public class PulseWeapon : MonoBehaviour
    {
        public Camera cam;
        public float range = 45f, cooldown = 0.45f, stunSeconds = 3f;
        public Vector3 LastRayOrigin, LastRayDir;
        public string LastResult = "-";

        LineRenderer beam;
        float cd, beamTimer;

        void Start()
        {
            var go = new GameObject("PulseBeam");
            go.transform.SetParent(transform, false);
            beam = go.AddComponent<LineRenderer>();
            beam.useWorldSpace = true;
            beam.positionCount = 2;
            beam.widthMultiplier = 0.18f;
            beam.sharedMaterial = LumenResources.I.line;
            beam.startColor = new Color(0.5f, 1f, 1f, 1f);
            beam.endColor = new Color(1f, 0.4f, 1f, 0.2f);
            beam.enabled = false;
        }

        void Update()
        {
            cd -= Time.deltaTime;
            if (beamTimer > 0f) { beamTimer -= Time.unscaledDeltaTime; if (beamTimer <= 0f) beam.enabled = false; }

            var gm = GameManager.I;
            if (gm == null || gm.State != GameManager.GameState.Playing || Cursor.lockState != CursorLockMode.Locked) return;
            if (cd <= 0f && LumenInput.Fire()) Fire();
        }

        void Fire()
        {
            cd = cooldown;
            Vector3 o = transform.position;
            Vector3 d = cam.transform.forward;
            LastRayOrigin = o; LastRayDir = d;

            // nearest Sentinel hit using our own intersection routines
            Sentinel best = null; float bestT = range;
            foreach (var s in WorldBuilder.I.Sentinels)
            {
                if (s == null) continue;
                Bounds b = s.WorldBounds;
                if (!RayIntersect.RayAABB(o, d, b.min, b.max, out float _)) continue;                  // broad phase
                if (!RayIntersect.RaySphere(o, d, s.transform.position, s.HitRadius, out float t)) continue; // narrow phase
                if (t < bestT) { bestT = t; best = s; }
            }

            // terrain occlusion
            if (Physics.Raycast(o, d, out RaycastHit terrainHit, range) && terrainHit.distance < bestT) { best = null; bestT = terrainHit.distance; }

            Vector3 end = o + d * (best != null || bestT < range ? bestT : range);
            beam.SetPosition(0, o + Vector3.down * 0.1f); beam.SetPosition(1, end);
            beam.enabled = true; beamTimer = 0.12f;

            if (best != null)
            {
                best.Stun(stunSeconds);
                ParticleFactory.Burst(end, new Color(0.6f, 1f, 1f), new Color(1f, 0.5f, 1f), 40, 7f, 0.35f, 0.7f);
                LastResult = "HIT Sentinel  t=" + bestT.ToString("F2");
                GameManager.I.Toast("Sentinel stunned!", 1.2f);
            }
            else
            {
                ParticleFactory.Burst(end, new Color(0.5f, 0.6f, 1f), new Color(0.6f, 0.2f, 0.8f), 14, 4f, 0.25f, 0.5f);
                LastResult = "miss";
            }
        }
    }
}
