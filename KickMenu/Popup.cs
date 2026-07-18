using System;
using RoR2;
using RoR2.UI;
using RiskOfOptions.Components.Options;

using static KickMenu.KickMenuPlugin;

namespace KickMenu
{
    internal static class Popup
    {
        private static void OnButtonClicked(NetworkUser networkUser, string message, Action<NetworkUser> action)
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
                action(networkUser);

                return;
            }

            SimpleDialogBox dialog = SimpleDialogBox.Create();

            dialog.gameObject.AddComponent<RooEscapeRouter>().escapePressed.AddListener(() =>
            {
                if (dialog && dialog.rootObject)
                {
                    UnityEngine.Object.Destroy(dialog.rootObject);
                }
            });

            dialog.headerLabel.text = $"Confirm {message}";
            dialog.descriptionLabel.text = $"Are you sure you want to {message} {networkUser.userName}?";

            dialog.AddActionButton(() => action(networkUser), "Yes", true);
            dialog.AddCancelButton("Cancel");
        }

        public static void OpenKickPopup(NetworkUser networkUser)
        {
            OnButtonClicked(networkUser, "kick", Player.Kick);
        }

        public static void OpenBanPopup(NetworkUser networkUser)
        {
            OnButtonClicked(networkUser, "ban", Player.Ban);
        }
    }
}
