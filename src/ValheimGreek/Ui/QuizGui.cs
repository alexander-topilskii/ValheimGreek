using System;
using UnityEngine;
using ValheimGreek.Core;

namespace ValheimGreek
{
    public static class QuizGui
    {
        private static readonly System.Random Rng = new System.Random();
        private static GUISkin? _skin;
        private const float FeedbackSeconds = 1f;

        private static GUIStyle? _panelStyle;
        private static GUIStyle? _promptStyle;
        private static GUIStyle? _buttonStyle;
        private static GUIStyle? _correctStyle;
        private static GUIStyle? _wrongStyle;
        private static GUIStyle? _labelStyle;
        private static bool _skinReady;
        private static int _answerFrame = -1;
        private static int _pickedIndex = -1;
        private static float _feedbackUntil;

        public static bool IsOpen { get; private set; }
        public static int SuppressMenuFrame { get; private set; } = -1;
        public static PendingAction? Pending { get; private set; }

        /// <summary>
        /// Клик по ответу держит Attack. Гасим удар, пока викторина открыта
        /// и пока эта кнопка не отпущена после закрытия.
        /// </summary>
        public static bool SuppressAttack { get; private set; }

        public static bool BlocksAttack => IsOpen || SuppressAttack;

        public static void Open(ItemDrop item, bool repeat, bool alt, int misses, QuizQuestion question)
        {
            Pending = new PendingAction
            {
                Item = item,
                Repeat = repeat,
                Alt = alt,
                Required = misses + 1,
                Correct = 0,
                Question = question
            };
            IsOpen = true;
            ClearFeedback();
        }

        public static void Close(bool swallowEscape)
        {
            IsOpen = false;
            Pending = null;
            ClearFeedback();
            if (swallowEscape)
                SuppressMenuFrame = Time.frameCount;
        }

