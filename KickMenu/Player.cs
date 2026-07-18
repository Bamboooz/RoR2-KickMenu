using System;
using RoR2;
using RoR2.Networking;
using UnityEngine.Networking;

using static KickMenu.KickMenuPlugin;

namespace KickMenu
{
    internal static class Player
    {
        public static bool IsHost()
        {
            return NetworkServer.active;
        }

        public static bool IsHost(NetworkUser user)
        {
            if (!NetworkServer.active)
            {
                return false;
            }

            return user.isLocalPlayer;
        }

        private static NetworkConnection GetNetworkConnection(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return null;
            }

            if (!IsHost())
            {
                return null;
            }

            if (IsHost(networkUser))
            {
                Log.LogWarning($"Attempted to remove host ({networkUser.userName}). Action aborted.");

                return null;
            }

            return networkUser.connectionToClient;
        }

        private static void SendRemoveMessage(NetworkUser networkUser, string message)
        {
            if (ModConfig.BroadcastKick.Value)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#e57373>{message} player:</color> <color=#ffffff><noparse>{networkUser.userName}</noparse></color>"
                });
            }
        }

        public static void RemovePlayer(NetworkUser networkUser, string message, Action<NetworkConnection> action)
        { 
            NetworkConnection client = GetNetworkConnection(networkUser);

            if (client == null)
            {
                Log.LogWarning($"No connection found for player: {networkUser?.userName}");

                return;
            }

            if (!client.isReady)
            {
                Log.LogWarning($"Client connection for {networkUser.userName} is not ready. Connection ID: {client.connectionId}");
            }

            try
            { 
                Log.LogInfo($"Removing player {networkUser.userName} (Connection ID: {client.connectionId})");

                action(client);

                SendRemoveMessage(networkUser, message);
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to remove player {networkUser.userName}: {e}");
            }
        }

        public static void Kick(NetworkUser networkUser)
        {
            RemovePlayer(networkUser, "Kicked", client =>
            {
                var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");

                NetworkManagerSystem.singleton.ServerKickClient(client, reason);
            });
        }

        public static void Ban(NetworkUser networkUser)
        {
            RemovePlayer(networkUser, "Banned", NetworkManagerSystem.singleton.ServerBanClient);
        }
    }
}
