// PlayerInputManager.cs
using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PlayerInputManager : SimulationBehaviour, INetworkRunnerCallbacks
{
    // Cached decision so it doesn't fluctuate frame-to-frame
    private bool _useMobileControls;

    [Header("Set this to the local player's Transform (only for local input aim)")]
    public Transform localPlayer;

    private void Start()
    {
        _useMobileControls = ShouldUseMobileControls();
    }

    private void OnEnable()
    {
        if (Runner != null)
            Runner.AddCallbacks(this);
    }

    private void OnDisable()
    {
        if (Runner != null)
            Runner.RemoveCallbacks(this);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        Vector2 move = Vector2.zero;
        Vector2 atk = Vector2.zero;

        if (_useMobileControls)
        {
            // Mobile / touch: use your joystick
            if (GameplayCanvasUI.Instance != null &&
                GameplayCanvasUI.Instance.movementJoystickController != null &&
                GameplayCanvasUI.Instance.atkJoystickController != null)
            {
                move = GameplayCanvasUI.Instance.movementJoystickController.movementAmount;
                atk = GameplayCanvasUI.Instance.atkJoystickController.movementAmount;

                // Optional: normalize move so diagonals aren't faster
                if (move.sqrMagnitude > 1f)
                    move.Normalize();

                // Optional: normalize atk direction if it’s meant to be a direction vector
                if (atk.sqrMagnitude > 1f)
                    atk.Normalize();
            }
        }
        else
        {
            // Desktop: WASD / Arrow keys
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;

            move = new Vector2(x, y);

            // Aim direction from player -> mouse world
            if (Input.GetMouseButton(0) && localPlayer != null && Camera.main != null)
            {
                Vector3 mouseWorld3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 mouseWorld = new Vector2(mouseWorld3.x, mouseWorld3.y);
                Vector2 playerPos = (Vector2)localPlayer.position;
                Vector2 delta = mouseWorld - playerPos;

                atk = (delta.sqrMagnitude > 0.0001f) ? delta.normalized : Vector2.zero;
            }
            else
            {
                atk = Vector2.zero;
            }

            // Normalize so diagonals aren't faster
            if (move.sqrMagnitude > 1f)
                move.Normalize();
        }

        // IMPORTANT: this will assert if Fusion hasn't registered PlayerNetworkInput
        input.Set(new PlayerNetworkInput
        {
            movementInput = move,
            atkInput = atk
        });
    }

    private bool ShouldUseMobileControls()
    {
        // 1) If we're actually on Android/iOS, definitely mobile.
        if (Application.isMobilePlatform)
            return true;

        // 2) WebGL: detect touch-capable device (covers phones/tablets in browsers).
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Input.touchSupported)
            return true;
#endif

        // 3) Default: desktop controls
        if (GameplayCanvasUI.Instance != null)
            GameplayCanvasUI.Instance.gameObject.SetActive(false);

        return false;
    }

    // --- all other INetworkRunnerCallbacks methods (remain empty) ---
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}

/// <summary>
/// Fusion Network Input: must be a public struct implementing INetworkInput.
