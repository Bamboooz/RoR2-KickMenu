using RoR2;
using System;
using System.Diagnostics;

using static KickMenu.KickMenuPlugin;

namespace KickMenu
{
    internal static class Steam
    {
        public static ulong GetSteamId(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return 0;
            }

            return Convert.ToUInt64(networkUser.Network_id.steamId.value);
        }

        private static string OpenSteamProfileCommand(ulong id)
        {
            if (id == 0)
            {
                return null;
            }

            return $"steam://openurl/https://steamcommunity.com/profiles/{id}";
        }

        public static void OpenSteamProfile(ulong id)
        {
            if (id == 0)
            {
                return;
            }

            var command = OpenSteamProfileCommand(id);

            if (command == null)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = command,
                    UseShellExecute = true
                });
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to open Steam profile: {e.Message}");
            }
        }
    }
}
