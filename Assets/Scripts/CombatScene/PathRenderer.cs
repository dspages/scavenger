using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathRenderer : MonoBehaviour
{
    public enum LineType
    {
        MovementPath,   // Marches toward the target (normal behavior)
        SpellTarget     // Emanates outward from the caster
    }

    // Draw a dashed line segment between two world positions
    public void DrawSegment(Vector3 start, Vector3 end, LineType lineType = LineType.MovementPath)
    {
        // Draw a dashed line by creating short line segments with gaps between them.
        float dashLength = 0.1f;
        float gapLength = 0.1f;
        float distance = Vector3.Distance(start, end);
        if (distance <= 0.01f) return;

        int segmentCount = Mathf.Clamp(Mathf.FloorToInt(distance / (dashLength + gapLength)), 1, 300);
        Vector3 direction = (end - start).normalized;

        float dashWidth = 0.08f;
        Color baseColor = lineType == LineType.SpellTarget 
            ? new Color(1f, 0.6f, 0.2f, 0.7f)  // Orange-ish for spells
            : new Color(0.9f, 0.95f, 1f, 0.6f); // Bluish white for movement

        for (int i = 0; i < segmentCount; i++)
        {
            float segStartDist = i * (dashLength + gapLength);
            float segEndDist = segStartDist + dashLength;
            if (segStartDist > distance) break;
            if (segEndDist > distance) segEndDist = distance;

            Vector3 segStart = start + direction * segStartDist;
            Vector3 segEnd = start + direction * segEndDist;

            GameObject dashObj = new GameObject("LineDash");
            dashObj.tag = "LineTag";
            LineRenderer dash = dashObj.AddComponent(typeof(LineRenderer)) as LineRenderer;
            dash.material = new Material(Shader.Find("Sprites/Default"));
            dash.positionCount = 2;
            dash.startWidth = dashWidth;
            dash.endWidth = dashWidth;
            dash.startColor = baseColor;
            dash.endColor = baseColor;
            dash.numCapVertices = 8; // rounded ends
            dash.useWorldSpace = true;
            dash.textureMode = LineTextureMode.Stretch;

            Vector3[] pts = new Vector3[2];
            pts[0] = segStart;
            pts[1] = segEnd;
            dash.SetPositions(pts);

            // Add a small animator to march the dash forward for motion
            PathDashAnimator animator = dashObj.AddComponent<PathDashAnimator>();
            animator.segmentIndex = i;
            animator.segmentCount = segmentCount;
            animator.marchSpeed = lineType == LineType.SpellTarget ? 2f : 1f; // Faster for spells
            animator.baseAlpha = baseColor.a;
            animator.marchAmplitude = 2f;
            animator.lineType = lineType;
            // Phase offset so the whole line contains a full cycle (marching effect aligned)
            animator.phase = (i / (float)segmentCount) * Mathf.PI * 2f;
        }
    }

    public void ClearAll()
    {
        foreach (GameObject line in GameObject.FindGameObjectsWithTag("LineTag"))
        {
            Destroy(line);
        }
    }
}

// Helper component to animate dash with a marching wave
public class PathDashAnimator : MonoBehaviour
{
    public int segmentIndex = 0;
    public int segmentCount = 1;
    public float marchSpeed = 1.5f;
    public float baseAlpha = 0.6f;
    public float marchAmplitude = 0.5f; // 0..1 scale of variation
    public float phase;
    public PathRenderer.LineType lineType = PathRenderer.LineType.MovementPath;

    private LineRenderer line;
    private Color baseColor;

    void Start()
    {
        line = GetComponent<LineRenderer>();
        if (line != null)
        {
            baseColor = line.startColor;
        }
    }

    void Update()
    {
        if (line == null) return;
        
        float offset = (float)segmentIndex / Mathf.Max(1, segmentCount);
        float progress;
        
        if (lineType == PathRenderer.LineType.SpellTarget)
        {
            // For spell targeting, reverse the direction so it emanates outward from the caster
            // Also make segments closer to origin activate first
            progress = (Time.time * marchSpeed - offset) % 1f;
        }
        else
        {
            // Normal movement: marches toward the target
            progress = (Time.time * marchSpeed + offset) % 1f;
        }
        
        float wave = 0.5f + 0.5f * Mathf.Sin(progress * Mathf.PI * 2f + phase);
        // Map wave (0..1) to alpha around baseAlpha with amplitude
        float a = baseAlpha * (1f + (wave - 0.5f) * 2f * marchAmplitude);
        a = Mathf.Clamp(a, 0f, 1f);
        Color c = new Color(baseColor.r, baseColor.g, baseColor.b, a);
        line.startColor = c;
        line.endColor = c;
    }
}


