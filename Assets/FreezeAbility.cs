using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class FreezeAbility : NetworkBehaviour
{
    [Header("Settings")]
    public float detectionRange = 2.5f;

    [Header("Visuals")]
    public Sprite frozenSprite;

    public NetworkVariable<bool> isFrozen = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private SpriteRenderer sr;
    private Sprite originalSprite;
    private NetworkPlayerMovement movement;

    private GameObject iceButtonsPanel;
    private GameObject waterButtonsPanel;
    private Button iceButton;
    private Button waterButton;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent<NetworkPlayerMovement>();
    }

    public override void OnNetworkSpawn()
    {
        isFrozen.OnValueChanged += OnFreezeChanged;

        if (IsOwner)
        {
            // Debug
            Debug.Log("FreezeAbility: IsOwner = true");
            Debug.Log("Role = " + (AppSession.Instance != null ? AppSession.Instance.role.ToString() : "NULL"));

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Debug.Log("Canvases found: " + canvases.Length);

            foreach (var canvas in canvases)
            {
                Debug.Log("Canvas: " + canvas.name);
                Transform ice = canvas.transform.Find("IceButtons");
                Transform water = canvas.transform.Find("WaterButtons");
                if (ice != null) { iceButtonsPanel = ice.gameObject; Debug.Log("IceButtons found!"); }
                if (water != null) { waterButtonsPanel = water.gameObject; Debug.Log("WaterButtons found!"); }
            }

            if (iceButtonsPanel != null)
                iceButton = iceButtonsPanel.GetComponentInChildren<Button>(true);

            if (waterButtonsPanel != null)
                waterButton = waterButtonsPanel.GetComponentInChildren<Button>(true);

            int role = AppSession.Instance != null ? (int)AppSession.Instance.role : 1;
            Debug.Log("role int = " + role);

            if (iceButtonsPanel != null) iceButtonsPanel.SetActive(role == 2);
            if (waterButtonsPanel != null) waterButtonsPanel.SetActive(role == 1);

            if (iceButton != null)
                iceButton.onClick.AddListener(OnIceButtonPressed);
            if (waterButton != null)
                waterButton.onClick.AddListener(OnWaterButtonPressed);
        }
    }


    private void Update()
    {
        if (!IsOwner) return;

        int role = AppSession.Instance != null ? (int)AppSession.Instance.role : 1;

        if (role == 2 && iceButton != null)
            iceButton.interactable = FindNearestWaterPlayer(frozen: false) != null;

        if (role == 1 && waterButton != null)
            waterButton.interactable = FindNearestWaterPlayer(frozen: true) != null;
    }

    void OnIceButtonPressed()
    {
        FreezeAbility target = FindNearestWaterPlayer(frozen: false);
        if (target != null)
            FreezePlayerServerRpc(target.NetworkObjectId, true);
    }

    void OnWaterButtonPressed()
    {
        FreezeAbility target = FindNearestWaterPlayer(frozen: true);
        if (target != null)
            FreezePlayerServerRpc(target.NetworkObjectId, false);
    }

    [ServerRpc]
    void FreezePlayerServerRpc(ulong targetNetworkId, bool freeze)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkId, out var netObj))
        {
            FreezeAbility target = netObj.GetComponent<FreezeAbility>();
            if (target != null)
                target.isFrozen.Value = freeze;
        }
    }

    void OnFreezeChanged(bool oldValue, bool newValue)
    {
        if (movement != null)
            movement.SetFrozen(newValue);

        if (sr != null)
        {
            if (newValue && frozenSprite != null)
            {
                if (originalSprite == null) originalSprite = sr.sprite;
                sr.sprite = frozenSprite;
            }
            else if (!newValue && originalSprite != null)
            {
                sr.sprite = originalSprite;
            }
            else if (newValue)
            {
                sr.color = new Color(0.5f, 0.8f, 1f);
            }
            else
            {
                sr.color = Color.white;
            }
        }
    }

    FreezeAbility FindNearestWaterPlayer(bool frozen)
    {
        FreezeAbility[] allPlayers = FindObjectsByType<FreezeAbility>(FindObjectsSortMode.None);
        FreezeAbility nearest = null;
        float minDist = detectionRange;

        foreach (var player in allPlayers)
        {
            if (player == this) continue;

            NetworkPlayerVisual vis = player.GetComponent<NetworkPlayerVisual>();
            if (vis == null || vis.roleIndex.Value != 1) continue;

            if (player.isFrozen.Value != frozen) continue;

            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = player;
            }
        }

        return nearest;
    }

    public override void OnNetworkDespawn()
    {
        isFrozen.OnValueChanged -= OnFreezeChanged;
    }
}
