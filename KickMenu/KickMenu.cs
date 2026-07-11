using BepInEx;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Components.Options;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Networking;
using RoR2.UI;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;

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
        public const string PluginVersion = "1.0.0";

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
            if (!IsHost())
            {
                return;
            }

            if (ModConfig.OpenMenuKey.Value.IsDown())
            {
                ToggleMenu();
            }
        }

        #endregion

        #region host

        private bool IsHost()
        {
            return NetworkServer.active;
        }

        private bool IsHost(NetworkUser networkUser)
        {
            return NetworkUser.readOnlyLocalPlayersList.Contains(networkUser);
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

        private bool IsSolo()
        {
            return !NetworkUser.readOnlyInstancesList.Any(user => !user.isLocalPlayer);
        }

        private void ToggleMenu()
        {
            if (menuPanel == null)
            {
                CreateMenu();
            }

            if (!menuOpen && IsSolo())
            {
                return;
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

            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            DontDestroyOnLoad(canvasObj);

            menuPanel = new GameObject("KickMenuPanel");
            menuPanel.transform.SetParent(canvasObj.transform);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();

            panelRect.sizeDelta = new Vector2(500, 450);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = menuPanel.AddComponent<Image>();

            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;

            ContentSizeFitter fitter = menuPanel.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
                if (IsHost(user))
                {
                    continue;
                }

                CreatePlayerEntry(user);
            }
        }

        private void CreatePlayerEntry(NetworkUser user)
        {
            GameObject row = new GameObject("PlayerRow");

            row.transform.SetParent(contentRoot);

            RectTransform rowRect = row.AddComponent<RectTransform>();

            rowRect.sizeDelta = new Vector2(450, 50);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 15;
            layout.childAlignment = TextAnchor.MiddleCenter;

            LayoutElement rowElement = row.AddComponent<LayoutElement>();

            rowElement.preferredHeight = 50;

            GameObject nameObj = new GameObject("PlayerName");

            nameObj.transform.SetParent(row.transform);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();

            nameRect.sizeDelta = new Vector2(280, 40);

            LayoutElement nameElement = nameObj.AddComponent<LayoutElement>();

            nameElement.preferredWidth = 280;

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();

            nameText.text = user.userName;
            nameText.fontSize = 20;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;

            nameText.color = Color.white;

            if (!buttonPrefab)
            {
                return;
            }

            GameObject kickButton = Instantiate(buttonPrefab, row.transform);

            kickButton.name = "KickButton";

            RectTransform buttonRect = kickButton.GetComponent<RectTransform>();

            buttonRect.sizeDelta = new Vector2(120, 40);

            LayoutElement buttonElement = kickButton.AddComponent<LayoutElement>();

            buttonElement.preferredWidth = 120;
            buttonElement.preferredHeight = 40;

            TextMeshProUGUI buttonText = kickButton.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText)
            {
                buttonText.text = "Kick";
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
            }

            HGButton hgButton = kickButton.GetComponent<HGButton>();

            if (hgButton)
            {
                hgButton.onClick.RemoveAllListeners();

                hgButton.onClick.AddListener(() =>
                {
                    OnKickButtonClicked(user);
                });
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

            if (!IsHost())
            {
                return;
            }

            if (!ModConfig.ConfirmKick.Value)
            {
                KickPlayer(networkUser);

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

            dialog.AddActionButton(() => KickPlayer(networkUser), "Yes", true);
            dialog.AddCancelButton("Cancel");
        }

        private void KickPlayer(NetworkUser networkUser)
        {
            try
            {
                if (networkUser == null)
                {
                    return;
                }

                if (!IsHost())
                {
                    return;
                }

                if (IsHost(networkUser))
                {
                    Log.LogWarning($"Attempted to kick host ({networkUser.userName}). Action aborted.");

                    return;
                }

                NetworkConnection client = networkUser.connectionToClient;

                if (client == null)
                {
                    Log.LogWarning($"No connection found for player: {networkUser.userName}");

                    return;
                }

                if (!client.isReady)
                {
                    Log.LogWarning($"Client connection for {networkUser.userName} is not ready. Connection ID: {client.connectionId}");
                }

                Log.LogInfo($"Kicking player {networkUser.userName} (Connection ID: {client.connectionId})");

                var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");

                NetworkManagerSystem.singleton.ServerKickClient(client, reason);

                if (ModConfig.BroadcastKick.Value)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = $"<color=#e57373>Kicked player:</color> <color=#ffffff><noparse>{networkUser.userName}</noparse></color>"
                    });
                }

                RefreshPlayerList();
            }
            catch (Exception e)
            {
                Log.LogError($"Exception in KickPlayer: {e.Message}\n{e.StackTrace}");
            }
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
