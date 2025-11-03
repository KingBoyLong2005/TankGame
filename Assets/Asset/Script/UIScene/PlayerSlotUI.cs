using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using Unity.Netcode;
using System.Collections.Generic;

public class PlayerSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonNext;
    [SerializeField] private Transform previewPoint;

    [Header("Skin Settings")]
    [SerializeField] private TankSkinDatabase skinDatabase;

    private GameObject previewObj;
    private int skinIndex;
    private bool isLocal;

    public void Setup(string playerName, bool isLocalPlayer, int initialSkin)
    {
        playerNameText.text = playerName;
        isLocal = isLocalPlayer;
        skinIndex = initialSkin;

        // Chỉ hiển thị nút prev/next cho local player
        buttonPrev.gameObject.SetActive(isLocal);
        buttonNext.gameObject.SetActive(isLocal);

        if (isLocal)
        {
            buttonPrev.onClick.AddListener(PrevSkin);
            buttonNext.onClick.AddListener(NextSkin);
        }

        ShowSkin(skinIndex);
    }

    private void ShowSkin(int index)
    {
        if (previewObj != null) 
            Destroy(previewObj);

        if (skinDatabase == null)
        {
            Debug.LogError("❌ TankSkinDatabase chưa được gán trong PlayerSlotUI!");
            return;
        }

        var skin = skinDatabase.GetSkinByIndex(index);
        if (skin == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy skin index {index}");
            return;
        }

        if (skin.previewPrefab == null)
        {
            Debug.LogWarning($"⚠️ Skin {skin.displayName} không có preview prefab!");
            return;
        }

        previewObj = Instantiate(skin.previewPrefab, previewPoint);
        var setup = previewObj.GetComponent<PlayerSetupPreview>();
        setup?.ApplySkin(index);
    }

    private async void ChangeSkin(int index)
    {
        ShowSkin(index);
        skinIndex = index;

        // ✅ 1. Cập nhật LobbyManager (local)
        LobbyManager.Instance?.SetSelectedSkin(index);

        // ✅ 2. Cập nhật qua Netcode (đồng bộ real-time)
        if (LobbyPlayersManager.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            LobbyPlayersManager.Instance.UpdatePlayerSkinServerRpc(index);
            Debug.Log($"[PlayerSlotUI] Sent skin update to server: {index}");
        }

        // ✅ 3. Cập nhật Lobby Service (để persist data)
        try
        {
            var update = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Skin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, index.ToString()) },
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") }
                }
            };

            if (LobbyManager.Instance?.joinLobby != null && AuthenticationService.Instance.IsSignedIn)
            {
                await LobbyService.Instance.UpdatePlayerAsync(
                    LobbyManager.Instance.joinLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    update
                );
                Debug.Log($"[PlayerSlotUI] Updated skin in Lobby Service: {index}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không thể cập nhật skin lên LobbyService: " + e.Message);
        }

        // ✅ 4. Cập nhật PlayerSetup (nếu đã spawn)
        var playerSetup = FindFirstObjectByType<PlayerSetup>();
        if (playerSetup != null && playerSetup.IsOwner)
        {
            playerSetup.SetSkinServerRpc(index);
        }
    }

    private void NextSkin()
    {
        if (skinDatabase == null) return;

        skinIndex++;
        if (skinIndex >= skinDatabase.skins.Count) 
            skinIndex = 0;
        
        ChangeSkin(skinIndex);
    }

    private void PrevSkin()
    {
        if (skinDatabase == null) return;

        skinIndex--;
        if (skinIndex < 0) 
            skinIndex = skinDatabase.skins.Count - 1;
        
        ChangeSkin(skinIndex);
    }

    private void OnDestroy()
    {
        if (isLocal)
        {
            buttonPrev.onClick.RemoveListener(PrevSkin);
            buttonNext.onClick.RemoveListener(NextSkin);
        }
    }
}