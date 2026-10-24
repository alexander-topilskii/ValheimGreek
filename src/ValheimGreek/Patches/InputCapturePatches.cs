using System;
using HarmonyLib;

namespace ValheimGreek
{
    [HarmonyPatch(typeof(Player), "AutoPickup")]
    public static class AutoPickupPatch
    {
        private static bool Prefix()
        {
            try
            {
                if (!PluginConfig.Enabled.Value || !PluginConfig.DisableAutoPickup.Value)
                    return true;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    public static class TakeInputPatch
    {
        private static bool Prefix(ref bool __result)
        {
            try
            {
                if (!QuizGui.IsOpen)
                    return true;
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    public static class MouseCapturePatch
    {
        private static bool Prefix()
        {
            try
            {
                if (!QuizGui.IsOpen)
                    return true;
                UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
                UnityEngine.Cursor.visible = true;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }
    }

    /// <summary>
    /// Левый клик по варианту — это же кнопка Attack. Её читает PlayerController,
    /// а не Player.TakeInput, поэтому удар нужно гасить здесь.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    public static class AttackInputPatch
    {
        private static void Prefix(
            ref bool attack,
            ref bool attackHold,
            ref bool secondaryAttack,
            ref bool secondaryAttackHold)
        {
            try
            {
                if (!QuizGui.BlocksAttack)
                    return;
                attack = false;
                attackHold = false;
                secondaryAttack = false;
                secondaryAttackHold = false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    public static class StartAttackPatch
    {
        private static bool Prefix(Humanoid __instance, ref bool __result)
        {
            try
            {
                if (!QuizGui.BlocksAttack || __instance != Player.m_localPlayer)
                    return true;
                __result = false;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(Menu), "Update")]
    public static class MenuUpdatePatch
    {
        private static bool Prefix()
        {
            try
            {
                if (QuizGui.IsOpen || QuizGui.SuppressMenuFrame == UnityEngine.Time.frameCount)
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                return true;
            }
        }
    }
}
