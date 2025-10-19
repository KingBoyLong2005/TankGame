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
        if (previewObj != null) Destroy(previewObj);

        var skin = skinDatabase.GetSkinByIndex(index);
        if (skin == null)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy skin index {index}");
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

        LobbyManager.Instance?.SetSelectedSkin(index);
        FindFirstObjectByType<PlayerSetup>()?.SetSkinServerRpc(index);

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
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Không thể cập nhật skin lên LobbyService: " + e.Message);
        }
    }

    private void NextSkin()
    {
        skinIndex++;
        if (skinIndex >= skinDatabase.skins.Count) skinIndex = 0;
        ChangeSkin(skinIndex);
    }

    private void PrevSkin()
    {
        skinIndex--;
        if (skinIndex < 0) skinIndex = skinDatabase.skins.Count - 1;
        ChangeSkin(skinIndex);
    }
}
