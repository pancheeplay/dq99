using Dq99.Prototype.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Dq99.Prototype.Unity
{
    public sealed class PrototypeHud
    {
        private readonly GameObject _root;
        private readonly Text _promptText;
        private readonly GameObject _dialoguePanel;
        private readonly Text _speakerText;
        private readonly Text _bodyText;
        private readonly Text _choiceText;

        private PrototypeHud(GameObject root, Text promptText, GameObject dialoguePanel, Text speakerText, Text bodyText, Text choiceText)
        {
            _root = root;
            _promptText = promptText;
            _dialoguePanel = dialoguePanel;
            _speakerText = speakerText;
            _bodyText = bodyText;
            _choiceText = choiceText;
        }

        public static PrototypeHud Create()
        {
            var root = new GameObject("PrototypeHud");
            Object.DontDestroyOnLoad(root);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            var font = LoadBuiltinFont();

            var prompt = CreatePanel("InteractPrompt", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(320f, 56f), new Color(0f, 0f, 0f, 0.55f));
            var promptText = CreateText("PromptText", prompt.transform, font, 24, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var dialoguePanel = CreatePanel("DialoguePanel", root.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 180f), new Vector2(760f, 240f), new Color(0.12f, 0.10f, 0.10f, 0.92f));
            var speakerText = CreateText("SpeakerText", dialoguePanel.transform, font, 26, TextAnchor.UpperLeft, new Color(1f, 0.92f, 0.65f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -18f), new Vector2(-20f, -58f));
            var bodyText = CreateText("BodyText", dialoguePanel.transform, font, 24, TextAnchor.UpperLeft, Color.white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -60f), new Vector2(-20f, -130f));
            var choiceText = CreateText("ChoiceText", dialoguePanel.transform, font, 22, TextAnchor.UpperLeft, new Color(0.78f, 0.92f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 18f), new Vector2(-20f, 96f));

            return new PrototypeHud(root, promptText, dialoguePanel, speakerText, bodyText, choiceText);
        }

        private static Font LoadBuiltinFont()
        {
            var preferredFonts = new[]
            {
                "LegacyRuntime.ttf",
                "Arial.ttf"
            };

            foreach (var fontName in preferredFonts)
            {
                try
                {
                    var font = Resources.GetBuiltinResource<Font>(fontName);
                    if (font != null)
                    {
                        return font;
                    }
                }
                catch
                {
                    // Unity changes the builtin font name across versions.
                }
            }

            Debug.LogError("Could not load a builtin font. UI text will not render correctly.");
            return null;
        }

        public void Render(PrototypeSnapshot snapshot)
        {
            var showPrompt = !snapshot.IsDialogueOpen && !string.IsNullOrEmpty(snapshot.HoverLabel);
            _promptText.text = showPrompt ? $"Z / Enter {snapshot.HoverPrompt}\n{snapshot.HoverLabel}" : string.Empty;
            _promptText.transform.parent.gameObject.SetActive(showPrompt);

            _dialoguePanel.SetActive(snapshot.IsDialogueOpen && snapshot.Dialogue != null);
            if (!_dialoguePanel.activeSelf || snapshot.Dialogue == null)
            {
                return;
            }

            _speakerText.text = snapshot.Dialogue.Speaker;
            _bodyText.text = snapshot.Dialogue.Text;
            _choiceText.text = BuildChoiceBlock(snapshot.Dialogue);
        }

        private static string BuildChoiceBlock(DialogueSnapshot dialogue)
        {
            if (dialogue.Choices == null || dialogue.Choices.Length == 0)
            {
                return "1. 结束";
            }

            var lines = new string[dialogue.Choices.Length];
            for (var i = 0; i < dialogue.Choices.Length; i++)
            {
                var choice = dialogue.Choices[i];
                lines[i] = $"{choice.Index}. {choice.Text}";
            }

            return string.Join("\n", lines);
        }

        private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            int size,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);

            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
