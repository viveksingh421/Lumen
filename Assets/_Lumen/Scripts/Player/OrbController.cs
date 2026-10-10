using UnityEngine;
using UnityEngine.InputSystem;

namespace Lumen
{
    /// <summary>
    /// The player: a glowing orb driven by forces on a Rigidbody (Physics is used only for ground contact).
    /// Also acts as the moving POINT LIGHT for every Lumen shader and squashes/stretches on landing (CONCEPT 10).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OrbController : MonoBehaviour
    {
        public const float Radius = 0.5f;

        public Transform visual;
        public OrbitCamera cam;
        public float moveAccel = 38f, maxSpeed = 12f, jumpSpeed = 7.5f, boostSpeed = 13f;

        Rigidbody rb;
        bool grounded, wasGrounded, jumpQueued;
        float lastVy, squashT = 99f, squashAmp, boostCooldown, invulnerable, flicker;
        Vector3 startPos;
        Material visualMat;
        Color baseEmission;

        public bool Grounded => grounded;
        public bool Invulnerable => invulnerable > 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearDamping = 0.35f;
            var col = GetComponent<SphereCollider>();
            col.material = new PhysicsMaterial("OrbMat") { dynamicFriction = 0.6f, staticFriction = 0.9f, frictionCombine = PhysicsMaterialCombine.Maximum, bounciness = 0f };
        }

        void Start()
        {
            startPos = transform.position;
            if (visual != null)
            {
                var r = visual.GetComponent<Renderer>();
                if (r != null) { visualMat = r.sharedMaterial; baseEmission = visualMat.GetColor("_EmissionColor"); }
            }
        }

        public void ResetTo(Vector3 pos)
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            startPos = pos;
            rb.position = pos;                       // set the body itself, otherwise interpolation/physics snaps the transform back
            transform.position = pos;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }

        public void Respawn() { ResetTo(startPos); }

        bool CanPlay => GameManager.I != null && GameManager.I.State == GameManager.GameState.Playing;

        void Update()
        {
            ShadingModeController.SetPointLight(transform.position + Vector3.up * 0.2f);

            if (invulnerable > 0f) invulnerable -= Time.deltaTime;
            boostCooldown -= Time.deltaTime;

            if (CanPlay && LumenInput.Pressed(Key.Space)) jumpQueued = true;

            // Landing detection -> squash & stretch
            if (grounded && !wasGrounded && lastVy < -3f) { squashAmp = Mathf.Clamp(-lastVy / 16f, 0.12f, 0.45f); squashT = 0f; }
            wasGrounded = grounded;
            lastVy = rb.linearVelocity.y;

            AnimateScale();

            if (visualMat != null)
            {
                flicker += Time.deltaTime * 30f;
                float k = invulnerable > 0f ? (Mathf.Sin(flicker) > 0 ? 0.2f : 1f) : 1f;
                visualMat.SetColor("_EmissionColor", baseEmission * k);
            }

            if (transform.position.y < -35f || new Vector2(transform.position.x, transform.position.z).magnitude > 90f) Respawn();
        }

        /// <summary>Damped-oscillation squash after landing + velocity stretch in the air (volume-preserving scale).</summary>
        void AnimateScale()
        {
            if (visual == null) return;
            float sy = 1f;
            if (squashT < 1.2f)
            {
                squashT += Time.deltaTime;
                sy -= squashAmp * Mathf.Exp(-7f * squashT) * Mathf.Cos(20f * squashT);
            }
            else if (!grounded) sy += Mathf.Clamp(Mathf.Abs(rb.linearVelocity.y) * 0.025f, 0f, 0.25f);
            float sxz = 1f / Mathf.Sqrt(Mathf.Max(0.3f, sy));
            visual.localScale = new Vector3(sxz, sy, sxz) * (Radius * 2f);
        }

        void FixedUpdate()
        {
            grounded = Physics.Raycast(transform.position, Vector3.down, Radius + 0.2f);
            if (!CanPlay) { jumpQueued = false; return; }

            Vector2 m = LumenInput.Move();
            float yaw = cam != null ? cam.Yaw : 0f;
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward, right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            Vector3 wish = fwd * m.y + right * m.x;
            if (wish.sqrMagnitude > 1f) wish.Normalize();
            rb.AddForce(wish * moveAccel * (grounded ? 1f : 0.55f), ForceMode.Acceleration);

            Vector3 v = rb.linearVelocity;
            Vector3 h = new Vector3(v.x, 0, v.z);
            if (h.magnitude > maxSpeed) { h = h.normalized * maxSpeed; v.x = h.x; v.z = h.z; }
            if (grounded && wish.sqrMagnitude < 0.01f) { v.x *= 0.92f; v.z *= 0.92f; }   // quick stop when no input
            rb.linearVelocity = v;

            if (jumpQueued)
            {
                jumpQueued = false;
                if (grounded)
                {
                    rb.linearVelocity = new Vector3(v.x, jumpSpeed, v.z);
                }
                else if (boostCooldown <= 0f)           // air boost: dash along the camera heading
                {
                    boostCooldown = 1.6f;
                    Vector3 dir = wish.sqrMagnitude > 0.01f ? wish.normalized : fwd;
                    rb.linearVelocity = new Vector3(dir.x * boostSpeed, Mathf.Max(v.y, 2.5f), dir.z * boostSpeed);
                    ParticleFactory.Burst(transform.position, new Color(0.4f, 1f, 1f), new Color(0.8f, 0.3f, 1f), 24, 4f, 0.3f, 0.6f);
                }
            }
        }

        /// <summary>Called by Sentinels on contact. Returns false if currently invulnerable.</summary>
        public bool TakeHit(Vector3 from)
        {
            if (invulnerable > 0f) return false;
            invulnerable = 1.6f;
            Vector3 away = transform.position - from; away.y = 0;
            if (away.sqrMagnitude < 1e-4f) away = Vector3.forward;
            rb.linearVelocity = away.normalized * 10f + Vector3.up * 5f;
            ParticleFactory.Burst(transform.position, new Color(1f, 0.3f, 0.2f), new Color(1f, 0.8f, 0.2f), 40, 7f, 0.4f, 0.8f);
            return true;
        }
    }
}
