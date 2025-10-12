using UnityEngine;

public class PlayerSetupPreview : MonoBehaviour
{
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer turretRenderer;
    [SerializeField] private TankSkinDatabase skinDatabase;

    public void ApplySkin(int index)
    {
        var skin = skinDatabase.GetSkinByIndex(index);
        if (skin == null) return;

        if (bodyRenderer) bodyRenderer.sprite = skin.bodySprite;
        if (turretRenderer) turretRenderer.sprite = skin.turretSprite;
    }
}
