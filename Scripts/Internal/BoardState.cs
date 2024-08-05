using System;
using MoonActive.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BoardState
{
    [SerializeField] private PlayerType?[] _board;
    [SerializeField] private PlayerType _currentPlayer;
    [SerializeField] private bool _gameWon;
    [SerializeField] private bool _gameTied;

    public PlayerType?[] Board { get => _board; set => _board = value; }
    public PlayerType CurrentPlayer { get => _currentPlayer; set => _currentPlayer = value; }
    public bool GameWon { get => _gameWon; set => _gameWon = value; }
    public bool GameTied { get => _gameTied; set => _gameTied = value; }

    public string Serialize()
    {
        // Convert PlayerType?[] to string[]
        string[] serializedBoard = _board.Select(tile => tile?.ToString() ?? "").ToArray();

        // Convert PlayerType to string
        string serializedCurrentPlayer = _currentPlayer.ToString();

        // Convert bool to int
        int serializedGameWon = _gameWon ? 1 : 0;
        int serializedGameTied = _gameTied ? 1 : 0;

        var serializedGameState = new SerializableBoardState
        {
            SBoard = serializedBoard,
            SCurrentPlayer = serializedCurrentPlayer,
            SGameWon = serializedGameWon,
            SGameTied = serializedGameTied
        };
        return JsonUtility.ToJson(serializedGameState);
    }

    public void Deserialize(string json)
    {
        var serializedGameState = JsonUtility.FromJson<SerializableBoardState>(json);

        // Convert string[] to PlayerType?[]
        _board = serializedGameState._sBoard.Select(tile => string.IsNullOrEmpty(tile) ? (PlayerType?)null : Enum.Parse<PlayerType>(tile)).ToArray();

        // Convert string to PlayerType
        _currentPlayer = Enum.Parse<PlayerType>(serializedGameState._sCurrentPlayer);

        // Convert int to bool
        _gameWon = serializedGameState.SGameWon == 1;
        _gameTied = serializedGameState.SGameTied == 1;
    }
}


[Serializable]
public class SerializableBoardState
{
    public string[] _sBoard;
    public string _sCurrentPlayer;
    private int _sGameWon;
    private int _sGameTied;

    
    public string[] SBoard { get => _sBoard; set => _sBoard = value; }
    public string SCurrentPlayer { get => _sCurrentPlayer; set => _sCurrentPlayer = value; }
    public int SGameWon { get => _sGameWon; set => _sGameWon = value; }
    public int SGameTied { get => _sGameTied; set => _sGameTied = value; }
    
}


public interface IBoardStateStorage
{
    void SaveBoardState(PlayerType?[] board, PlayerType currentPlayer, bool gameWon, bool gameTied);
    BoardState LoadBoardState();
}


public class InMemoryBoardStateStorage : IBoardStateStorage
{
    private BoardState storedState;
    private bool hasStoredState = false;

    public void SaveBoardState(PlayerType?[] board, PlayerType currentPlayer, bool gameWon, bool gameTied)
    {
        storedState = new BoardState
        {
            Board = board.ToArray(),
            CurrentPlayer = currentPlayer,
            GameWon = gameWon,
            GameTied = gameTied
        };
        hasStoredState = true;
    }

    public BoardState LoadBoardState()
    {
        return hasStoredState? storedState: null;
    }
}


public class PlayerPrefsBoardStateStorage : IBoardStateStorage
{
    private const string PlayerPrefsKey = "SavedGameState";
    private bool hasStoredState = false;

    public void SaveBoardState(PlayerType?[] board, PlayerType currentPlayer, bool gameWon, bool gameTied)
    {
        BoardState gameState = new BoardState
        {
            Board = board.ToArray(),
            CurrentPlayer = currentPlayer,
            GameWon = gameWon,
            GameTied = gameTied
        };
        // Save the state to JSON, for setting it as a string to PlayerPrefs
        string jsonState = gameState.Serialize();
        PlayerPrefs.SetString(PlayerPrefsKey, jsonState);
        PlayerPrefs.Save();
        hasStoredState = true;
    }

    public BoardState LoadBoardState()
    {
        if (hasStoredState)
        {
            string jsonState = PlayerPrefs.GetString(PlayerPrefsKey);
            if (!string.IsNullOrEmpty(jsonState))
            {
                // Load the state from the JSON and deserialize to the desired types
                BoardState loadedState = new BoardState();
                loadedState.Deserialize(jsonState);
                if (loadedState != null && loadedState.Board != null)
                {
                    return loadedState;
                }
            }
        }
        return null;
    }
}
