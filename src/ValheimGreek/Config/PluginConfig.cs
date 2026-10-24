using BepInEx;
using BepInEx.Configuration;

namespace ValheimGreek
{
    public static class PluginConfig
    {
        public static ConfigEntry<bool> Enabled = null!;
        public static ConfigEntry<bool> DisableAutoPickup = null!;
        public static ConfigEntry<string> DictionaryFile = null!;

        public static void Bind(ConfigFile config)
        {
            Enabled = config.Bind("General", "Enabled", true, "Выключено — автолут и подбор как в ваниле.");
            DisableAutoPickup = config.Bind(
                "General",
                "DisableAutoPickup",
                true,
                "Предметы остаются на земле, пока игрок сам не взаимодействует с ними. Имеет силу только при Enabled.");
            DictionaryFile = config.Bind(
                "General",
                "DictionaryFile",
                "dictionary.json",
                "Имя файла рядом с DLL или абсолютный путь к словарю.");
        }

        public static void SetEnabled(bool enabled)
        {
            if (Enabled.Value == enabled)
                return;
            Enabled.Value = enabled;
            if (!enabled && QuizGui.IsOpen)
                QuizGui.Close(false);
        }
    }
}
