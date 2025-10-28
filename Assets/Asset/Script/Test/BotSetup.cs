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
        // ✅ Apply skin ngay khi spawn
        ApplySkin(skinIndex.Value);
        
        // Lắng nghe thay đổi skin
        skinIndex.OnValueChanged += (oldVal, newVal) => ApplySkin(newVal);
        
        Debug.Log($"🤖 Bot spawned: {botName.Value} with skin {skinIndex.Value}");
    }

    // ✅ Hàm public để server gọi trực tiếp (không qua RPC)
    public void InitBot(string name, int skin)
    {
        if (!IsServer)
        {
            Debug.LogWarning("⚠️ InitBot chỉ được gọi trên server!");
            return;
        }

        botName.Value = new FixedString64Bytes(name);
        skinIndex.Value = skin;

        // Apply skin ngay lập tức
        ApplySkin(skin);
        
        Debug.Log($"✅ Bot initialized: {name} with skin {skin}");
    }

    // ✅ Giữ ServerRpc cho trường hợp cần thiết
    [ServerRpc(RequireOwnership = false)]
    public void InitBotServerRpc(string name, int skin)
    {
        InitBot(name, skin);
    }

    private void ApplySkin(int idx)
    {
        if (skinDatabase == null)
        {
            Debug.LogError("❌ Skin database chưa được gán!");
            return;
        }

        var skin = skinDatabase.GetSkinByIndex(idx);
        if (skin == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy skin index {idx}");
            return;
        }

        if (bodyRenderer != null) 
        {
            bodyRenderer.sprite = skin.bodySprite;
            Debug.Log($"✅ Applied body sprite for skin {idx}");
        }
        else
        {
            Debug.LogWarning("⚠️ Body renderer chưa được gán!");
        }

        if (turretRenderer != null) 
        {
            turretRenderer.sprite = skin.turretSprite;
            Debug.Log($"✅ Applied turret sprite for skin {idx}");
        }
        else
        {
            Debug.LogWarning("⚠️ Turret renderer chưa được gán!");
        }
    }

    public string GetBotName() => botName.Value.ToString();
    public int GetSkinIndex() => skinIndex.Value;
}