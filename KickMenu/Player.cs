using RoR2;
using RoR2.Networking;
using System;
using System.Linq;
using UnityEngine.Networking;

using static KickMenu.KickMenuPlugin;

namespace KickMenu
{
    internal class Player
    {
        public static bool IsHost()
        {
            return NetworkServer.active;
        }

        public static bool IsHost(NetworkUser networkUser)
        {
            return NetworkUser.readOnlyLocalPlayersList.Contains(networkUser);
        }

        public static bool IsSolo()
        {
            return NetworkUser.readOnlyInstancesList.All(user => user.isLocalPlayer);
        }

        public static ulong GetId(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return 0;
            }

            return networkUser.id.value;
        }

        public static bool Kick(NetworkUser networkUser, Action onKick)
        {
            try
            {
                if (networkUser == null)
                {
                    return false;
                }

                if (!IsHost())
                {
                    return false;
                }

                if (IsHost(networkUser))
                {
                    Log.Warning($"Attempted to kick host ({networkUser.userName}). Action aborted.");

                    return false;
                }

                NetworkConnection client = networkUser.connectionToClient;

                if (client == null)
                {
                    Log.Warning($"No connection found for player: {networkUser.userName}");

                    return false;
                }

                if (!client.isReady)
                {
                    Log.Warning($"Client connection for {networkUser.userName} is not ready. Connection ID: {client.connectionId}");
                }

                Log.Info($"Kicking player {networkUser.userName} (Connection ID: {client.connectionId})");

                var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");

                NetworkManagerSystem.singleton.ServerKickClient(client, reason);

                if (ModConfig.BroadcastKick.Value)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = $"<color=#e57373>Kicked player:</color> <color=#ffffff><noparse>{networkUser.userName}</noparse></color>"
                    });
                }

                onKick?.Invoke();

                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Exception in KickPlayer: {e.Message}\n{e.StackTrace}");

                return false;
            }
        }
    }
}