        public static void Draw()
        {
            if (!IsOpen || Pending == null || Pending.Question == null)
                return;

            EnsureSkin();
            CommitFeedbackIfDue();
            if (!IsOpen || Pending == null || Pending.Question == null)
                return;

            var previous = GUI.skin;
            if (_skin != null)
                GUI.skin = _skin;
            try
            {
                var showingFeedback = IsShowingFeedback;
                var choice = showingFeedback ? -1 : ReadKeyChoice();
                var width = 680f;
                var height = 520f;
                var area = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
                GUILayout.BeginArea(area, _panelStyle);
                var pending = Pending;
                var question = pending.Question;
                GUILayout.Label(pending.Correct + "/" + pending.Required, _labelStyle);
                GUILayout.Space(12f);
                GUILayout.Label(question.Prompt, _promptStyle);
                GUILayout.Space(16f);
                for (var i = 0; i < question.Options.Count; i++)
                {
                    var clicked = GUILayout.Button((i + 1) + ". " + question.Options[i], StyleFor(i, question.CorrectIndex), GUILayout.Height(56f));
                    if (!showingFeedback && clicked)
                        choice = i;
                }

                GUILayout.Space(8f);
                GUILayout.Label("Esc — оставить предмет", _labelStyle);
                GUILayout.EndArea();

                if (!showingFeedback && choice >= 0)
                    Answer(choice);
                else if (!showingFeedback && choice == -2)
                    Close(true);
            }
            finally
            {
                GUI.skin = previous;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private static int ReadKeyChoice()
        {
            var current = Event.current;
            if (current == null || current.type != EventType.KeyDown)
                return -1;
            if (current.keyCode == KeyCode.Escape)
            {
                current.Use();
                return -2;
            }

            var index = KeyIndex(current.keyCode);
            if (index < 0)
                return -1;
            current.Use();
            return index;
        }

        private static int KeyIndex(KeyCode key)
        {
            if (key == KeyCode.Alpha1 || key == KeyCode.Keypad1)
                return 0;
            if (key == KeyCode.Alpha2 || key == KeyCode.Keypad2)
                return 1;
            if (key == KeyCode.Alpha3 || key == KeyCode.Keypad3)
                return 2;
            if (key == KeyCode.Alpha4 || key == KeyCode.Keypad4)
                return 3;
            return -1;
        }

        private static void Answer(int index)
        {
            if (_answerFrame == Time.frameCount)
                return;
            var pending = Pending;
            if (pending == null || pending.Question == null)
                return;
            if (index < 0 || index >= pending.Question.Options.Count)
                return;
            _answerFrame = Time.frameCount;

            if (pending.Item == null)
            {
                Close(false);
                return;
            }

            SuppressAttack = true;
            GreekSpeaker.Speak(pending.Question.Options[index]);
            _pickedIndex = index;
            _feedbackUntil = Time.unscaledTime + FeedbackSeconds;
        }

        public static void TrackAttackSuppression()
        {
            bool held;
            try
            {
                held = ZInput.GetButton("Attack") || ZInput.GetButton("JoyAttack");
            }
            catch (Exception)
            {
                held = false;
            }

            if (IsOpen)
            {
                if (held)
                    SuppressAttack = true;
                return;
            }

            if (!held)
                SuppressAttack = false;
        }

        private static bool IsShowingFeedback => _pickedIndex >= 0 && _feedbackUntil > 0f;

        private static void CommitFeedbackIfDue()
        {
            if (!IsShowingFeedback || Time.unscaledTime < _feedbackUntil)
                return;

            var pending = Pending;
            var picked = _pickedIndex;
            ClearFeedback();
            if (pending == null || pending.Question == null || pending.Item == null)
            {
                Close(false);
                return;
            }

            if (picked != pending.Question.CorrectIndex)
            {
                ItemMissCounter.Increment(pending.Item);
                Plugin.Log.LogInfo("Wrong answer for " + pending.Item.name + ", next attempt needs " + (ItemMissCounter.Get(pending.Item) + 1));
                Close(false);
                return;
            }

            pending.Correct++;
            if (pending.Correct < pending.Required)
            {
                if (!QuizBuilder.TryCreate(DictionaryService.Words, Rng, out var next))
                {
                    Plugin.Log.LogWarning("Could not build the next question. Pickup continues.");
                    Finish(pending);
                    return;
                }

                pending.Question = next;
                return;
            }

            Finish(pending);
        }

        private static void ClearFeedback()
        {
            _pickedIndex = -1;
            _feedbackUntil = 0f;
        }

        private static GUIStyle StyleFor(int index, int correctIndex)
        {
            if (IsShowingFeedback && index == correctIndex)
                return _correctStyle ?? _buttonStyle!;
            if (IsShowingFeedback && index == _pickedIndex)
                return _wrongStyle ?? _buttonStyle!;
            return _buttonStyle!;
        }

        private static void Finish(PendingAction pending)
        {
            var item = pending.Item;
            var player = Player.m_localPlayer;
            Close(false);
            if (item == null || player == null)
            {
                Plugin.Log.LogWarning("Pickup target disappeared before the answer was confirmed.");
                return;
            }

            InteractBypass.Run(item, player, false, pending.Alt);
        }

        private static void EnsureSkin()
        {
            if (_skinReady)
                return;
            _skinReady = true;

            var font = LoadFont();
            _skin = UnityEngine.Object.Instantiate(GUI.skin);
            var text = new Color(0.96f, 0.96f, 0.96f, 1f);
            _panelStyle = new GUIStyle(_skin.box)
            {
                font = font,
                padding = new RectOffset(28, 28, 22, 22)
            };
            Paint(_panelStyle, Solid(new Color(0.07f, 0.07f, 0.08f, 0.98f)), text);
            _promptStyle = new GUIStyle(_skin.label)
            {
                font = font,
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            Paint(_promptStyle, null, text);
            _buttonStyle = MakeButton(font, new Color(0.16f, 0.16f, 0.18f, 1f), text);
            _correctStyle = MakeButton(font, new Color(0.12f, 0.48f, 0.24f, 1f), text);
            _wrongStyle = MakeButton(font, new Color(0.62f, 0.14f, 0.14f, 1f), text);
            _labelStyle = new GUIStyle(_skin.label)
            {
                font = font,
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            Paint(_labelStyle, null, new Color(0.82f, 0.82f, 0.84f, 1f));
        }

        private static GUIStyle MakeButton(Font? font, Color background, Color text)
        {
            var style = new GUIStyle(_skin!.button)
            {
                font = font,
                fontSize = 20,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter
            };
            Paint(style, Solid(background), text);
            return style;
        }

        private static void Paint(GUIStyle style, Texture2D? background, Color text)
        {
            GUIStyleState[] states =
            {
                style.normal, style.hover, style.active, style.focused,
                style.onNormal, style.onHover, style.onActive, style.onFocused
            };
            foreach (var state in states)
            {
                if (background != null)
                    state.background = background;
                state.textColor = text;
            }
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static Font? LoadFont()
        {
            var installed = Font.GetOSInstalledFontNames();
            string[] preferred = { "Arial", "Times New Roman", "Noto Sans", "Helvetica", "Lucida Grande" };
            foreach (var name in preferred)
            {
                foreach (var existing in installed)
                {
                    if (!string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var font = Font.CreateDynamicFontFromOSFont(existing, 20);
                    if (font != null)
                    {
                        Plugin.Log.LogInfo("Quiz font: " + existing);
                        return font;
                    }
                }
            }

            if (installed.Length == 0)
            {
                Plugin.Log.LogWarning("No OS font found. Greek options may render as boxes.");
                return null;
            }

            Plugin.Log.LogInfo("Quiz font: " + installed[0]);
            return Font.CreateDynamicFontFromOSFont(installed[0], 20);
        }
    }
}
