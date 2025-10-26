using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class BotSetup : NetworkBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer turretRenderer;
    [SerializeField] private TankSkinDatabase skinDatabase;

    public NetworkVariable<FixedString64Bytes> botName =
        new NetworkVariable<FixedString64Bytes>(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> skinIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Vector3 spawnPos = SpawnPointManager.Instance.GetNextSpawnPosition();
            transform.position = spawnPos;
        }

        ApplySkin(skinIndex.Value);
        skinIndex.OnValueChanged += (oldVal, newVal) => ApplySkin(newVal);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitBotServerRpc(string name, int skin)
    {
        botName.Value = new FixedString64Bytes(name);
        skinIndex.Value = skin;
    }

    private void ApplySkin(int idx)
    {
        var skin = skinDatabase.GetSkinByIndex(idx);
        if (skin == null) return;

        if (bodyRenderer != null) bodyRenderer.sprite = skin.bodySprite;
        if (turretRenderer != null) turretRenderer.sprite = skin.turretSprite;
    }

    public string GetBotName() => botName.Value.ToString();
}
