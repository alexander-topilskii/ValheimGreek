using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;

namespace ValheimGreek
{
    public static class GameplayModToggle
    {
        public const string ToggleName = "ValheimGreekEnabled";
        public const string LabelName = "ValheimGreekEnabledLabel";
        public const string Label = "Греческая викторина";

        public static Toggle? Toggle;

        public static Toggle? FieldToggle(GameplaySettings settings, string name)
        {
            return AccessTools.Field(typeof(GameplaySettings), name)?.GetValue(settings) as Toggle;
        }

        public static void ApplyLabel()
        {
            if (Toggle == null)
                return;
            var root = Toggle.transform.parent;
            if (root == null)
                return;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform == Toggle.transform || !text.transform.IsChildOf(root))
                    continue;
                var owner = text.transform;
                while (owner.parent != null && owner.parent != root)
                    owner = owner.parent;
                if (owner.name == LabelName || owner.name == ToggleName || text.transform.IsChildOf(Toggle.transform))
                    text.text = Label;
            }

            foreach (var text in Toggle.GetComponentsInChildren<TMP_Text>(true))
                text.text = Label;
        }
    }

    [HarmonyPatch(typeof(GameplaySettings), nameof(GameplaySettings.Initialize))]
    public static class GameplaySettingsInitPatch
    {
        private static readonly string[] ShiftFields =
        {
            "m_autoBackups",
            "m_autoBackupsText",
            "m_deleteAccount",
            "m_radialSettingsButton",
            "m_blockedPlayerList",
            "m_cloudStorageWarning"
        };

        private static void Postfix(GameplaySettings __instance)
        {
            try
            {
                InsertRow(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }

        private static void InsertRow(GameplaySettings settings)
        {
            var skip = GameplayModToggle.FieldToggle(settings, "m_skipIntroCinematic");
            var above = GameplayModToggle.FieldToggle(settings, "m_showBuildPieceAuthor")
                ?? GameplayModToggle.FieldToggle(settings, "m_enableConsole");
            if (skip == null)
                return;

            var skipRt = skip.GetComponent<RectTransform>();
            var pitch = RowPitch(skipRt, above != null ? above.GetComponent<RectTransform>() : null);
            var row = RowRoot(skip);
            var existing = row.parent.Find(GameplayModToggle.ToggleName);
            if (existing == null && row.parent.Find(GameplayModToggle.LabelName) != null)
                existing = row.parent.Find(GameplayModToggle.LabelName);
            if (existing != null)
                UnityEngine.Object.Destroy(existing.gameObject);

            if (IsLabeledRow(row, skip))
            {
                PlaceClonedRow(row, pitch);
                ShiftBelow(settings, skipRt, pitch);
                GameplayModToggle.ApplyLabel();
                Plugin.Log.LogInfo("Gameplay toggle row added.");
                return;
            }

            PlaceClonedToggle(skip, pitch);
            PlaceClonedLabel(settings, skip, pitch);
            ShiftBelow(settings, skipRt, pitch);
            GameplayModToggle.ApplyLabel();
            Plugin.Log.LogInfo("Gameplay toggle added.");
        }

        private static Transform RowRoot(Toggle toggle)
        {
            var parent = toggle.transform.parent;
            if (parent != null && parent.GetComponent<GameplaySettings>() == null)
                return parent;
            return toggle.transform;
        }

        private static bool IsLabeledRow(Transform row, Toggle toggle)
        {
            if (row == toggle.transform)
                return false;
            if (row.GetComponent<GameplaySettings>() != null)
                return false;
            return row.GetComponentsInChildren<TMP_Text>(true).Length > 0;
        }

        private static float RowPitch(RectTransform skip, RectTransform? above)
        {
            if (above != null)
            {
                var local = skip.anchoredPosition.y - above.anchoredPosition.y;
                if (Mathf.Abs(local) > 8f)
                    return Mathf.Abs(local);
                var world = skip.transform.position.y - above.transform.position.y;
                if (Mathf.Abs(world) > 0.01f)
                    return Mathf.Abs(ToParentDeltaY(skip, world));
            }

            return 36f;
        }

        private static float ToParentDeltaY(RectTransform self, float worldDy)
        {
            var parent = self.parent as RectTransform;
            if (parent == null)
                return worldDy;
            var shifted = parent.InverseTransformPoint(self.position + new Vector3(0f, -Mathf.Abs(worldDy), 0f));
            return Mathf.Abs(self.localPosition.y - shifted.y);
        }

        private static void PlaceClonedRow(Transform row, float pitch)
        {
            var parent = row.parent;
            var cloneObject = UnityEngine.Object.Instantiate(row.gameObject, parent);
            cloneObject.name = GameplayModToggle.ToggleName;
            var cloneRt = cloneObject.GetComponent<RectTransform>();
            if (cloneRt != null)
            {
                var sourceRt = row.GetComponent<RectTransform>();
                cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -pitch);
                cloneRt.SetSiblingIndex(row.GetSiblingIndex() + 1);
            }

            var clone = cloneObject.GetComponentInChildren<Toggle>(true);
            BindToggle(clone);
        }

        private static void PlaceClonedToggle(Toggle skip, float pitch)
        {
            var parent = skip.transform.parent;
            var cloneObject = UnityEngine.Object.Instantiate(skip.gameObject, parent);
            cloneObject.name = GameplayModToggle.ToggleName;
            var cloneRt = cloneObject.GetComponent<RectTransform>();
            var sourceRt = skip.GetComponent<RectTransform>();
            cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -pitch);
            cloneRt.SetSiblingIndex(skip.transform.GetSiblingIndex() + 1);
            BindToggle(cloneObject.GetComponent<Toggle>());
        }

        private static void PlaceClonedLabel(GameplaySettings settings, Toggle skip, float pitch)
        {
            var label = FindAlignedLabel(settings.transform, skip);
            if (label == null)
                return;
            var sourceRt = label.rectTransform;
            var cloneObject = UnityEngine.Object.Instantiate(label.gameObject, sourceRt.parent);
            cloneObject.name = GameplayModToggle.LabelName;
            var cloneRt = cloneObject.GetComponent<RectTransform>();
            cloneRt.anchoredPosition = sourceRt.anchoredPosition + new Vector2(0f, -pitch);
            foreach (var text in cloneObject.GetComponentsInChildren<TMP_Text>(true))
                text.text = GameplayModToggle.Label;
        }

        private static TMP_Text? FindAlignedLabel(Transform root, Toggle skip)
        {
            var skipY = skip.transform.position.y;
            TMP_Text? best = null;
            var bestDy = float.MaxValue;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.transform == skip.transform || text.transform.IsChildOf(skip.transform))
                    continue;
                var dy = Mathf.Abs(text.transform.position.y - skipY);
                if (dy >= bestDy)
                    continue;
                bestDy = dy;
                best = text;
            }

            return best;
        }

        private static void BindToggle(Toggle? clone)
        {
            if (clone == null)
                return;
            clone.onValueChanged = new Toggle.ToggleEvent();
            clone.isOn = PluginConfig.Enabled.Value;
            clone.interactable = true;
            clone.gameObject.SetActive(true);
            var tooltip = clone.GetComponent<SettingsTooltip>() ?? clone.GetComponentInChildren<SettingsTooltip>(true);
            if (tooltip != null)
                UnityEngine.Object.Destroy(tooltip);
            GameplayModToggle.Toggle = clone;
        }

        private static void ShiftBelow(GameplaySettings settings, RectTransform skipRt, float pitch)
        {
            var skipY = skipRt.anchoredPosition.y;
            var parent = skipRt.parent as RectTransform;
            if (parent != null)
            {
                foreach (RectTransform child in parent)
                {
                    if (child.name == GameplayModToggle.ToggleName || child.name == GameplayModToggle.LabelName)
                        continue;
                    if (child == skipRt || child.IsChildOf(skipRt))
                        continue;
                    if (child.anchoredPosition.y < skipY - 1f)
                        child.anchoredPosition += new Vector2(0f, -pitch);
                }
            }

            foreach (var name in ShiftFields)
            {
                var component = AccessTools.Field(typeof(GameplaySettings), name)?.GetValue(settings) as Component;
                if (component == null)
                    continue;
                var rt = component.transform as RectTransform;
                if (rt == null || rt.name == GameplayModToggle.ToggleName)
                    continue;
                if (rt.parent == parent)
                    continue;
                rt.anchoredPosition += new Vector2(0f, -pitch);
            }
        }
    }

    [HarmonyPatch(typeof(GameplaySettings), nameof(GameplaySettings.OnTabOpen))]
    public static class GameplaySettingsTabOpenPatch
    {
        private static void Postfix()
        {
            try
            {
                GameplayModToggle.ApplyLabel();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }

    [HarmonyPatch(typeof(GameplaySettings), nameof(GameplaySettings.OnOkAsync))]
    public static class GameplaySettingsOkPatch
    {
        private static void Prefix()
        {
            try
            {
                if (GameplayModToggle.Toggle == null)
                    return;
                PluginConfig.SetEnabled(GameplayModToggle.Toggle.isOn);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }
}
