using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class PlayerName : MonoBehaviour
{
    public Transform target; // nhân vật
    public Vector3 offset = new Vector3(0, 1f, 0);
    [SerializeField] private TMP_Text PlayerNameFloating;

    private PlayerSetup playerSetup;

    private void Start()
    {
        playerSetup = target.GetComponent<PlayerSetup>();
        if (playerSetup != null)
        {
            PlayerNameFloating.text = playerSetup.GetPlayerName();
            playerSetup.playerName.OnValueChanged += OnNameChanged;
        }
    }

    private void OnDestroy()
    {
        if (playerSetup != null)
        {
            playerSetup.playerName.OnValueChanged -= OnNameChanged;
        }
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        PlayerNameFloating.text = newValue.ToString();
    }
    private void Update()
    {
        if (target == null) return;

        // đặt text ở vị trí target + offset
        transform.position = target.position + offset;

        // giữ text không xoay theo target
        transform.rotation = Quaternion.identity;
    }
}
