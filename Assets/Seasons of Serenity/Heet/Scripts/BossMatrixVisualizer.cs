using UnityEngine;

public class BossMatrixVisualizer : MonoBehaviour
{
    public FinalBossController boss;

    [Header("Matrix")]
    public int gridSize = 4;
    public Vector2 topLeft = new Vector2(10, 10);
    public float cellSize = 50f;
    public float padding = 8f;

    [Header("Display")]
    public bool show = true;

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

    private void OnGUI()
    {
        if (!show) return;

        Rect panel = new Rect(topLeft.x, topLeft.y,
            gridSize * cellSize + padding * 2,
            gridSize * cellSize + 70);

        GUI.Box(panel, "Boss Decision Matrix");

        if (boss == null)
        {
            GUI.Label(new Rect(panel.x + 10, panel.y + 25, panel.width - 20, 20),
                "Assign FinalBossController to 'boss'.");
            return;
        }

        Vector2Int cur = boss.CurrentVertexForDebug;
        Vector2Int prev = boss.PreviousVertexForDebug;
        string atk = boss.LastChosenAttackNameForDebug;

        // Info line
        GUI.Label(new Rect(panel.x + 10, panel.y + panel.height - 40, panel.width - 20, 18),
            $"Prev: {prev}  ->  Curr: {cur}   Attack: {atk}");

        // Draw grid (y=0 at top like your matrix[0,0] start)
        float startX = panel.x + padding;
        float startY = panel.y + 25;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                Rect cell = new Rect(startX + x * cellSize, startY + y * cellSize, cellSize, cellSize);

                // base
                DrawRect(cell, new Color(0.15f, 0.15f, 0.15f, 0.9f));
                DrawBorder(cell, 2f, new Color(1f, 1f, 1f, 0.25f));

                // label
                GUI.Label(new Rect(cell.x + 6, cell.y + 6, cell.width - 12, 18), $"({x},{y})");

                // highlight prev/curr
                if (prev.x == x && prev.y == y)
                    DrawRect(new Rect(cell.x + 2, cell.y + 2, cell.width - 4, cell.height - 4),
                        new Color(1f, 0.9f, 0.2f, 0.35f)); // yellow

                if (cur.x == x && cur.y == y)
                    DrawRect(new Rect(cell.x + 2, cell.y + 2, cell.width - 4, cell.height - 4),
                        new Color(0.2f, 1f, 0.3f, 0.35f)); // green
            }
        }

        // Draw arrow from prev -> curr
        if (IsValid(prev) && IsValid(cur) && prev != cur)
        {
            Vector2 p1 = CellCenter(startX, startY, prev);
            Vector2 p2 = CellCenter(startX, startY, cur);

            DrawLine(p1, p2, 3f, Color.white);

            // Arrow head
            Vector2 dir = (p2 - p1).normalized;
            Vector2 left = new Vector2(-dir.y, dir.x);
            Vector2 headBase = p2 - dir * 10f;
            DrawLine(p2, headBase + left * 6f, 3f, Color.white);
            DrawLine(p2, headBase - left * 6f, 3f, Color.white);

            // Attack label near arrow
            Vector2 mid = (p1 + p2) * 0.5f;
            GUI.Label(new Rect(mid.x + 6, mid.y - 10, 200, 20), atk);
        }
    }

    private bool IsValid(Vector2Int v) =>
        v.x >= 0 && v.y >= 0 && v.x < gridSize && v.y < gridSize;

    private Vector2 CellCenter(float startX, float startY, Vector2Int v)
    {
        return new Vector2(
            startX + v.x * cellSize + cellSize * 0.5f,
            startY + v.y * cellSize + cellSize * 0.5f
        );
    }

    private void DrawRect(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, tex);
        GUI.color = old;
    }

    private void DrawBorder(Rect r, float w, Color c)
    {
        DrawRect(new Rect(r.x, r.y, r.width, w), c);
        DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
        DrawRect(new Rect(r.x, r.y, w, r.height), c);
        DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
    }

    private void DrawLine(Vector2 a, Vector2 b, float width, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;

        float angle = Vector2.SignedAngle(Vector2.right, b - a);
        float length = (b - a).magnitude;

        Matrix4x4 m = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, a);
        GUI.DrawTexture(new Rect(a.x, a.y, length, width), tex);
        GUI.matrix = m;

        GUI.color = old;
    }
}
