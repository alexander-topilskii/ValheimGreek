using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ValheimGreek
{
    [BepInPlugin("com.valheimgreek.mod", "ValheimGreek", "0.1.0")]
    public sealed class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log = null!;

        private void Awake()
        {
            Log = Logger;
            PluginConfig.Bind(Config);
            try
            {
                var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
                var path = DictionaryService.ResolvePath(directory, PluginConfig.DictionaryFile.Value);
                DictionaryService.Load(path);
                Log.LogInfo("Loaded " + DictionaryService.Words.Count + " words from " + path);
            }
            catch (System.Exception ex)
            {
                Log.LogError("Dictionary was not loaded. Pickup stays vanilla until the file is fixed.");
                Log.LogError(ex);
            }

            GreekSpeaker.Prepare();
            var harmony = new Harmony("com.valheimgreek.mod");
            harmony.PatchAll(typeof(Plugin).Assembly);
            Log.LogInfo("ValheimGreek patches applied.");
        }

        private void OnGUI()
        {
            QuizGui.Draw();
        }

        private void LateUpdate()
        {
            QuizGui.TrackAttackSuppression();
            if (!QuizGui.IsOpen)
                return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
