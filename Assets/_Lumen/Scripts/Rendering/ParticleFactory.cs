using UnityEngine;

namespace Lumen
{
    /// <summary>
    /// CG CONCEPT 11 - Particle systems created entirely in code: additive blending (see ParticleAdditive.shader),
    /// colour-over-lifetime gradients and size-over-lifetime curves.
    /// </summary>
    public static class ParticleFactory
    {
        static Gradient MakeGradient(Color a, Color b)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
            return g;
        }

        static ParticleSystem Create(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // must be stopped before changing duration
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = LumenResources.I.particle;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        static void Style(ParticleSystem ps, float life, float speed, float size, Color from, Color to, float gravity = 0f, bool world = true)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.gravityModifier = gravity;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 600;

            var col = ps.colorOverLifetime; col.enabled = true; col.color = MakeGradient(from, to);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.6f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)));
        }

        /// <summary>One-shot radial burst (shard collection, pulse impact sparks). Destroys itself.</summary>
        public static void Burst(Vector3 pos, Color from, Color to, int count, float speed = 6f, float size = 0.35f, float life = 0.9f)
        {
            var ps = Create("Burst", null, pos);
            Style(ps, life, speed, size, from, to, 0.3f);
            var main = ps.main; main.loop = false; main.duration = 0.2f;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            ps.Play();
            Object.Destroy(ps.gameObject, life + 0.5f);
        }

        /// <summary>Continuous trail that follows the orb (world-space simulation leaves a streak).</summary>
        public static ParticleSystem Trail(Transform parent)
        {
            var ps = Create("OrbTrail", parent, parent.position);
            Style(ps, 0.9f, 0.3f, 0.5f, new Color(0.4f, 1f, 1f), new Color(0.7f, 0.2f, 1f));
            var em = ps.emission; em.enabled = true; em.rateOverTime = 0; em.rateOverDistance = 9f;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
            ps.Play();
            return ps;
        }

        /// <summary>Swirling ring of particles around the portal.</summary>
        public static ParticleSystem PortalSwirl(Transform parent, Vector3 pos, Vector3 normal)
        {
            var ps = Create("PortalSwirl", parent, pos);
            ps.transform.rotation = Quaternion.LookRotation(normal);
            Style(ps, 1.6f, 0.4f, 0.35f, new Color(0.3f, 1f, 1f), new Color(1f, 0.2f, 0.9f), 0f, true);
            var em = ps.emission; em.enabled = true; em.rateOverTime = 70;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 2.6f; sh.radiusThickness = 0.15f;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalZ = new ParticleSystem.MinMaxCurve(2.2f);
            vel.radial = new ParticleSystem.MinMaxCurve(-1.2f);
            ps.Play();
            return ps;
        }

        /// <summary>Ambient floating dust: a box around 'parent' (the camera), simulated in world space.</summary>
        public static ParticleSystem Dust(Transform parent)
        {
            var ps = Create("Dust", parent, parent.position);
            Style(ps, 8f, 0.4f, 0.12f, new Color(0.7f, 0.5f, 1f), new Color(0.3f, 0.8f, 1f), -0.01f);
            var main = ps.main; main.maxParticles = 400;
            var em = ps.emission; em.enabled = true; em.rateOverTime = 45;
            var sh = ps.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(30, 12, 30);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.3f;
            ps.Play();
            return ps;
        }
    }
}
