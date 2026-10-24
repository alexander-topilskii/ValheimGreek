using System;
using HarmonyLib;
using ValheimGreek.Core;

namespace ValheimGreek
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Interact))]
    public static class ItemDropInteractPatch
    {
        private static readonly System.Random Rng = new System.Random();

        private static bool Prefix(ItemDrop __instance, Humanoid character, bool repeat, bool alt, ref bool __result)
        {
            try
            {
                if (InteractBypass.Active || !PluginConfig.Enabled.Value)
                    return true;
                if (__instance == null || character == null || Player.m_localPlayer == null || character != Player.m_localPlayer)
                    return true;
                if (QuizGui.IsOpen)
                {
                    __result = false;
                    return false;
                }

                if (repeat || GameUi.BlocksQuiz() || !CanQuizItem(__instance, alt))
                    return true;
                if (!DictionaryService.CanQuiz || !QuizBuilder.TryCreate(DictionaryService.Words, Rng, out var question))
                    return true;

                QuizGui.Open(__instance, repeat, alt, ItemMissCounter.Get(__instance), question);
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }

        private static bool CanQuizItem(ItemDrop item, bool alt)
        {
            var data = item.m_itemData;
            if (data == null || data.m_shared == null)
                return false;
            if (item.InTar())
                return false;
            if (!alt && data.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable && item.IsPiece())
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.GetHoverText))]
    public static class ItemDropHoverPatch
    {
        private const string Marker = "\nНужно верных:";

        private static void Postfix(ItemDrop __instance, ref string __result)
        {
            try
            {
                if (!PluginConfig.Enabled.Value || __instance == null)
                    return;
                var text = __result ?? "";
                var marker = text.LastIndexOf(Marker, StringComparison.Ordinal);
                if (marker >= 0)
                    text = text.Substring(0, marker);
                __result = text + Marker + " " + (ItemMissCounter.Get(__instance) + 1);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }
}
