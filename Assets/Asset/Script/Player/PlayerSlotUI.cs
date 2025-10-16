using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Button buttonPrev;
    [SerializeField] private Button buttonNext;
    [SerializeField] private Transform previewPoint;

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

        ShowSkin();
    }

    private void ShowSkin()
    {
        if (previewObj != null) Destroy(previewObj);
        var db = FindFirstObjectByType<TankSkinDatabase>();
        var skinPrefab = db.skins[skinIndex].previewPrefab;
        previewObj = Instantiate(skinPrefab, previewPoint);
    }

    private void NextSkin()
    {
        skinIndex++;
        var db = FindFirstObjectByType<TankSkinDatabase>();
        if (skinIndex >= db.skins.Count) skinIndex = 0;
        ShowSkin();

        if (isLocal)
            LobbyManager.Instance.SetSelectedSkin(skinIndex);
    }

    private void PrevSkin()
    {
        skinIndex--;
        var db = FindFirstObjectByType<TankSkinDatabase>();
        if (skinIndex < 0) skinIndex = db.skins.Count - 1;
        ShowSkin();

        if (isLocal)
            LobbyManager.Instance.SetSelectedSkin(skinIndex);
    }
}
