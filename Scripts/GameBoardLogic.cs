using MoonActive.Scripts;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime;

public class GameBoardLogic
{
    private GameView boardView;
    private UserActionEvents boardEvents;

    private PlayerType currentPlayer;
    private bool gameWon;
    private bool gameTied;
    private BoardGridSize gridSize;
    private PlayerType?[] board;
    private int boardLength;
    private GameStateSource currentSource;
    private IBoardStateStorage boardStateStorage;

    public GameBoardLogic(GameView gameView, UserActionEvents userActionEvents)
    {
        /*
         * Constructor
         */
        this.boardView = gameView;
        this.boardEvents = userActionEvents;
    }

    public void Initialize(int columns, int rows)
    {
        /*
         * Initialization logic will be called once per session
         */
        // Assuming a valid input is only n*n, as you answer me in the mail
        if (columns != rows)
            throw new ArgumentException("A valid board size is only N * N");

        this.boardEvents.StartGameClicked += HandleStartGameClicked;
        this.gridSize = new BoardGridSize(rows, columns);
        boardLength = rows * columns;
        board = new PlayerType?[boardLength];

        // Set the default source (as in the ui)
        this.currentSource = GameStateSource.PlayerPrefs;
        this.boardStateStorage = new PlayerPrefsBoardStateStorage();
    }

    public void DeInitialize()
    {
        /*
         * DeInitialization logic will be called once per session, at disposal
         */
        boardEvents.StartGameClicked -= HandleStartGameClicked;
        boardEvents.TileClicked -= HandleTileClicked;
        boardEvents.SaveStateClicked -= HandleSaveStateClicked;
        boardEvents.LoadStateClicked -= HandleLoadStateClicked;
        board = new PlayerType?[boardLength];
    }

    private void ResetBoard()
    {
        Array.Fill(board, null);      
    }

    private void HandleStartGameClicked()
    {
        gameWon = false;
        gameTied = false;
        ResetBoard();
        currentPlayer = PlayerType.PlayerX;

        // Here and not in the Initialize so that the player couldn't start before click on the button
        this.boardEvents.TileClicked += HandleTileClicked;
        this.boardEvents.SaveStateClicked += HandleSaveStateClicked;
        this.boardEvents.LoadStateClicked += HandleLoadStateClicked;
        boardView.StartGame(currentPlayer);
    }

    private void HandleTileClicked(BoardTilePosition tilePosition)
    {
        if (!gameWon && !gameTied)
        {          
            int index = tilePosition.Row * gridSize.Columns + tilePosition.Column;
            // Check validity of position (empty indeed and between the valid range)
            if (!board[index].HasValue && index >= 0 && index < board.Length)
            {
                // Update the tile sign and check for valid moves
                boardView.SetTileSign(currentPlayer, tilePosition);
                board[index] = currentPlayer;

                // Check for win or tie
                if (!CheckForWin()) {
                    if (!CheckForTie())
                    {
                        // Switch to the next player's turn
                        if (!gameWon && !gameTied)
                        {
                            currentPlayer = (currentPlayer == PlayerType.PlayerX) ? PlayerType.PlayerO : PlayerType.PlayerX;
                            boardView.ChangeTurn(currentPlayer);
                        }
                    }
                }              
            }
        }
    }

    private bool CheckForTie()
    {
        bool isBoardFull = board.All(tile => tile.HasValue);
        if (isBoardFull)
        {
            gameTied = true;
            boardView.GameTie();
            return true;
        }
        return false;
    }

    private bool CheckForWin()
    {
        // Check rows
        for (int row = 0; row < gridSize.Rows; row++)
        {
            if (CheckRowForWin(row))
                return true;
        }

        // Check columns
        for (int col = 0; col < gridSize.Columns; col++)
        {
            if (CheckColumnForWin(col))
                return true;
        }

        // Check diagonals
        if (CheckDiagonalForWin() || CheckAntiDiagonalForWin())
            return true;
        return false;
    }

    private bool CheckRowForWin(int row)
    {
        // First tile in the row
        PlayerType? playerNullable = board[row * gridSize.Columns];
        if (!playerNullable.HasValue)
            return false;

        PlayerType player = playerNullable.Value;
        for (int col = 1; col < gridSize.Columns; col++)
        {
            if (board[row * gridSize.Columns + col] != player)
                return false;
        }

        DeclareWinner(player);
        return true;
    }

