using UnityEngine;
using UnityEngine.UI;

public class ContinueMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinueClicked);
        exitButton.onClick.AddListener(OnExitClicked);
        panel.SetActive(false);
    }

    public void Show()
    {
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void OnContinueClicked()
    {
        Hide();
        GameManager.Instance.PlayerVoteServerRpc(true);
    }

    private void OnExitClicked()
    {
        Hide();
        GameManager.Instance.PlayerVoteServerRpc(false);
    }
}
