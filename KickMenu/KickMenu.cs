using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Components.Options;
using RiskOfOptions.Options;
using RoR2;
using RoR2.UI;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace KickMenu
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency("___riskofthunder.RoR2BepInExPack", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.HardDependency)]
    public class KickMenuPlugin : BaseUnityPlugin
    {
        private static GameObject buttonPrefab;
        internal static BepInEx.Logging.ManualLogSource Log;

        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Bamboooz";
        public const string PluginName = "KickMenu";
        public const string PluginVersion = "1.0.3";

        private GameObject menuPanel;
        private Transform contentRoot;

        private bool menuOpen = false;

        #region mod

        public void Awake()
        {
            Log = Logger;

            try
            {
                ModSettingsManager.SetModDescription("A Risk of Rain 2 mod that allows you to kick players from your lobby as well as during the game. Default keybind to open the menu is F1.");

                string pluginDir = System.IO.Path.GetDirectoryName(Info.Location);
                string iconPath = System.IO.Path.Combine(pluginDir, "icon.png");

                if (!File.Exists(iconPath))
                {
                    iconPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(pluginDir), "icon.png");
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

                NetworkUser.onNetworkUserDiscovered += OnNetworkUserDiscovered;

                On.RoR2.NetworkUser.OnDestroy += OnNetworkUserDestroyed;

                buttonPrefab = Addressables
                    .LoadAssetAsync<GameObject>("RoR2/Base/UI/GenericMenuButton.prefab")
                    .WaitForCompletion();
            }
            catch (Exception e)
            {
                Log.LogError("Failed to initialize KickMenu: " + e.Message);
            }
        }

        public void OnDestroy()
        {
            NetworkUser.onNetworkUserDiscovered -= OnNetworkUserDiscovered;

            On.RoR2.NetworkUser.OnDestroy -= OnNetworkUserDestroyed;
        }

        public void Update()
        {
            if (ModConfig.OpenMenuKey.Value.IsDown())
            {
                ToggleMenu();
            }

            if (new KeyboardShortcut(KeyCode.Escape).IsDown())
            {
                menuOpen = false;

                menuPanel.SetActive(menuOpen);
            }
        }

        #endregion

        #region kick menu

        private void OnNetworkUserDiscovered(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return;
            }

            if (menuOpen)
            {
                RefreshPlayerList();
            }
        }

        private void OnNetworkUserDestroyed(On.RoR2.NetworkUser.orig_OnDestroy orig, NetworkUser self)
        {
            if (self != null && menuOpen)
            {
                RefreshPlayerList();
            }

            orig(self);
        }

        private void ToggleMenu()
        {
            if (menuPanel == null)
            {
                CreateMenu();
            }

            menuOpen = !menuOpen;

            menuPanel.SetActive(menuOpen);

            if (menuOpen)
            {
                RefreshPlayerList();
            }
        }

        private void CreateMenu()
        {
            GameObject canvasObj = new GameObject("KickMenuCanvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasObj);

            menuPanel = new GameObject("KickMenuPanel");

            menuPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();

            panelRect.sizeDelta = new Vector2(750, 0);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = menuPanel.AddComponent<Image>();

            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20,20,20,20);
            layout.spacing = 8;

            layout.childAlignment = TextAnchor.UpperCenter;

            layout.childControlWidth = true;
            layout.childControlHeight = true;

            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = menuPanel.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            contentRoot = menuPanel.transform;

            menuPanel.SetActive(false);
        }

        private void RefreshPlayerList()
        {
            foreach (Transform child in contentRoot)
            {
                Destroy(child.gameObject);
            }

            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                CreatePlayerEntry(user);
            }
        }

        private void CreatePlayerEntry(NetworkUser user)
        {
            GameObject row = new GameObject("PlayerRow");

            row.transform.SetParent(contentRoot, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();

            rowRect.sizeDelta = new Vector2(700, 50);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;

            layout.childControlWidth = true;
            layout.childControlHeight = true;

            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();

            rowElement.preferredHeight = 50;

            GameObject nameObj = new GameObject("PlayerName");

            nameObj.transform.SetParent(row.transform, false);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();

            nameText.text = user.userName;
            nameText.fontSize = 20;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = Color.white;

            LayoutElement nameElement = nameObj.AddComponent<LayoutElement>();

            nameElement.flexibleWidth = 1;
            nameElement.preferredHeight = 40;

            if (!buttonPrefab)
            {
                return;
            }

            if (Player.IsHost() && !Player.IsHost(user))
            {
                GameObject kickButton = Instantiate(buttonPrefab, row.transform);

                kickButton.name = "KickButton";

                LayoutElement kickElement = kickButton.AddComponent<LayoutElement>();
                kickElement.preferredWidth = 100;
                kickElement.preferredHeight = 40;

                TextMeshProUGUI kickText = kickButton.GetComponentInChildren<TextMeshProUGUI>();

                if (kickText)
                {
                    kickText.text = "Kick";
                    kickText.fontSize = 16;
                    kickText.alignment = TextAlignmentOptions.Center;
                }

                HGButton kickHgButton = kickButton.GetComponent<HGButton>();

                if (kickHgButton)
                {
                    kickHgButton.onClick.RemoveAllListeners();

                    kickHgButton.onClick.AddListener(() =>
                    {
                        OnKickButtonClicked(user);
                    });
                }
            }

            GameObject profileButton = Instantiate(buttonPrefab, row.transform);

            profileButton.name = "SteamProfileButton";

            LayoutElement profileElement = profileButton.AddComponent<LayoutElement>();

            profileElement.preferredWidth = 100;
            profileElement.preferredHeight = 40;

            TextMeshProUGUI profileText = profileButton.GetComponentInChildren<TextMeshProUGUI>();

            if (profileText)
            {
                profileText.text = "Profile";
                profileText.fontSize = 16;
                profileText.alignment = TextAlignmentOptions.Center;
            }

            HGButton profileHgButton = profileButton.GetComponent<HGButton>();

            if (profileHgButton)
            {
                profileHgButton.onClick.RemoveAllListeners();

                profileHgButton.onClick.AddListener(() =>
                {
                    Steam.OpenSteamProfile(Player.GetId(user));
                });
            }

            ulong userId = Player.GetId(user);

            if (userId == 0)
            {
                profileButton.SetActive(false);
            }
        }

        #endregion

        #region kick logic

        private void OnKickButtonClicked(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return;
            }

            if (!Player.IsHost())
            {
                return;
            }

            if (!ModConfig.ConfirmKick.Value)
            {
                Player.Kick(networkUser, RefreshPlayerList);

                return;
            }

            ToggleMenu();

            SimpleDialogBox dialog = SimpleDialogBox.Create();

            dialog.gameObject.AddComponent<RooEscapeRouter>().escapePressed.AddListener(() =>
            {
                if (dialog && dialog.rootObject)
                {
                    Destroy(dialog.rootObject);
                }
            });

            dialog.headerLabel.text = "Confirm Kick";
            dialog.descriptionLabel.text = $"Are you sure you want to kick {networkUser.userName}?";

            dialog.AddActionButton(() => Player.Kick(networkUser, RefreshPlayerList), "Yes", true);
            dialog.AddCancelButton("Cancel");
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
                    "If enabled, a confirmation dialog will appear before kicking a player.\n\nDefault: true"
                );

                BroadcastKick = config.Bind(
                    "General",
                    "Kick Broadcast",
                    false,
                    "If enabled, a message will be sent to chat when a player is kicked.\n\nDefault: false"
                );

                OpenMenuKey = config.Bind(
                    "Controls",
                    "Open Kick Menu Key",
                    new KeyboardShortcut(KeyCode.F1),
                    "Keybind used to open the player kick menu. \n\nDefault: F1"
                );

                ModSettingsManager.AddOption(new CheckBoxOption(ConfirmKick));
                ModSettingsManager.AddOption(new CheckBoxOption(BroadcastKick));
                ModSettingsManager.AddOption(new KeyBindOption(OpenMenuKey));
            }
        }

        #endregion
    }
}
