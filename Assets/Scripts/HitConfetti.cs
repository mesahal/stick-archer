using UnityEngine;

/// <summary>
/// Colorful confetti pop spawned when an archer takes a hit — the reference game's
/// signature "puff of confetti" on every connect. Self-destroys after the burst.
/// </summary>
public class HitConfetti : MonoBehaviour
{
    static readonly Color[] Palette =
    {
        new Color(1.00f, 0.85f, 0.20f), // yellow
        new Color(0.95f, 0.30f, 0.35f), // red
        new Color(0.30f, 0.70f, 1.00f), // blue
        new Color(0.40f, 0.85f, 0.45f), // green
        new Color(1.00f, 0.55f, 0.85f), // pink
        new Color(1.00f, 1.00f, 1.00f), // white
    };

    /// <summary>Spawn a confetti burst at a world position. <paramref name="strength"/> scales the count/spread.</summary>
    public static void Burst(Vector3 position, float strength = 1f)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(16 * Mathf.Clamp(strength, 0.6f, 2f)), 10, 40);

        var holder = new GameObject("HitConfetti");
        holder.transform.position = position;

        var ps = holder.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.startLifetime  = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
        m.startSpeed     = new ParticleSystem.MinMaxCurve(3.5f, 7.5f);
        m.startSize      = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        m.startRotation  = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        m.gravityModifier = 1.1f;          // flutter down like real confetti
        m.maxParticles   = count;
        m.playOnAwake    = false;

        // Random multi-color start.
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Palette[0], 0f), new GradientColorKey(Palette[3], 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        m.startColor = new ParticleSystem.MinMaxGradient(Palette[0], Palette[1]);

        var e = ps.emission;
        e.enabled = false;
        e.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var s = ps.shape;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius    = 0.08f;

        // Spin the flakes as they fly.
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

        // Fade out near the end.
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Sprites/Default");
        if (shader != null) r.material = new Material(shader);
        r.renderMode  = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 30;

        ps.Play();
        Destroy(holder, 1.1f);
    }
}
