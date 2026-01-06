using UnityEngine;
using System.Collections.Generic;

public class BossRLDetailedGraph : MonoBehaviour
{
    [Header("Reference")]
    public FinalBossController boss;

    [Header("Sampling")]
    public float sampleEverySeconds = 0.2f;
    public int maxSamples = 600; // ~2 minutes @ 0.2s

    [Header("UI")]
    public bool show = true;
    public Vector2 panelPos = new Vector2(10, 10);
    public Vector2 panelSize = new Vector2(720, 420);

    [Header("Chart")]
    public bool clampMinAtZero = true;
    public float yPaddingPercent = 0.10f;
    public int horizontalGridLines = 5;
    public int verticalGridLines = 10;

    // Weight history (lines)
    private readonly List<float> s1 = new List<float>();
    private readonly List<float> s2 = new List<float>();
    private readonly List<float> s3 = new List<float>();
    private readonly List<float> s4 = new List<float>();

    // Choice counts (distribution)
    private int c1, c2, c3, c4;
    private string lastSeenChoice = "none";

    private float timer;

    // tiny texture for drawing lines
    private static Texture2D tex;

    private void Awake()
    {
        if (tex == null)
        {
            tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
        }
    }

    private void Update()
    {
        if (!show || boss == null) return;

        timer += Time.deltaTime;
        if (timer < sampleEverySeconds) return;
        timer = 0f;

        float[] w = boss.GetCurrentNodeWeightsForDebug();
        if (w == null || w.Length < 4) return;

        Push(s1, w[0]);
        Push(s2, w[1]);
        Push(s3, w[2]);
        Push(s4, w[3]);

        // Update choice counts when boss changes choice (so it doesn't count every frame)
        string choice = boss.LastChosenAttackNameForDebug;
        if (!string.IsNullOrEmpty(choice) && choice != "none" && choice != lastSeenChoice)
        {
            switch (choice)
            {
                case "Attack1": c1++; break;
                case "Attack2": c2++; break;
                case "Attack3": c3++; break;
                case "Attack4": c4++; break;
            }
            lastSeenChoice = choice;
        }
    }

    private void Push(List<float> list, float value)
    {
        list.Add(value);
        if (list.Count > maxSamples) list.RemoveAt(0);
    }

    private void OnGUI()
    {
        if (!show) return;

        Rect panel = new Rect(panelPos.x, panelPos.y, panelSize.x, panelSize.y);
        GUI.Box(panel, "Boss RL Detailed Graph (Weights over time + choice %)");

        if (boss == null)
        {
            GUI.Label(new Rect(panel.x + 10, panel.y + 30, panel.width - 20, 20),
                "Assign your FinalBossController to the 'boss' field.");
            return;
        }

        // Top info
        Vector2Int v = boss.CurrentVertexForDebug;
        string last = boss.LastChosenAttackNameForDebug;
        GUI.Label(new Rect(panel.x + 10, panel.y + 25, panel.width - 20, 20),
            $"Vertex: {v}   Last attack: {last}   Samples: {s1.Count}/{maxSamples}");

        // Chart area
        Rect chart = new Rect(panel.x + 55, panel.y + 55, panel.width - 75, panel.height - 140);
        GUI.Box(chart, GUIContent.none);

        // Find min/max
        float ymin, ymax;
        GetMinMax(out ymin, out ymax);

        if (clampMinAtZero) ymin = Mathf.Min(0f, ymin);

        float pad = (ymax - ymin) * yPaddingPercent;
        if (pad < 0.05f) pad = 0.05f;
        ymin -= pad;
        ymax += pad;

        // Grid + labels
        DrawGrid(chart, ymin, ymax);

        // Draw lines
        DrawSeries(chart, s1, ymin, ymax, new Color(0.2f, 0.85f, 1f, 1f)); // Attack1
        DrawSeries(chart, s2, ymin, ymax, new Color(0.3f, 1f, 0.3f, 1f));  // Attack2
        DrawSeries(chart, s3, ymin, ymax, new Color(1f, 0.85f, 0.2f, 1f)); // Attack3
        DrawSeries(chart, s4, ymin, ymax, new Color(1f, 0.35f, 0.6f, 1f)); // Attack4

        // Legend + last values
        float a1 = LastValue(s1), a2 = LastValue(s2), a3 = LastValue(s3), a4 = LastValue(s4);
        Rect legend = new Rect(panel.x + 10, panel.y + panel.height - 78, panel.width - 20, 70);

        int total = c1 + c2 + c3 + c4;
        float p1 = total > 0 ? (c1 * 100f / total) : 0f;
        float p2 = total > 0 ? (c2 * 100f / total) : 0f;
        float p3 = total > 0 ? (c3 * 100f / total) : 0f;
        float p4 = total > 0 ? (c4 * 100f / total) : 0f;

        GUI.Label(legend,
            $"Weights now:  A1={a1:0.00}  A2={a2:0.00}  A3={a3:0.00}  A4={a4:0.00}\n" +
            $"Choices:      A1={c1} ({p1:0.0}%)  A2={c2} ({p2:0.0}%)  A3={c3} ({p3:0.0}%)  A4={c4} ({p4:0.0}%)\n" +
            $"Proof tip: If learning works, the more-successful attack’s line rises and its choice % increases.");
    }

