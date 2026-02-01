using System;
using System.Linq;
using UnityEngine;

public enum GameState { MENU, TAPING, PAINTING, RESULTS };

[Serializable]
public class GameMode { public string name; public Texture2D goalTexture; }

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    [SerializeField]
    private GameMode[] gameModes;

    private GameState state = GameState.MENU;

    private Texture2D currentGoal;

    public event EventHandler<GameState> OnGameStateChanged;

    public void StartGame(string _name)
    {
        currentGoal = Array.Find(gameModes, game => game.name == _name).goalTexture;
        state = GameState.TAPING;
        OnGameStateChanged?.Invoke(this, state);
    }

    public GameState GetState()
    {
        return state;
    }

    public Texture2D GetCurrentGoalTexture()
    {
        return currentGoal;
    }

    public string[] GetGameModeNames()
    {
        return gameModes.Select(gameModes => gameModes.name).ToArray();
    }

}
