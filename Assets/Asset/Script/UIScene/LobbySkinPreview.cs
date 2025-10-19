using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
    [SerializeField] private Transform previewPoint;

    [Header("Skin Settings")]
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

        buttonPrev?.onClick.AddListener(PrevSkin);
        buttonNext?.onClick.AddListener(NextSkin);

        ShowSkin(currentSkinIndex);
    }

    
    private void ShowSkin(int index)
    {
        if (currentPreview != null)
            Destroy(currentPreview);

        var skin = skinDatabase.GetSkinByIndex(index);
        if (skin == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy skin index {index}");
            return;
        }

        currentPreview = Instantiate(skin.previewPrefab, previewPoint);
        var setup = currentPreview.GetComponent<PlayerSetupPreview>();
        setup?.ApplySkin(index);

        LobbyManager.Instance?.SetSelectedSkin(index);
        currentSkinIndex = index;
    }

    private async void ChangeSkin(int index)
    {
        ShowSkin(index);

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

            if (LobbyManager.Instance?.joinLobby != null)
            {
                await LobbyService.Instance.UpdatePlayerAsync(
                    LobbyManager.Instance.joinLobby.Id,
                    AuthenticationService.Instance.PlayerId,
                    update
                );
            }

            FindFirstObjectByType<PlayerSetup>()?.SetSkinServerRpc(index);
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