    private float LastValue(List<float> s)
    {
        if (s == null || s.Count == 0) return 0f;
        return s[s.Count - 1];
    }

    private void GetMinMax(out float ymin, out float ymax)
    {
        ymin = float.PositiveInfinity;
        ymax = float.NegativeInfinity;

        ScanSeries(s1, ref ymin, ref ymax);
        ScanSeries(s2, ref ymin, ref ymax);
        ScanSeries(s3, ref ymin, ref ymax);
        ScanSeries(s4, ref ymin, ref ymax);

        if (float.IsInfinity(ymin) || float.IsInfinity(ymax))
        {
            ymin = 0f;
            ymax = 1f;
        }

        if (Mathf.Abs(ymax - ymin) < 0.0001f)
        {
            ymax = ymin + 1f;
        }
    }

    private void ScanSeries(List<float> s, ref float ymin, ref float ymax)
    {
        if (s == null) return;
        for (int i = 0; i < s.Count; i++)
        {
            float v = s[i];
            if (v < ymin) ymin = v;
            if (v > ymax) ymax = v;
        }
    }

    private void DrawGrid(Rect chart, float ymin, float ymax)
    {
        // Horizontal lines + y labels
        for (int i = 0; i <= horizontalGridLines; i++)
        {
            float t = i / (float)horizontalGridLines;
            float y = Mathf.Lerp(chart.yMax, chart.yMin, t);
            DrawLine(new Vector2(chart.xMin, y), new Vector2(chart.xMax, y), 1f, new Color(1f, 1f, 1f, 0.15f));

            float val = Mathf.Lerp(ymin, ymax, t);
            GUI.Label(new Rect(chart.xMin - 50, y - 8, 48, 16), val.ToString("0.00"));
        }

        // Vertical lines (time grid)
        for (int i = 0; i <= verticalGridLines; i++)
        {
            float t = i / (float)verticalGridLines;
            float x = Mathf.Lerp(chart.xMin, chart.xMax, t);
            DrawLine(new Vector2(x, chart.yMin), new Vector2(x, chart.yMax), 1f, new Color(1f, 1f, 1f, 0.10f));
        }
    }

    private void DrawSeries(Rect chart, List<float> s, float ymin, float ymax, Color color)
    {
        if (s == null || s.Count < 2) return;

        Vector2 prev = Map(chart, 0, s[0], ymin, ymax, s.Count);

        for (int i = 1; i < s.Count; i++)
        {
            Vector2 cur = Map(chart, i, s[i], ymin, ymax, s.Count);
            DrawLine(prev, cur, 2f, color);
            prev = cur;
        }
    }

    private Vector2 Map(Rect chart, int i, float v, float ymin, float ymax, int count)
    {
        float tx = (count <= 1) ? 0f : i / (float)(count - 1);
        float ty = Mathf.InverseLerp(ymin, ymax, v);

        float x = Mathf.Lerp(chart.xMin, chart.xMax, tx);
        float y = Mathf.Lerp(chart.yMax, chart.yMin, ty);
        return new Vector2(x, y);
    }

    private void DrawLine(Vector2 a, Vector2 b, float width, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;

        float angle = Vector2.SignedAngle(Vector2.right, b - a);
        float length = (b - a).magnitude;

        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y, length, width), tex);
        GUI.matrix = m;

        GUI.color = old;
    }
}
