using System;
using System.Diagnostics;

namespace KickMenu
{
    internal class Steam
    {
        private static String OpenSteamProfileCommand(ulong id)
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
                Log.Error($"Failed to open Steam profile: {e.Message}");
            }
        }
    }
}