    private bool CheckColumnForWin(int col)
    {
        // First tile in the col
        PlayerType? playerNullable = board[col];
        if (!playerNullable.HasValue)
            return false;

        PlayerType player = playerNullable.Value;
        for (int row = 1; row < gridSize.Rows; row++)
        {
            if (board[row * gridSize.Columns + col] != player)
                return false;
        }

        DeclareWinner(player);
        return true;
    }

    private bool CheckDiagonalForWin()
    {
        PlayerType? playerNullable = board[0];
        if (!playerNullable.HasValue)
            return false;

        PlayerType player = playerNullable.Value;
        for (int i = 1; i < gridSize.Columns; i++)
        {
            if (board[i * (gridSize.Columns + 1)] != player)
                return false;
        }

        DeclareWinner(player);
        return true;
    }

    private bool CheckAntiDiagonalForWin()
    {
        PlayerType? playerNullable = board[gridSize.Columns - 1];
        if (!playerNullable.HasValue)
            return false;

        PlayerType player = playerNullable.Value;
        for (int i = 2; i <= gridSize.Columns; i++)
        {
            if (board[i * (gridSize.Columns - 1)] != player)
                return false;
        }

        DeclareWinner(player);
        return true;
    }

    private void DeclareWinner(PlayerType player)
    {
        gameWon = true;
        boardView.GameWon(player);
    }

    private void InitializeBoardStateStorage(GameStateSource source)
    {
        if (!source.Equals(currentSource))
        {
            currentSource = source;
            switch (source)
            {
                case GameStateSource.InMemory:
                    boardStateStorage = new InMemoryBoardStateStorage();
                    break;

                case GameStateSource.PlayerPrefs:
                    boardStateStorage = new PlayerPrefsBoardStateStorage();
                    break;

                // If we want to add additional storage types in the future, then it will be easy to add more cases
                default:
                    throw new ArgumentException("Invalid BoardStateSource");
            }
        }
    }

    private void HandleSaveStateClicked(GameStateSource source)
    {
        InitializeBoardStateStorage(source);
        boardStateStorage.SaveBoardState(board, currentPlayer, gameWon, gameTied);

    }

    private void HandleLoadStateClicked(GameStateSource source)
    {
        InitializeBoardStateStorage(source);
        BoardState loadedState = boardStateStorage.LoadBoardState();
        if (loadedState != null)
        {
            RestartAndLoad(loadedState);
        }
        else
        {
            throw new Exception("There is no state saved");
        }
    }

    private void RestartAndLoad(BoardState loadedState)
    {
        // Extract the loaded state parameters
        PlayerType?[] loadedBoard = loadedState.Board;
        gameWon = loadedState.GameWon;
        gameTied = loadedState.GameTied;
        currentPlayer = loadedState.CurrentPlayer;

        // If the board is not null and the lengths match, copy the loaded board
        if (loadedBoard != null && loadedBoard.Length == boardLength)
        {
            // Restart the game, then set the saved state
            board = new PlayerType?[boardLength];
            Array.Copy(loadedBoard, board, boardLength);
            boardView.StartGame(currentPlayer);        

            // Update the UI after restarting the game
            UpdateUI(loadedBoard);
        }
        else
        {
            throw new Exception("The loaded board is invalid");
        }
    }

    private void UpdateUI(PlayerType?[] loadedBoard)
    {
        // Set the tile signs on the BoardView based on the loaded state
        for (int i = 0; i < boardLength; i++)
        {
            if (loadedBoard[i].HasValue)
            {
                int row = i / gridSize.Columns;
                int col = i % gridSize.Columns;
                BoardTilePosition tilePosition = new BoardTilePosition(row, col);
                boardView.SetTileSign(board[i].Value, tilePosition);
            }
        }
        // Check if the game is won or tied and update the UI accordingly
        if (gameWon)
        {
            boardView.GameWon(currentPlayer);
        }
        else if (gameTied)
        {
            boardView.GameTie();
        }
    }
}
