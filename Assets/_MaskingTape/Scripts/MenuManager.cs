using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonPrefab;

    [SerializeField]
    private GameObject mainMenuContainer;

    [SerializeField]
    private Button startPaintingBtn;

    [SerializeField]
    private Button stopPaintingBtn;

    void Start()
    {
        CreateMainMenu();
        startPaintingBtn.onClick.AddListener(delegate { GameManager.instance.SetGameState(GameState.PAINTING); });
        stopPaintingBtn.onClick.AddListener(delegate { GameManager.instance.SetGameState(GameState.RESULTS); });

        ShowHideButtons(GameManager.instance.GetState());
        GameManager.instance.OnGameStateChanged += ShowHideButtons;
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

    private void ShowHideButtons(GameState _state)
    {
        mainMenuContainer.SetActive(_state == GameState.MENU);
        startPaintingBtn.gameObject.SetActive(_state == GameState.TAPING);
        stopPaintingBtn.gameObject.SetActive(_state == GameState.PAINTING);
    }
}
