using UnityEngine;

namespace KiboElOrigen
{
    // Efectos visuales hechos por código (partículas de teletransporte, destellos)
    public static class KiboEfectos
    {
        static Material particula;

        public static Material MaterialParticula()
        {
            if (particula != null) return particula;
            particula = Resources.Load<Material>("KiboUI/Particula");
            if (particula == null)
            {
                var sh = Shader.Find("Legacy Shaders/Particles/Additive");
                if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                particula = new Material(sh);
                var s = KiboEstilo.Dibujo("brillo");
                if (s != null) particula.mainTexture = s.texture;
            }
            return particula;
        }

        // Remolino de chispas con un destello de luz
        public static void Teletransporte(Vector3 posicion, Color color, float escala = 1f)
        {
            var go = new GameObject("KIBO_Teletransporte");
            go.transform.position = posicion;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * escala, 2.2f * escala);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * escala, 0.2f * escala);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.2f);
            main.maxParticles = 150;
            main.useUnscaledTime = true;

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
            em.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)55),
                new ParticleSystem.Burst(0.15f, (short)35)
            });

            var sh = ps.shape;
            sh.enabled = true;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.35f * escala;

            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(1.6f * escala);
            vel.z = new ParticleSystem.MinMaxCurve(0f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(6f);

            var tam = ps.sizeOverLifetime;
            tam.enabled = true;
            tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = MaterialParticula();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            ps.Play();

            var luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.range = 4f * escala;
            luz.intensity = 3f;
            luz.shadows = LightShadows.None;
            go.AddComponent<KiboDestello>();

            Object.Destroy(go, 2.5f);
        }

        public static Color ColorDe(TipoCaneca t)
        {
            switch (t)
            {
                case TipoCaneca.Blanca: return new Color(0.55f, 0.85f, 1f);
                case TipoCaneca.Negra: return new Color(0.75f, 0.6f, 1f);
                default: return new Color(0.45f, 1f, 0.5f);
            }
        }
    }
}
