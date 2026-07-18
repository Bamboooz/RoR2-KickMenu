using BepInEx.Configuration;
using RoR2;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

using static KickMenu.KickMenuPlugin;

namespace KickMenu
{
    internal class Menu
    {
        private static GameObject buttonPrefab;

        private static GameObject menuPanel;
        private static Transform contentRoot;

        private static bool menuOpen = false;

        private static readonly KeyboardShortcut closeKey = new(KeyCode.Escape);

        public static void Init()
        {
            buttonPrefab = Addressables
                .LoadAssetAsync<GameObject>("RoR2/Base/UI/GenericMenuButton.prefab")
                .WaitForCompletion();

            NetworkUser.onNetworkUserDiscovered += OnNetworkUserDiscovered;

            On.RoR2.NetworkUser.OnDestroy += OnNetworkUserDestroyed;
        }

        public static void Update()
        {
            if (ModConfig.OpenMenuKey.Value.IsDown())
            {
                ToggleMenu();
            }

            if (menuOpen && closeKey.IsDown())
            {
                menuOpen = false;

                menuPanel.SetActive(menuOpen);
            }
        }

        public static void Destroy()
        {
            NetworkUser.onNetworkUserDiscovered -= OnNetworkUserDiscovered;

            On.RoR2.NetworkUser.OnDestroy -= OnNetworkUserDestroyed;
        }

        private static void OnNetworkUserDiscovered(NetworkUser networkUser)
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

        private static void OnNetworkUserDestroyed(On.RoR2.NetworkUser.orig_OnDestroy orig, NetworkUser self)
        {
            orig(self);

            if (menuOpen)
            {
                RoR2Application.onNextUpdate += RefreshPlayerList;
            }
        }

        private static void ToggleMenu()
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

        private static void CreateMenu()
        {
            GameObject canvasObj = new GameObject("KickMenuCanvas");

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            Object.DontDestroyOnLoad(canvasObj);

            menuPanel = new GameObject("KickMenuPanel");

            menuPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();

            panelRect.sizeDelta = new Vector2(750, 0);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = menuPanel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.75f);

            VerticalLayoutGroup layout = menuPanel.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20, 20, 20, 20);
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

        private static void RefreshPlayerList()
        {
            if (contentRoot == null)
            {
                return;
            }

            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

            foreach (Transform child in contentRoot)
            {
                Object.Destroy(child.gameObject);
            }

            foreach (NetworkUser user in NetworkUser.readOnlyInstancesList)
            {
                CreatePlayerEntry(user);
            }
        }

        private static void CreatePlayerEntry(NetworkUser user)
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

            if (Player.IsHost() && !Player.IsHost(user))
            {
                CreateButton(row.transform, "Kick", () =>
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

                    ToggleMenu();

                    Popup.OpenKickPopup(user);
                });


                CreateButton(row.transform, "Ban", () =>
                {
                    UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

                    ToggleMenu();

                    Popup.OpenBanPopup(user);
                });
            }

            ulong userId = Steam.GetSteamId(user);

            GameObject profileButton = CreateButton(row.transform, "Profile", () =>
            {
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);

                ToggleMenu();

                Steam.OpenSteamProfile(userId);
            });

            if (userId == 0)
            {
                profileButton.SetActive(false);
            }
        }

        private static GameObject CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            if (!buttonPrefab)
            {
                return null;
            }

            GameObject button = Object.Instantiate(buttonPrefab, parent);

            button.name = text + "Button";

            LayoutElement element = button.AddComponent<LayoutElement>();

            element.preferredWidth = 100;
            element.preferredHeight = 40;

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText)
            {
                buttonText.text = text;
                buttonText.fontSize = 16;
                buttonText.alignment = TextAlignmentOptions.Center;
            }

            HGButton hgButton = button.GetComponent<HGButton>();

            if (hgButton)
            {
                hgButton.onClick.RemoveAllListeners();

                hgButton.onClick.AddListener(action);
            }

            return button;
        }
    }
}
