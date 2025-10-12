using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneUI : MonoBehaviour
{
    [SerializeField] private List<Button> skinButtons; // gán 4 nút
    [SerializeField] private List<Image> skinLockOverlays; // overlay lock icon (tùy UI)
    [SerializeField] private Button Ready;
    [SerializeField] private Button CancelReady;

    private void Awake()
    {
        Ready.onClick.AddListener(() =>
        {
            LobbyGameFlow.Instance.SetReady(true);
        });
        CancelReady.onClick.AddListener(() =>
        {
            LobbyGameFlow.Instance.SetReady(false);
        });

        for (int i = 0; i < skinButtons.Count; i++)
        {
            int index = i; // tránh capture bug
            skinButtons[i].onClick.AddListener(() =>
            {
                LobbyManager.Instance.SetSelectedSkin(index);
            });
        }
    }
    private void Update()
    {
        if (LobbyManager.Instance.hostLobby == null) return;
        LobbyGameFlow.Instance.TryCheckAllReady();
        RefreshSkinButtons();
    }
    private void RefreshSkinButtons()
    {
        if (LobbyManager.Instance.joinLobby == null) return;

        HashSet<string> claimed = new HashSet<string>();
        foreach (var p in LobbyManager.Instance.joinLobby.Players)
        {
            if (p.Data != null && p.Data.TryGetValue("Skin", out var sd) && !string.IsNullOrEmpty(sd.Value))
                claimed.Add(sd.Value);
        }

        string myId = LobbyManager.Instance.GetPlayerId(); // ← giờ dùng cái này
        string mySkin = LobbyGameFlow.Instance.GetPlayerSkin(myId);

        for (int i = 0; i < skinButtons.Count; i++)
        {
           skinButtons[i].interactable = true;
            if (i < skinLockOverlays.Count)
                skinLockOverlays[i].gameObject.SetActive(false);
        }
    }
    // [SerializeField] Button Skin1;
    // [SerializeField] Button Skin2;
    // [SerializeField] Button Skin3;
    // [SerializeField] Button Skin4;
    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // private void Awake()
    // {
    //     Skin1.onClick.AddListener(() =>
    //     {
    //         LobbyGameFlow.Instance.SelectSkin(1);
    //     });
    //     Skin2.onClick.AddListener(() =>
    //     {
    //         LobbyGameFlow.Instance.SelectSkin(2);
    //     });
    //     Skin3.onClick.AddListener(() =>
    //     {
    //         LobbyGameFlow.Instance.SelectSkin(3);
    //     });

    //     Skin4.onClick.AddListener(() =>
    //     {
    //         LobbyGameFlow.Instance.SelectSkin(4);
    //     });
    // }
}
