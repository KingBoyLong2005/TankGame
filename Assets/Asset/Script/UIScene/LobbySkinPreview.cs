using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;


public class LobbySkinPreview : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonNext;
    // [SerializeField] private TMP_Text skinNameText;
    [SerializeField] private Transform previewPoint;

    [Header("Skin Settings")]
    [SerializeField] private GameObject previewPrefab; // prefab player đơn giản
    [SerializeField] private TankSkinDatabase skinDatabase;

    private GameObject currentPreview;
    private int currentSkinIndex = 0;
    private void Start()
    {
        if (skinDatabase == null || skinDatabase.skins.Count == 0)
        {
            Debug.LogError("Không tìm thấy database skin!");
            return;
        }

        buttonPrev.onClick.AddListener(PrevSkin);
        buttonNext.onClick.AddListener(NextSkin);

        ShowSkin(currentSkinIndex);
    }

    private void ShowSkin(int index)
    {
        // try
        // {
        //     var update = new UpdatePlayerOptions
        //     {
        //         Data = new Dictionary<string, PlayerDataObject>
        //         {
        //             { "Skin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, currentSkinIndex.ToString()) },
        //             { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") }
        //         }
        //     };
        // Xóa preview cũ nếu có
        // if (currentPreview != null)
        //     Destroy(currentPreview);

        // Spawn nhân vật preview
        int playerIndex = (int)OwnerClientId;

        Vector3 spawnPosition = Vector3.zero;
        spawnPosition = SpawnPointManager.Instance.GetSpawnPosition(playerIndex);
        currentPreview = Instantiate(previewPrefab, spawnPosition, Quaternion.identity);
        currentPreview.transform.SetParent(previewPoint, false);
        // currentPreview.transform.localScale = Vector3.one;

        var setup = currentPreview.GetComponent<PlayerSetupPreview>();
        if (setup != null)
        {
            setup.ApplySkin(index);
        }
        else
        {
            Debug.LogError("❌ Prefab preview không có PlayerSetupPreview!");
        }

        // Hiển thị tên skin
        // skinNameText.text = skin.displayName;

        // Lưu lại lựa chọn
        LobbyManager.Instance.SetSelectedSkin(index);
        currentSkinIndex = index;
        // }
        // catch (Exception e) { Debug.LogError("SelectSkin error: " + e); }
    }
    private async void ChangeSkin(int index)
    {
        try
        {

            var setup = currentPreview.GetComponent<PlayerSetupPreview>();
            if (setup != null)
            {
                    setup.ApplySkin(index);
            }
            else
            {
                Debug.LogError("❌ Prefab preview không có PlayerSetupPreview!");
            }

            LobbyManager.Instance.SetSelectedSkin(index);
            FindFirstObjectByType<PlayerSetup>().SetSkinServerRpc(index);
            var update = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "Skin", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, index.ToString()) },
                    { "IsReady", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "false") }
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(
                LobbyManager.Instance.joinLobby.Id,
                AuthenticationService.Instance.PlayerId,
                update
            );
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không thể cập nhật skin lên LobbyService: " + e.Message);
        }
    }

    private void NextSkin()
    {
        currentSkinIndex++;
        if (currentSkinIndex >= skinDatabase.skins.Count)
            currentSkinIndex = 0;
        ChangeSkin(currentSkinIndex);
    }
    private void PrevSkin()
    {
        currentSkinIndex--;
        if (currentSkinIndex < 0)
            currentSkinIndex = skinDatabase.skins.Count - 1;
        ChangeSkin(currentSkinIndex);
    }
    public int GetSkinIndex() => currentSkinIndex;
}
