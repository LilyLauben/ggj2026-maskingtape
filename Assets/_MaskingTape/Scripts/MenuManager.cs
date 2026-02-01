using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonPrefab;

    [SerializeField]
    private GameObject mainMenuContainer;

    void Start()
    {
        CreateMainMenu();
    }

    private void CreateMainMenu()
    {
        string[] gameModeNames = GameManager.instance.GetGameModeNames();
        foreach (string mode in gameModeNames)
        {
            GameObject modeButton = Instantiate(buttonPrefab, mainMenuContainer.transform);
            modeButton.GetComponentInChildren<TextMeshProUGUI>().text = mode;
            modeButton.GetComponent<Button>().onClick.AddListener(delegate { GameManager.instance.StartGame(mode); });
        }
    }
}
