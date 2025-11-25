using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MobileInput : MonoBehaviour
{
    public Joystick joystick; // Tham chiếu đến joystick UI (VD: Floating Joystick)
    public PlayerControllerTest player; // Tham chiếu PlayerController cần điều khiển
    public PlayerShootingTest playerShooting;
    public Button fireBtn;
    public bool isMobile = false; // Dễ test trong editor

    private void Start()
    {
    #if UNITY_ANDROID || UNITY_IOS
        isMobile = true;
        #else
                isMobile = false;
        #endif
        fireBtn.onClick.AddListener(() => playerShooting.ShootTrue());
    }

    private void Update()
    {
        if (!isMobile || joystick == null || player == null) return;

        // Lấy đầu vào từ joystick
        Vector2 moveInput = new Vector2(joystick.Horizontal, joystick.Vertical);

        // Truyền thẳng vào player (bỏ qua InputAction)
        player.SetMobileInput(moveInput);
    }
}
