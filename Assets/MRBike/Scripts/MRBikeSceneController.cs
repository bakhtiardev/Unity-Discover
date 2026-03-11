// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

namespace MRBike
{
    /// <summary>
    /// Manages the standalone MRBike virtual environment scene.
    /// Starts a Photon Fusion session in Single Player mode so all
    /// NetworkBehaviour components (BikeVisibleObject, VONetworkManager,
    /// NetworkTaskTracker) operate locally without a network connection.
    /// This makes the scene fully testable in the Meta XR Simulator.
    /// </summary>
    public class MRBikeSceneController : MonoBehaviour, INetworkRunnerCallbacks
    {
        private async void Start()
        {
            var runnerGO = new GameObject("NetworkRunner");
            DontDestroyOnLoad(runnerGO);
            var runner = runnerGO.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
            runner.AddCallbacks(this);

            var args = new StartGameArgs
            {
                GameMode = GameMode.Single,
                IsVisible = false,
            };

            var result = await runner.StartGame(args);

            if (!result.Ok)
            {
                Debug.LogError(
                    $"[MRBike] Failed to start session: {result.ShutdownReason}. " +
                    "Ensure the Photon Fusion SDK is properly configured and the " +
                    "App ID is set in PhotonAppSettings (Assets/Photon/Resources/PhotonAppSettings).");
            }
            else
            {
                Debug.Log("[MRBike] Single Player session started — BikeInteraction ready.");
            }
        }

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner) { }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }

        public void OnSceneLoadDone(NetworkRunner runner) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
}
