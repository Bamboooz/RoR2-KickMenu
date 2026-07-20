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

        public static bool IsHost(NetworkUser networkUser)
        {
            if (networkUser == null)
            {
                return false;
            }

            return networkUser.isLocalPlayer;
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

        private static void SendRemoveMessage(string username, string message)
        {
            if (ModConfig.BroadcastKick.Value)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                {
                    baseToken = $"<color=#e57373>{message} player:</color> <color=#ffffff><noparse>{username}</noparse></color>"
                });
            }
        }

        public static void RemovePlayer(NetworkUser networkUser, string message, Action<NetworkConnection> action)
        {
            if (networkUser == null)
            {
                Log.LogWarning("No connection found for player: <null NetworkUser>");

                return;
            }

            string userName = networkUser.userName;

            NetworkConnection client = GetNetworkConnection(networkUser);

            if (client == null)
            {
                Log.LogWarning($"No connection found for player: {userName}");

                return;
            }

            int connectionId = client.connectionId;

            if (!client.isReady)
            {
                Log.LogWarning($"Client connection for {userName} is not ready (Connection ID: {connectionId}). Still attempting removal.");
            }

            try
            {
                Log.LogInfo($"Removing player {userName} (Connection ID: {connectionId})...");

                action?.Invoke(client);

                try
                {
                    client.Disconnect();
                }
                catch (Exception disconnectException)
                {
                    Log.LogDebug($"client.Disconnect() fallback for {userName} threw (connection was likely already closed): {disconnectException.Message}");
                }

                Log.LogInfo($"{message} player {userName} (Connection ID: {connectionId})");

                SendRemoveMessage(userName, message);
            }
            catch (Exception e)
            {
                Log.LogError($"Failed to remove player {userName} (Connection ID: {connectionId}): {e}");
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
            RemovePlayer(networkUser, "Banned", client =>
            {
                NetworkManagerSystem.singleton.ServerBanClient(client);

                var reason = new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_KICK");

                NetworkManagerSystem.singleton.ServerKickClient(client, reason);
            });
        }
    }
}
