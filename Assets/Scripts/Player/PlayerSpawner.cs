using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [Header("Fusion (recommended)")]
    public NetworkPrefabRef PlayerPrefab;   // <-- register this in NetworkProjectConfig Prefab Table

    [Header("Spawn")]
    public Vector2 SpawnXY = Vector2.zero;
    public float SpawnZ = 0f;

    [Header("Debug")]
    public bool VerboseLogs = true;
    public bool ForceCameraZMinus10 = true;

    public PlayerInputManager PlayerInputManager;

    public void PlayerJoined(PlayerRef player)
    {
        // Safety checks / loud logging
        if (Runner == null)
        {
            Debug.LogError("[PlayerSpawner] Runner is NULL. This behaviour isn't attached to the active Runner instance.");
            return;
        }

        if (VerboseLogs)
        {
            Debug.Log(
                $"[PlayerSpawner] PlayerJoined fired. " +
                $"joinedPlayer={player} localPlayer={Runner.LocalPlayer} " +
                $"mode={Runner.GameMode} isRunning={Runner.IsRunning}"
            );
        }

        // Only spawn our own local avatar (common pattern)
        if (player != Runner.LocalPlayer)
            return;

        // In Host/Server modes, only the server/host should spawn.
        // In Shared mode, any client can call Spawn() (Fusion forwards request to Shared Server).
        bool isShared = Runner.GameMode == GameMode.Shared;
        bool canSpawnHere = isShared || Runner.IsServer;

        if (!canSpawnHere)
        {
            Debug.LogWarning($"[PlayerSpawner] Not allowed to spawn here. mode={Runner.GameMode} IsServer={Runner.IsServer}");
            return;
        }

        // Spawn position (2D but using Vector3)
        Vector3 pos = new Vector3(SpawnXY.x, SpawnXY.y, SpawnZ);

        // IMPORTANT: in Shared mode input authority param is "not relevant" per Photon docs,
        // but in other modes it matters for input ownership patterns.
        // Passing 'player' is harmless in Shared and helpful elsewhere.
        NetworkObject spawned = Runner.Spawn(PlayerPrefab, pos, Quaternion.identity, player);
        PlayerInputManager.localPlayer =spawned.transform;
        if (spawned == null)
        {
            Debug.LogError("[PlayerSpawner] Runner.Spawn returned NULL. Check Prefab Table registration and NetworkObject on prefab root.");
            return;
        }

        if (VerboseLogs)
        {
            Debug.Log($"[PlayerSpawner] Spawned NetworkObject name={spawned.name} id={spawned.Id} pos={spawned.transform.position}");
        }

        // Ensure camera can actually see 2D sprites
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PlayerSpawner] Camera.main is NULL (no tagged MainCamera).");
            return;
        }

        if (ForceCameraZMinus10)
        {
            var cp = cam.transform.position;
            if (cp.z > -0.01f) // camera too close / same plane
            {
                cam.transform.position = new Vector3(cp.x, cp.y, -10f);
                if (VerboseLogs) Debug.Log("[PlayerSpawner] Forced MainCamera Z to -10 for 2D visibility.");
            }
        }

        if (cam.TryGetComponent(out CameraFollow follow))
        {
            follow.target = spawned.transform;
            if (VerboseLogs) Debug.Log("[PlayerSpawner] CameraFollow target set.");
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] No CameraFollow found on MainCamera.");
        }
        GameplayCanvasUI.Instance.movementJoystickController.gameObject.SetActive(true);
    }
}