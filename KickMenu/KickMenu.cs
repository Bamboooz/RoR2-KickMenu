using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using System;
using System.IO;
using UnityEngine;

namespace KickMenu
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("___riskofthunder.RoR2BepInExPack", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.HardDependency)]
    public class KickMenuPlugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource Log;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Bamboooz";
        public const string PluginName = "KickMenu";
        public const string PluginVersion = "1.0.4";

        #region mod

        public void Awake()
        {
            Log = Logger;

            try
            {
                ModSettingsManager.SetModDescription("A Risk of Rain 2 mod for lobby management. Open the menu using F1.");

                string pluginDir = Path.GetDirectoryName(Info.Location);
                string iconPath = Path.Combine(pluginDir, "icon.png");

                if (!File.Exists(iconPath))
                {
                    iconPath = Path.Combine(Path.GetDirectoryName(pluginDir), "icon.png");
                }

                if (File.Exists(iconPath))
                {
                    Texture2D tex = new Texture2D(2, 2);

                    if (tex.LoadImage(File.ReadAllBytes(iconPath)))
                    {
                        ModSettingsManager.SetModIcon(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
                    }
                }

                ModConfig.Init(Config);

                Menu.Init();
            }
            catch (Exception e)
            {
                Log.LogError("Failed to initialize KickMenu: " + e.Message);
            }
        }

        public void Update()
        {
            Menu.Update();
        }

        public void OnDestroy()
        {
            Menu.Destroy();
        }

        #endregion

        #region config

        public static class ModConfig
        {
            public static ConfigEntry<bool> ConfirmKick;
            public static ConfigEntry<bool> BroadcastKick;
            public static ConfigEntry<KeyboardShortcut> OpenMenuKey;

            public static void Init(ConfigFile config)
            {
                ConfirmKick = config.Bind(
                    "General",
                    "Confirmation Dialog",
                    true,
                    "If enabled, a confirmation dialog will appear before kicking / banning a player.\n\nDefault: true"
                );

                BroadcastKick = config.Bind(
                    "General",
                    "Kick / Ban Broadcast",
                    false,
                    "If enabled, a message will be sent to chat when a player is kicked / banned.\n\nDefault: false"
                );

                OpenMenuKey = config.Bind(
                    "General",
                    "Open Kick Menu Key",
                    new KeyboardShortcut(KeyCode.F1),
                    "Keybind used to open the player management menu. \n\nDefault: F1"
                );

                ModSettingsManager.AddOption(new CheckBoxOption(ConfirmKick));
                ModSettingsManager.AddOption(new CheckBoxOption(BroadcastKick));
                ModSettingsManager.AddOption(new KeyBindOption(OpenMenuKey));
            }
        }

        #endregion
    }
}
