using System.Collections.Generic;
using UnityEngine;

namespace Lab.W01.HoleTools
{
    /// <summary>
    /// The small labels beside the plate's holes: each hole's diameter, and its depth once measured. A
    /// label never shows a screw length, because choosing that is the trainee's job. They are drawn on the
    /// screen with IMGUI in the lab's panel style, each joined to its hole by a line, and are kept from
    /// overlapping when holes sit close together.
    /// </summary>
    public class HoleLabels : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float FontSize = 15f;
        const float OffsetX = -110f;
        const float BoxHeight = 26f;
        const float Gap = 4f;
        const float PadX = 9f;
        const float LineWidth = 1.5f;

        static readonly Color BackgroundColour = new Color(0.05f, 0.07f, 0.09f, 0.88f);
        static readonly Color PlainColour = new Color(0.92f, 0.95f, 0.97f);
        static readonly Color MeasuredColour = new Color(1f, 0.86f, 0.3f);
        static readonly Color LineColour = new Color(0.92f, 0.95f, 0.97f, 0.55f);

        [SerializeField] HoleSite[] sites = new HoleSite[0];

        readonly List<int> _order = new List<int>();
        Camera _camera;
        GUIStyle _style;

        public IReadOnlyList<HoleSite> Sites => sites;

        public void Load(HoleSite[] labelled) => sites = labelled ?? new HoleSite[0];

        /// <summary>What a hole's label says.</summary>
        public static string Text(HoleSite site) => site != null ? site.Label : "";

        /// <summary>
        /// Moves labels apart so each is at least <paramref name="spacing"/> below the one above it, and
        /// keeps the group centred on where the labels wanted to be. The values must be in ascending order.
        /// </summary>
        public static void Spread(IList<float> ascending, float spacing)
        {
            if (ascending.Count < 2) return;
            float wanted = 0f, got = 0f;
            foreach (var y in ascending) wanted += y;
            for (var i = 1; i < ascending.Count; i++)
                if (ascending[i] < ascending[i - 1] + spacing) ascending[i] = ascending[i - 1] + spacing;
            foreach (var y in ascending) got += y;
            var shift = (wanted - got) / ascending.Count;
            for (var i = 0; i < ascending.Count; i++) ascending[i] += shift;
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || sites.Length == 0) return;

            var scale = Screen.height / ReferenceHeight;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _style.fontSize = Mathf.RoundToInt(FontSize * scale);

            // Where each hole is on screen, in IMGUI's top-down pixels. Holes behind the camera are skipped.
            var points = new Vector2[sites.Length];
            _order.Clear();
            for (var i = 0; i < sites.Length; i++)
            {
                if (sites[i] == null) continue;
                var screen = _camera.WorldToScreenPoint(sites[i].Hole.Seat.position);
                if (screen.z <= 0f) continue;
                points[i] = new Vector2(screen.x, Screen.height - screen.y);
                _order.Add(i);
            }
            _order.Sort((a, b) => points[a].y.CompareTo(points[b].y));
            var rows = new List<float>(_order.Count);
            foreach (var i in _order) rows.Add(points[i].y);
            Spread(rows, (BoxHeight + Gap) * scale);

            for (var n = 0; n < _order.Count; n++)
            {
                var site = sites[_order[n]];
                var hole = points[_order[n]];
                var text = Text(site);
                var width = _style.CalcSize(new GUIContent(text)).x + PadX * 2f * scale;
                var box = new Rect(hole.x + OffsetX * scale - width, rows[n] - BoxHeight * scale * 0.5f, width, BoxHeight * scale);

                Line(new Vector2(box.xMax, box.center.y), hole, LineWidth * scale);
                Fill(box, BackgroundColour);
                _style.normal.textColor = site.IsMeasured ? MeasuredColour : PlainColour;
                GUI.Label(new Rect(box.x + PadX * scale, box.y, box.width, box.height), text, _style);
            }
        }

        static void Fill(Rect rect, Color colour)
        {
            var previous = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        static void Line(Vector2 from, Vector2 to, float width)
        {
            var delta = to - from;
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            Fill(new Rect(from.x, from.y - width * 0.5f, delta.magnitude, width), LineColour);
            GUI.matrix = matrix;
        }
    }
}
