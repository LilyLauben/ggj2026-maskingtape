using System;
using UnityEngine;

public enum GameState { MENU, TAPING, PAINTING, RESULTS };

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
    private (string name, Texture2D goalTexture)[] games;

    private GameState state = GameState.MENU;

    private Texture2D currentGoal;

    public event EventHandler<GameState> OnGameStateChanged;

    public void StartGame(string _name)
    {
        currentGoal = Array.Find(games, game => game.name == _name).goalTexture;
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

}
