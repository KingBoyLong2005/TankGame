using TMPro;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class BotName : MonoBehaviour
{
    [Header("Target Bot")]
    public Transform target; // Bot mà text sẽ bám theo
    public Vector3 offset = new Vector3(0, 1f, 0); // Độ cao hiển thị chữ

    [SerializeField] private TMP_Text botNameFloating;

    private BotSetup botSetup;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"[{name}] ⚠️ Chưa gán target cho BotName, thử tìm tự động...");
            var bot = GetComponentInParent<BotSetup>();
            if (bot != null)
                target = bot.transform;
        }

        if (target == null)
        {
            Debug.LogError($"[{name}] ❌ Không thể tìm thấy target bot để hiển thị tên!");
            enabled = false;
            return;
        }

        botSetup = target.GetComponent<BotSetup>();
        if (botSetup != null)
        {
            // Gán tên ban đầu
            botNameFloating.text = botSetup.GetBotName();

            // Nếu BotSetup có NetworkVariable botName, đăng ký lắng nghe
            botSetup.botName.OnValueChanged += OnNameChanged;
        }
        else
        {
            Debug.LogWarning($"[{name}] ⚠️ BotSetup không có trên {target.name}, dùng tên mặc định.");
            botNameFloating.text = "Bot";
        }
    }

    private void OnDestroy()
    {
        if (botSetup != null)
        {
            botSetup.botName.OnValueChanged -= OnNameChanged;
        }
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        botNameFloating.text = newValue.ToString();
    }

    private void Update()
    {
        if (target == null) return;

        // Giữ text trên đầu bot
        transform.position = target.position + offset;

        // Luôn hướng về camera (nếu bạn muốn)
        transform.rotation = Quaternion.identity;
    }
}
