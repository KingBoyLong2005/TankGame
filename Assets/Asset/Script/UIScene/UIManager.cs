using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private ContinueMenuUI continueMenuPrefab;
    private ContinueMenuUI continueMenuInstance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public void ShowContinueMenu()
    {
        if (continueMenuInstance == null)
        {
            continueMenuInstance = Instantiate(continueMenuPrefab);
        }

        continueMenuInstance.Show();
    }

    public void HideContinueMenu()
    {
        if (continueMenuInstance != null)
        {
            continueMenuInstance.Hide();
        }
    }
}
