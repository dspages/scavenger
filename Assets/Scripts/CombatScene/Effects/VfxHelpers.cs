using System.Collections;
using UnityEngine;

public static class VfxHelpers
{
    // Easing helpers
    public static float EaseInQuad(float x) { return x * x; }
    public static float EaseOutQuad(float x) { return 1f - (1f - x) * (1f - x); }

    // Snap to nearest cardinal direction
    public static void SnapToCardinal(Transform t)
    {
        if (t == null) return;
        Vector3 up = t.up;
        t.up = GetNearestCardinal(up);
    }

    public static Vector3 GetNearestCardinal(Vector3 dir)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        if (absX > absY)
        {
            return dir.x >= 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            return dir.y >= 0 ? Vector3.up : Vector3.down;
        }
    }

    // Move a transform from->to with easing; optionally keep facing a fixed direction
    public static IEnumerator MoveWithEase(Transform t, Vector3 from, Vector3 to, float duration, System.Func<float, float> ease, bool keepFacing, Vector3 facingDir)
    {
        if (t == null) yield break;
        float elapsed = 0f;
        duration = Mathf.Max(0.0001f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            float e = ease != null ? ease(u) : u;
            t.position = Vector3.LerpUnclamped(from, to, e);
            if (keepFacing && facingDir != Vector3.zero)
            {
                t.up = new Vector3(facingDir.x, facingDir.y, 0f);
            }
            yield return null;
        }
        t.position = to;
    }

    // Simple streak projectile
    public static IEnumerator ProjectileStreak(Vector3 from, Vector3 to, float speed = 12f)
    {
        GameObject go = new GameObject("Projectile_Streak");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.06f;
        lr.endWidth = 0.00f;
        lr.sortingOrder = 20;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.2f, 0.6f, 1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        lr.colorGradient = grad;

        Vector3 dir = (to - from).normalized;
        float totalDist = Vector3.Distance(from, to);
        float traveled = 0f;
        Vector3 pos = from;
        while (traveled < totalDist)
        {
            float step = speed * Time.deltaTime;
            traveled += step;
            pos += dir * step;
            Vector3 tail = pos - dir * 0.25f;
            lr.SetPosition(0, tail);
            lr.SetPosition(1, pos);
            yield return null;
        }
        Object.Destroy(go);
    }

    // Whooshing ball projectile (bright cross + rotating ring + trail)
    public static IEnumerator ProjectileWhooshingBall(Vector3 from, Vector3 to, float speed = 20f)
    {
        GameObject proj = new GameObject("Projectile_Ball");
        proj.transform.position = from;

        GameObject dotAGo = new GameObject("DotA");
        dotAGo.transform.SetParent(proj.transform, false);
        LineRenderer dotA = dotAGo.AddComponent<LineRenderer>();
        dotA.useWorldSpace = true; dotA.positionCount = 2; dotA.material = new Material(Shader.Find("Sprites/Default"));
        dotA.startWidth = 0.08f; dotA.endWidth = 0.08f; dotA.sortingOrder = 22; dotA.startColor = Color.white; dotA.endColor = Color.white;

        GameObject dotBGo = new GameObject("DotB");
        dotBGo.transform.SetParent(proj.transform, false);
        LineRenderer dotB = dotBGo.AddComponent<LineRenderer>();
        dotB.useWorldSpace = true; dotB.positionCount = 2; dotB.material = new Material(Shader.Find("Sprites/Default"));
        dotB.startWidth = 0.08f; dotB.endWidth = 0.08f; dotB.sortingOrder = 22; dotB.startColor = Color.white; dotB.endColor = Color.white;

        GameObject ringGo = new GameObject("WhooshRing");
        ringGo.transform.SetParent(proj.transform, false);
        LineRenderer ring = ringGo.AddComponent<LineRenderer>();
        ring.useWorldSpace = true; ring.loop = true; ring.material = new Material(Shader.Find("Sprites/Default"));
        ring.startWidth = 0.04f; ring.endWidth = 0.04f; ring.sortingOrder = 21; ring.positionCount = 24;
        Gradient ringGrad = new Gradient();
        ringGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.0f, 1f) }
        );
        ring.colorGradient = ringGrad;

        TrailRenderer trail = proj.AddComponent<TrailRenderer>();
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.time = 0.12f; trail.startWidth = 0.10f; trail.endWidth = 0.00f; trail.sortingOrder = 20;
        Gradient trailGrad = new Gradient();
        trailGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 1f, 0.7f), 0f), new GradientColorKey(new Color(1f, 0.6f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        trail.colorGradient = trailGrad;

        Vector3 dir = (to - from).normalized;
        float dist = Vector3.Distance(from, to);
        float traveled = 0f;
        while (traveled < dist)
        {
            Vector3 pos = from + dir * traveled;
            proj.transform.position = pos;
            Vector3 off = new Vector3(0.05f, 0f, 0f);
            dotA.SetPosition(0, pos - off); dotA.SetPosition(1, pos + off);
            off = new Vector3(0f, 0.05f, 0f);
            dotB.SetPosition(0, pos - off); dotB.SetPosition(1, pos + off);

            UpdateCircleLine(ring, 0.12f, proj.transform.position, Time.time * 40f);

            traveled += speed * Time.deltaTime;
            yield return null;
        }
        Object.Destroy(proj);
    }

    // Expanding AoE ring centered at position
    public static IEnumerator AoEExpandingRing(Vector3 center, float radiusTiles, float duration = 0.35f)
    {
        GameObject go = new GameObject("AOE_Ring");
        go.transform.position = center;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.sortingOrder = 19;
        lr.positionCount = 64;
        Color inner = new Color(1f, 0.7f, 0.2f, 0.9f);
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(inner, 0f),
                new GradientColorKey(inner, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        lr.colorGradient = grad;

        float t = 0f;
        duration = Mathf.Max(0.0001f, duration);
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float r = Mathf.Lerp(0.2f, Mathf.Max(0.2f, radiusTiles), t);
            UpdateCircleLine(lr, r, center);
            yield return null;
        }
        Object.Destroy(go);
    }

    private static void UpdateCircleLine(LineRenderer lr, float radius, Vector3 center, float rotationDegrees = 0f)
    {
        if (lr == null) return;
        int segments = Mathf.Max(8, lr.positionCount);
        float rotRad = rotationDegrees * Mathf.Deg2Rad;
        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f + rotRad;
            Vector3 p = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
            lr.SetPosition(i, center + p);
        }
    }
}


