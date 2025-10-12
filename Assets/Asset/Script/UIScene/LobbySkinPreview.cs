using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LobbySkinPreview : MonoBehaviour
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
        try
        {
            // Xóa preview cũ nếu có
            if (currentPreview != null)
                Destroy(currentPreview);

            // Spawn nhân vật preview
            currentPreview = Instantiate(previewPrefab, previewPoint.position, Quaternion.identity);
            currentPreview.transform.SetParent(previewPoint, false);
            currentPreview.transform.localScale = Vector3.one;

            // Lấy skin từ database
            var skin = skinDatabase.GetSkinByIndex(index);
            if (skin == null) return;

            // Áp dụng sprite cho preview (dùng PlayerSetup hoặc script tạm)
            // Lấy skin từ database
            var setup = currentPreview.GetComponent<PlayerSetupPreview>();
            if (setup != null)
            {
                setup.ApplySkin(index);
            }

            // Hiển thị tên skin
            // skinNameText.text = skin.displayName;

            // Lưu lại lựa chọn
            LobbyManager.Instance.SetSelectedSkin(index);
        }
        catch (Exception e) { Debug.LogError("SelectSkin error: " + e); }
    }

    private void NextSkin()
    {
        currentSkinIndex++;
        if (currentSkinIndex >= skinDatabase.skins.Count)
            currentSkinIndex = 0;
        ShowSkin(currentSkinIndex);
    }

    private void PrevSkin()
    {
        currentSkinIndex--;
        if (currentSkinIndex < 0)
            currentSkinIndex = skinDatabase.skins.Count - 1;
        ShowSkin(currentSkinIndex);
    }
}
