using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerVisual : NetworkBehaviour
{
    [Header("Visual Objects")]
    public GameObject waterVisual;
    public GameObject iceVisual;

    public NetworkVariable<int> roleIndex = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isFrozenVisual = new NetworkVariable<bool>(false);

    private Camera playerCamera;
    private AudioListener playerAudioListener;

    public override void OnNetworkSpawn()
    {
        Debug.Log("Player Spawned");
        Debug.Log("IsOwner = " + IsOwner);

        Camera cam = GetComponentInChildren<Camera>(true);

        if (cam == null)
        {
            Debug.LogError("CAMERA NOT FOUND");
        }
        else
        {
            Debug.Log("CAMERA FOUND");
        }
        Debug.Log($"[PlayerVisual] Spawned! IsOwner: {IsOwner}, Role: {roleIndex.Value}");

        SetupCameraForLocalPlayer();

        roleIndex.OnValueChanged += OnRoleChanged;
        isFrozenVisual.OnValueChanged += OnFrozenChanged;

        UpdateVisuals(roleIndex.Value, isFrozenVisual.Value);

        if (IsOwner)
        {
            DetermineRole();
        }
    }

    private void SetupCameraForLocalPlayer()
    {
        playerCamera = GetComponentInChildren<Camera>(true);
        playerAudioListener = GetComponentInChildren<AudioListener>(true);

        bool isLocalPlayer = IsOwner;

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(isLocalPlayer);
            playerCamera.enabled = isLocalPlayer;

            if (isLocalPlayer)
            {
                playerCamera.tag = "MainCamera";
                playerCamera.targetDisplay = 0;
                playerCamera.depth = 10;
                Debug.Log("[PlayerVisual] Local player camera enabled.");
            }
            else
            {
                playerCamera.tag = "Untagged";
            }
        }
        else if (isLocalPlayer)
        {
            Debug.LogError("[PlayerVisual] لا توجد Camera داخل Player Prefab!");
        }

        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = isLocalPlayer;
        }
    }

    private void DetermineRole()
    {
        int myRole = 0;

        if (AppSession.Instance != null)
            myRole = (int)AppSession.Instance.role;

        if (myRole == 0)
            myRole = IsServer ? 2 : 1;

        Debug.Log($"[PlayerVisual] Setting Role to: {myRole}");
        SetRoleServerRpc(myRole);
    }

    [ServerRpc]
    public void SetRoleServerRpc(int value)
    {
        roleIndex.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetFrozenVisualServerRpc(bool frozen)
    {
        isFrozenVisual.Value = frozen;
    }

    private void OnRoleChanged(int oldValue, int newValue)
    {
        UpdateVisuals(newValue, isFrozenVisual.Value);
    }

    private void OnFrozenChanged(bool oldValue, bool newValue)
    {
        UpdateVisuals(roleIndex.Value, newValue);
    }

    public void UpdateVisuals(int role, bool frozen)
    {
        if (waterVisual != null)
            waterVisual.SetActive(role == 1);

        if (iceVisual != null)
            iceVisual.SetActive(role == 2);
    }

    public Animator GetActiveAnimator()
    {
        if (roleIndex.Value == 1 && waterVisual != null)
            return waterVisual.GetComponent<Animator>();

        if (roleIndex.Value == 2 && iceVisual != null)
            return iceVisual.GetComponent<Animator>();

        return null;
    }

    public SpriteRenderer GetActiveSpriteRenderer()
    {
        if (roleIndex.Value == 1 && waterVisual != null)
            return waterVisual.GetComponent<SpriteRenderer>();

        if (roleIndex.Value == 2 && iceVisual != null)
            return iceVisual.GetComponent<SpriteRenderer>();

        return null;
    }

    public override void OnNetworkDespawn()
    {
        roleIndex.OnValueChanged -= OnRoleChanged;
        isFrozenVisual.OnValueChanged -= OnFrozenChanged;
    }
}