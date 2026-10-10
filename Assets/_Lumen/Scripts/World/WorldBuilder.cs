using System.Collections.Generic;
using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// Builds (and rebuilds) the whole level from a seed: terrain, scrolling grid floor, orb, shards, sentinels and portal.
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public static WorldBuilder I { get; private set; }

        public int Seed { get; private set; }
        public const int ShardCount = 12;
        public const int SentinelCount = 4;

        public Transform Orb { get; private set; }
        public OrbController OrbCtrl { get; private set; }
        public Portal Portal { get; private set; }
        public Mesh TerrainMesh { get; private set; }
        public Transform TerrainTf { get; private set; }
        public readonly List<Shard> Shards = new List<Shard>();
        public readonly List<Sentinel> Sentinels = new List<Sentinel>();
        public Vector3 StartPos { get; private set; }

        public OrbitCamera orbitCam;
        public PulseWeapon pulse;

        Transform worldRoot;
        Mesh crystalMesh, sentinelMesh, discMesh;

        void Awake() { I = this; }

        public void Build(int seed)
        {
            Seed = seed;
            if (worldRoot != null) Destroy(worldRoot.gameObject);
            worldRoot = new GameObject("World").transform;
            Shards.Clear(); Sentinels.Clear();
            var res = LumenResources.I;

            if (crystalMesh == null) crystalMesh = MeshFactory.Crystal(0.45f, 0.85f);
            if (sentinelMesh == null) sentinelMesh = MeshFactory.Dragon();
            if (discMesh == null) discMesh = MeshFactory.Disc(1f);

            // ---- terrain ----
            TerrainMesh = TerrainGenerator.Generate(TerrainGenerator.Settings.Default(seed));
            var tgo = new GameObject("Terrain");
            tgo.transform.SetParent(worldRoot, false);
            tgo.AddComponent<MeshFilter>().sharedMesh = TerrainMesh;
            var tr = tgo.AddComponent<MeshRenderer>(); tr.sharedMaterial = res.terrain;
            tgo.AddComponent<MeshCollider>().sharedMesh = TerrainMesh;
            TerrainTf = tgo.transform;

            // ---- scrolling synthwave floor far below / beyond the arena ----
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Destroy(floor.GetComponent<Collider>());
            floor.name = "GridFloor";
            floor.transform.SetParent(worldRoot, false);
            floor.transform.position = new Vector3(0, -12f, 0);
            floor.transform.localScale = new Vector3(80f, 1f, 80f);     // plane is 10x10 -> 800x800
            floor.GetComponent<Renderer>().sharedMaterial = res.grid;

            // ---- orb ----
            var rng = new System.Random(seed * 7919 + 13);
            StartPos = new Vector3(0, TerrainGenerator.HeightAt(0, 0) + 2f, 0);
            if (Orb == null) CreateOrb();
            OrbCtrl.ResetTo(StartPos);
            orbitCam.Yaw = 0; orbitCam.Pitch = 24;

            // ---- shards: spread on a ring with jitter, kept apart from each other ----
            var used = new List<Vector2>();
            for (int i = 0; i < ShardCount; i++)
            {
                Vector2 p = Vector2.zero; bool ok = false;
                for (int tries = 0; tries < 60 && !ok; tries++)
                {
                    float ang = (i / (float)ShardCount) * Mathf.PI * 2f + (float)(rng.NextDouble() - 0.5) * 0.4f;
                    float rad = 18f + (float)rng.NextDouble() * 48f;
                    p = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                    ok = true;
                    foreach (var q in used) if ((q - p).magnitude < 12f) { ok = false; break; }
                }
                used.Add(p);
                var go = new GameObject("Shard" + i);
                go.transform.SetParent(worldRoot, false);
                var sh = go.AddComponent<Shard>();
                sh.mesh = crystalMesh; sh.material = res.shard;
                sh.basePos = new Vector3(p.x, TerrainGenerator.HeightAt(p.x, p.y) + 1.6f, p.y);
                Shards.Add(sh);
            }

            // ---- sentinels: each patrols a closed Bezier loop around a pseudo-random centre ----
            for (int i = 0; i < SentinelCount; i++)
            {
                float ang = (i / (float)SentinelCount) * Mathf.PI * 2f + 0.7f;
                float cr = 25f + (float)rng.NextDouble() * 25f;
                Vector2 c = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * cr;
                float lr = 10f + (float)rng.NextDouble() * 6f;
                int n = 4;
                var anchors = new Vector3[n];
                for (int k = 0; k < n; k++)
                {
                    float a = k / (float)n * Mathf.PI * 2f + (float)rng.NextDouble() * 0.5f;
                    float rr = lr * (0.7f + 0.6f * (float)rng.NextDouble());
                    float x = c.x + Mathf.Cos(a) * rr, z = c.y + Mathf.Sin(a) * rr;
                    anchors[k] = new Vector3(x, TerrainGenerator.HeightAt(x, z) + 1.6f, z);
                }
                var chain = Bezier.BuildClosedChain(anchors);
                var go = new GameObject("Sentinel" + i);
                go.transform.SetParent(worldRoot, false);
                var s = go.AddComponent<Sentinel>();
                s.Init(chain, (float)rng.NextDouble() * n, res.NewLit(res.sentinel), sentinelMesh);
                Sentinels.Add(s);
            }

            // ---- portal: far side of the arena, facing the centre ----
            float pa = (float)rng.NextDouble() * Mathf.PI * 2f;
            Vector2 pp = new Vector2(Mathf.Cos(pa), Mathf.Sin(pa)) * 56f;
            var pgo = new GameObject("Portal");
            pgo.transform.SetParent(worldRoot, false);
            Portal = pgo.AddComponent<Portal>();
            Portal.disc = discMesh; Portal.material = res.portal;
            Portal.center = new Vector3(pp.x, TerrainGenerator.HeightAt(pp.x, pp.y) + 3.2f, pp.y);
            Vector3 toCentre = new Vector3(-pp.x, 0, -pp.y);
            Portal.facingYaw = Mathf.Atan2(-toCentre.x, -toCentre.z) * Mathf.Rad2Deg;   // disc normal (-Z) points at the centre

            var viz = CurveVisualizer.I;
            if (viz != null) viz.Rebuild();
        }

        void CreateOrb()
        {
            var go = new GameObject("Orb");
            go.transform.position = StartPos;
            var rb = go.AddComponent<Rigidbody>(); rb.mass = 1f;
            var col = go.AddComponent<SphereCollider>(); col.radius = OrbController.Radius;
            var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(vis.GetComponent<Collider>());
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale = Vector3.one * (OrbController.Radius * 2f);
            vis.GetComponent<Renderer>().sharedMaterial = LumenResources.I.orb;
            OrbCtrl = go.AddComponent<OrbController>();
            OrbCtrl.visual = vis.transform; OrbCtrl.cam = orbitCam;
            Orb = go.transform;
            ParticleFactory.Trail(go.transform);
            pulse = go.AddComponent<PulseWeapon>();
            pulse.cam = orbitCam.GetComponent<Camera>();
            orbitCam.target = go.transform;
            var proj = orbitCam.GetComponent<ProjectionController>();
            if (proj != null) proj.focus = go.transform;
        }

        public Shard NearestShard(Vector3 p)
        {
            Shard best = null; float bd = float.MaxValue;
            foreach (var s in Shards)
            {
                if (s == null || s.Collected) continue;
                float d = (s.transform.position - p).sqrMagnitude;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }
    }
}
