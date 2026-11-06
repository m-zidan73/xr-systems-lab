using UnityEngine;

namespace Lab.Core
{
    /// <summary>A box in the corner of the Game view with the demo's instructions and its latest message. Drawn with IMGUI, so it needs no canvas.</summary>
    public class StatusLine : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float FontSize = 15f;
        const float Width = 470f;
        const float Margin = 12f;

        [SerializeField, TextArea(2, 4)] string hint = "";

        GUIStyle _style;

        public string Hint => hint;
        public string Message { get; private set; }

        public void Show(string message) => Message = message;

        void OnGUI()
        {
            var scale = Screen.height / ReferenceHeight;
            _style ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true, richText = false };
            _style.fontSize = Mathf.RoundToInt(FontSize * scale);
            _style.padding = new RectOffset(10, 10, 8, 8);

            var text = string.IsNullOrEmpty(Message) ? hint : $"{hint}\n{Message}";
            var width = Width * scale;
            var height = _style.CalcHeight(new GUIContent(text), width);
            GUI.Box(new Rect(Margin * scale, Margin * scale, width, height), text, _style);
        }
    }
}
