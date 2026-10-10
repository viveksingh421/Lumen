using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Patrolling enemy. Patrol path = closed chain of cubic Bezier segments (CONCEPT 5). Vision = view-cone dot-product test
    /// + a hand-marched line-of-sight ray against the terrain height-field (CONCEPT 12). Turning uses the hand-written
    /// quaternion slerp (CONCEPT 10). Speed grows with the number of shards collected.
    /// </summary>
    public class Sentinel : MonoBehaviour
    {
        public enum State { Patrol, Chase, Stunned }

        public Vector3[] Chain;                 // 3n+1 Bezier control points
        public float baseSpeed = 4.5f, chaseBonus = 2.2f, HitRadius = 1.3f;
        public float sightRange = 24f, sightHalfAngle = 55f;

        public State Current { get; private set; } = State.Patrol;
        public Vector3 Forward => transform.forward;
        public Bounds WorldBounds => new Bounds(transform.position, new Vector3(4f, 3f, 5f));
        public float SightRange => sightRange;
        public float SightHalfAngle => sightHalfAngle;

        float u;                                // curve parameter in [0,n)
        float stunTimer, lostSightTimer;
        Material mat;
        Renderer rend;
        Transform eyeL, eyeR;
        Vector3 lastPos;
        static readonly Color patrolCol = new Color(1f, 0.25f, 0.2f), chaseCol = new Color(1f, 0.75f, 0.1f), stunCol = new Color(0.3f, 0.9f, 1f);

        public void Init(Vector3[] chain, float startU, Material material, Mesh mesh)
        {
            Chain = chain; u = startU;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            rend = gameObject.AddComponent<MeshRenderer>();
            rend.sharedMaterial = mat = material;

            // Glowing dragon eyes (left and right) on the snout/head
            eyeL = CreateEye(new Vector3(-0.22f, 0.65f, 1.75f));
            eyeR = CreateEye(new Vector3( 0.22f, 0.65f, 1.75f));

            transform.position = Ground(Bezier.EvaluateChain(chain, u));
            lastPos = transform.position;
            SetColor(patrolCol);
        }

        Transform CreateEye(Vector3 localPos)
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(e.GetComponent<Collider>());
            e.transform.SetParent(transform, false);
            e.transform.localPosition = localPos;
            e.transform.localScale = Vector3.one * 0.24f;
            e.GetComponent<Renderer>().sharedMaterial = LumenResources.I.orb;
            return e.transform;
        }

        Vector3 Ground(Vector3 p)
        {
            p.y = Mathf.Max(p.y, TerrainGenerator.HeightAt(p.x, p.z) + 1.5f);
            return p;
        }

        void SetColor(Color c)
        {
            mat.SetColor("_BaseColor", c * 0.9f);
            mat.SetColor("_EmissionColor", c * 0.9f);
        }

        public void Stun(float seconds)
        {
            Current = State.Stunned; stunTimer = seconds; SetColor(stunCol);
        }

        float Speed()
        {
            int shards = GameManager.I != null ? GameManager.I.ShardsCollected : 0;
            return baseSpeed * (1f + 0.09f * shards);          // difficulty scaling
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            bool playing = gm.State == GameManager.GameState.Playing;
            Vector3 orbPos = WorldBuilder.I.Orb != null ? WorldBuilder.I.Orb.position : Vector3.zero;
            Vector3 desiredDir = transform.forward;
            Vector3 pos = transform.position;

            switch (Current)
            {
                case State.Stunned:
                    stunTimer -= Time.deltaTime;
                    transform.Rotate(0f, 540f * Time.deltaTime, 0f, Space.World);
                    pos.y = Mathf.Lerp(pos.y, TerrainGenerator.HeightAt(pos.x, pos.z) + 1.0f, 3f * Time.deltaTime);
                    if (stunTimer <= 0f) { Current = State.Patrol; SetColor(patrolCol); }
                    break;

                default:
                    if (playing && CanSee(orbPos))
                    {
                        if (Current != State.Chase) { Current = State.Chase; SetColor(chaseCol); }
                        lostSightTimer = 2.5f;
                    }
                    else if (Current == State.Chase)
                    {
                        lostSightTimer -= Time.deltaTime;
                        if (lostSightTimer <= 0f) { Current = State.Patrol; SetColor(patrolCol); u = ClosestParam(pos); }
                    }

                    if (!playing) break;

                    if (Current == State.Chase)
                    {
                        Vector3 to = orbPos - pos; to.y = 0;
                        desiredDir = to.sqrMagnitude > 0.01f ? to.normalized : transform.forward;
                        pos += desiredDir * (Speed() + chaseBonus) * Time.deltaTime;
                        pos.y = Mathf.Lerp(pos.y, TerrainGenerator.HeightAt(pos.x, pos.z) + 1.5f, 4f * Time.deltaTime);
                    }
                    else
                    {
                        // advance along the curve by arc length ~ speed*dt (divide by |B'(t)|)
                        float segLen = Mathf.Max(1f, Bezier.ChainTangent(Chain, u).magnitude);
                        u += Speed() * Time.deltaTime / segLen;
                        Vector3 target = Ground(Bezier.EvaluateChain(Chain, u));
                        desiredDir = Bezier.ChainTangent(Chain, u); desiredDir.y = 0;
                        if (desiredDir.sqrMagnitude < 1e-4f) desiredDir = transform.forward;
                        pos = Vector3.Lerp(pos, target, 1f - Mathf.Exp(-8f * Time.deltaTime));
                    }

                    // smooth turn toward the movement direction with OUR slerp
                    Quaternion want = QuaternionMath.LookRotation(desiredDir.normalized, Vector3.up);
                    transform.rotation = QuaternionMath.Slerp(transform.rotation, want, 1f - Mathf.Exp(-6f * Time.deltaTime));

                    // contact with the player: sphere-sphere test
                    if (RayIntersect.SphereSphere(pos, HitRadius, orbPos, OrbController.Radius))
                        gm.HitPlayer(pos);
                    break;
            }

            transform.position = pos;
            float bob = Mathf.Sin(Time.time * 3f + u) * 0.08f;
            if (eyeL != null) eyeL.localPosition = new Vector3(-0.22f, 0.65f + bob, 1.75f);
            if (eyeR != null) eyeR.localPosition = new Vector3( 0.22f, 0.65f + bob, 1.75f);
        }

        bool CanSee(Vector3 target)
        {
            Vector3 eyePos = transform.position + Vector3.up * 0.3f;
            Vector3 tp = target + Vector3.up * 0.3f;
            if (Current != State.Chase)
            {
                if (!RayIntersect.InViewCone(eyePos, transform.forward, tp, sightHalfAngle, sightRange)) return false;
            }
            else if ((tp - eyePos).magnitude > sightRange * 1.5f) return false;
            return TerrainLineOfSight(eyePos, tp);
        }

        /// <summary>March a ray from a to b; blocked if it dips below the terrain height-field.</summary>
        static bool TerrainLineOfSight(Vector3 a, Vector3 b)
        {
            const int steps = 24;
            for (int i = 1; i < steps; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)steps);
                if (p.y < TerrainGenerator.HeightAt(p.x, p.z) + 0.1f) return false;
            }
            return true;
        }

        float ClosestParam(Vector3 p)
        {
            int n = (Chain.Length - 1) / 3;
            float best = 0, bd = float.MaxValue;
            for (float s = 0; s < n; s += 0.05f)
            {
                float d = (Bezier.EvaluateChain(Chain, s) - p).sqrMagnitude;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }
    }
}
