using UnityEngine;

[System.Serializable]
public class TankSkin
{
    [Header("Basic Info")]
    public string id;                     // Mã định danh (ví dụ: "skin_default", "skin_red_tiger")
    public string displayName;            // Tên hiển thị trong UI

    [Header("Sprites")]
    public Sprite bodySprite;             // Sprite phần thân
    public Sprite turretSprite;  
    
    [Header("Visuals")]
    public Sprite icon;                   // Icon hiển thị trong nút chọn skin
    public GameObject previewPrefab;      // Prefab dùng cho hiển thị preview (trong lobby)
    public GameObject inGamePrefab;       // Prefab dùng khi spawn vào trận thực tế

    [Header("Gameplay/Meta")]
    public int unlockLevel;               // Mở khóa ở level mấy (nếu có progression)
    public bool isDefault;                // Skin mặc định
}
