/*
 * Ömer Fatih Çelik RowMatch 06.02.2022
 * Board.cs
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum GameState
{
    WAIT,
    MOVE
}

public struct Point
{
    public int row;
    public int column;
    
    public Point(int row, int column)
    {
        this.row = row;
        this.column = column;
    }
}

// initially a struct
public class Partition
{
    public int firstRow;
    public int secondRow;
    public int redCount;
    public int greenCount;
    public int blueCount;
    public int yellowCount;

    public Partition(int firstRow, int secondRow)
    {
        this.firstRow = firstRow;
        this.secondRow = secondRow;
        this.redCount = 0;
        this.greenCount = 0;
        this.blueCount = 0;
        this.yellowCount = 0;
    }

    public void increaseCount(ColorType color)
    {
        if (color == ColorType.RED)
        {
            this.redCount++;
        }
        if (color == ColorType.GREEN)
        {
            this.greenCount++;
        }
        if (color == ColorType.BLUE)
        {
            this.blueCount++;
        }
        if (color == ColorType.YELLOW)
        {
            this.yellowCount++;
        }
    }

    public void decreaseCountBy(ColorType color, int count)
    {
        if (color == ColorType.RED)
        {
            this.redCount -= count;
        }
        if (color == ColorType.GREEN)
        {
            this.greenCount -= count;
        }
        if (color == ColorType.BLUE)
        {
            this.blueCount -= count;
        }
        if (color == ColorType.YELLOW)
        {
            this.yellowCount -= count;
        }
    }

    public void cleanCounts()
    {
        this.redCount = 0;
        this.greenCount = 0;
        this.blueCount = 0;
        this.yellowCount = 0;
    }

    public void setFirstRow(int row)
    {
        this.firstRow = row;
    }

    public void setSecondRow(int row)
    {
        this.secondRow = row;
    }
}

public class Board : MonoBehaviour
{
    public const float BLOCK_PADDING_RATIO = 0.25F;
    public const float BLOCK_SIDE_LENGTH = 0.32F;

    public int highscore;
    public int levelNumber;
    public int boardHeight; // in number of blocks
    public int boardWidth; // in number of blocks
    public int moveCount;
    public string initialColors;

    public int points = 0;
    private int colorIndex = 0;

    public Text highscoreText;
    public Text currentScoreText;
    public Text movesText;

    public List<Sprite> blockSprites = new List<Sprite>();
    private Block[,] blocks;

    public GameState currentState;
    public GameObject prefabBlock;
    public GameObject prefabGrid;

    private float actualBlockWidth;
    private float actualBlockHeight;
    private List<Partition> partitions;

    private bool startSceneEnd;
    private int sceneEndCounter;

    // Start is called before the first frame update
    void Start()
    {
        startSceneEnd = false;
        levelNumber = PlayerPrefs.GetInt("levelNumber");
        boardHeight = PlayerPrefs.GetInt("boardHeight");
        boardWidth = PlayerPrefs.GetInt("boardWidth");
        moveCount = PlayerPrefs.GetInt("moveCount");
        initialColors = PlayerPrefs.GetString("initialColors");
        highscore = PlayerPrefs.GetInt("highscore");
        partitions = new List<Partition>();
        partitions.Add(new Partition(0, boardHeight - 1));
        InitializeScorePanel();
        CreateBoard(BLOCK_SIDE_LENGTH,
                    BLOCK_SIDE_LENGTH);
        currentState = GameState.MOVE;
    }

    void InitializeScorePanel()
    {
        highscoreText = GameObject.Find("ScoreText1").GetComponent<Text>();
        currentScoreText = GameObject.Find("ScoreText2").GetComponent<Text>();
        movesText = GameObject.Find("MovesText").GetComponent<Text>();
        UpdateScorePanel();
    }

    void UpdateScorePanel()
    {
        highscoreText.text = "Highscore: " + highscore;
        currentScoreText.text = "Score: " + points;
        movesText.text = "Moves: " + moveCount;
    }

    /*
     * Board is created from left bottom corner
     */
    void CreateBoard(float blockWidth, float blockHeight)
    {
        actualBlockWidth = (blockWidth * (1 + (BLOCK_PADDING_RATIO * 2)));
        actualBlockHeight = (blockHeight * (1 + (BLOCK_PADDING_RATIO * 2)));

        blocks = new Block[boardHeight, boardWidth];

        transform.position = new Vector2(actualBlockWidth * (boardWidth - 1) / (-2),
                                         actualBlockHeight * (boardHeight - 1) / (-2));

        for (int h = 0; h < boardHeight; h++)
        {
            for (int w = 0; w < boardWidth; w++)
            {
                float xPosition = transform.position.x + (actualBlockWidth * w);
                float yPosition = transform.position.y + (actualBlockHeight * h);

                GameObject newBlock = Instantiate(prefabBlock,
                                                  new Vector3(xPosition, yPosition, 0),
                                                  prefabBlock.transform.rotation);
                Instantiate(prefabGrid,
                            new Vector3(xPosition, yPosition, 0),
                            prefabGrid.transform.rotation);
                blocks[h, w] = newBlock.GetComponent<Block>();
                newBlock.transform.parent = transform;
                AssignInitialColor(blocks[h, w], initialColors[colorIndex]);
                blocks[h, w].SetId(colorIndex);
                colorIndex++;
            }
        }
    }

    void AssignInitialColor(Block block, char c)
    {
        if (c == 'r')
        {
            block.SetColor(ColorType.RED);
            block.GetComponent<SpriteRenderer>().sprite = blockSprites[0];
            partitions[0].increaseCount(ColorType.RED);
        }
        else if (c == 'g')
        {
            block.SetColor(ColorType.GREEN);
            block.GetComponent<SpriteRenderer>().sprite = blockSprites[1];
            partitions[0].increaseCount(ColorType.GREEN);
        }
        else if (c == 'b')
        {
            block.SetColor(ColorType.BLUE);
            block.GetComponent<SpriteRenderer>().sprite = blockSprites[2];
            partitions[0].increaseCount(ColorType.BLUE);
        }
        else
        {
            block.SetColor(ColorType.YELLOW);
            block.GetComponent<SpriteRenderer>().sprite = blockSprites[3];
            partitions[0].increaseCount(ColorType.YELLOW);
        }
    }

    public bool SwapPieces(int requesterId, Vector2 direction)
    {
        currentState = GameState.WAIT;
        Point blockLoc = GetBlockLocationFromId(requesterId);
        Debug.Log(blockLoc.row + " row/col" + blockLoc.column);
        if (!SwapWithOtherPossible(blockLoc, direction) || startSceneEnd)
        {
            currentState = GameState.MOVE;
            return false;
        }

        Swap(blockLoc, direction);

        CheckRowMatchBoard(blockLoc, direction);

        if (!CheckMorePointsPossible())
        {
            if (points > highscore)
            {
                Debug.Log("Made the highscore");
                PlayerPrefs.SetInt("highscore" + levelNumber, points);

                currentScoreText.text = "New Highscore " + highscore;
                highscoreText.text = "";
                movesText.text = "";
                startSceneEnd = true;
                sceneEndCounter = 150; 
            }
            else
            {
                Debug.Log("No highscore");

                SceneManager.LoadScene("MenuScene");
            }
        }

        currentState = GameState.MOVE;
        moveCount--;
        UpdateScorePanel();
        return true;
    }

    Point GetBlockLocationFromId(int id)
    {
        Point searchedBlockLocation = new Point(-1, -1);
        bool blockFound = false;
        for (int h = 0; (h < boardHeight) && !blockFound; h++)
        {
            for (int w = 0; (w < boardWidth) && !blockFound; w++)
            {
                if (blocks[h, w].GetId() == id)
                {
                    searchedBlockLocation = new Point(h, w);
                    blockFound = true;
                }
            }
        }
        return searchedBlockLocation;
    }

    bool SwapWithOtherPossible(Point blockLoc, Vector2 direction)
    {
        ColorType firstColor = blocks[blockLoc.row, blockLoc.column].GetColor();
        if (direction == Vector2.up)
        {
            if (blockLoc.row >= boardHeight - 1)
            {
                return false;
            }
            if (blocks[blockLoc.row + 1, blockLoc.column].IsMatched() ||
                firstColor == blocks[blockLoc.row + 1, blockLoc.column].GetColor())
            {
                // if other block is matched or the blocks are same color
                return false;
            }
        }
        else if (direction == Vector2.down)
        {
            if (blockLoc.row <= 0)
            {
                return false;
            }
            if (blocks[blockLoc.row - 1, blockLoc.column].IsMatched() ||
                firstColor == blocks[blockLoc.row - 1, blockLoc.column].GetColor())
            {
                // if other block is matched or the blocks are same color
                return false;
            }
        }
        else if (direction == Vector2.left)
        {
            if (blockLoc.column <= 0)
            {
                return false;
            }
            if ((blocks[blockLoc.row, blockLoc.column].GetColor()
                == blocks[blockLoc.row, blockLoc.column - 1].GetColor()))
            {
                // if the blocks are same color
                return false;
            }
        }
        else if (direction == Vector2.right)
        {
            if (blockLoc.column >= boardWidth - 1)
            {
                return false;
            }
            if ((blocks[blockLoc.row, blockLoc.column].GetColor()
                == blocks[blockLoc.row, blockLoc.column + 1].GetColor()))
            {
                // if the blocks are same color
                return false;
            }
        }
        else
        {
            return false;
        }
        return true;
    }

    void Swap(Point blockLoc, Vector2 direction)
    {
        Block temp = blocks[blockLoc.row, blockLoc.column];
        if (direction == Vector2.up)
        {
            blocks[blockLoc.row, blockLoc.column] = blocks[blockLoc.row + 1, blockLoc.column];
            blocks[blockLoc.row + 1, blockLoc.column] = temp;
            blocks[blockLoc.row + 1, blockLoc.column].transform.position += new Vector3(0, actualBlockHeight, 0);
            blocks[blockLoc.row, blockLoc.column].transform.position -= new Vector3(0, actualBlockHeight, 0);
            blocks[blockLoc.row, blockLoc.column].playSwipeAnimation(new Vector2(0,actualBlockHeight));
            blocks[blockLoc.row + 1, blockLoc.column].playSwipeAnimation(new Vector2(0, -1 * actualBlockHeight));
        }
        else if (direction == Vector2.down)
        {
            blocks[blockLoc.row, blockLoc.column] = blocks[blockLoc.row - 1, blockLoc.column];
            blocks[blockLoc.row - 1, blockLoc.column] = temp;
            blocks[blockLoc.row - 1, blockLoc.column].transform.position -= new Vector3(0, actualBlockHeight, 0);
            blocks[blockLoc.row, blockLoc.column].transform.position += new Vector3(0, actualBlockHeight, 0);
            blocks[blockLoc.row, blockLoc.column].playSwipeAnimation(new Vector2(0, actualBlockHeight));
            blocks[blockLoc.row - 1, blockLoc.column].playSwipeAnimation(new Vector2(0, -1 * actualBlockHeight));
        }
        else if (direction == Vector2.left)
        {
            blocks[blockLoc.row, blockLoc.column] = blocks[blockLoc.row, blockLoc.column - 1];
            blocks[blockLoc.row, blockLoc.column - 1] = temp;
            blocks[blockLoc.row, blockLoc.column - 1].transform.position -= new Vector3(actualBlockWidth, 0, 0);
            blocks[blockLoc.row, blockLoc.column].transform.position += new Vector3(actualBlockWidth, 0, 0);
            blocks[blockLoc.row, blockLoc.column].playSwipeAnimation(new Vector2(actualBlockWidth, 0));
            blocks[blockLoc.row, blockLoc.column - 1].playSwipeAnimation(new Vector2(-1 * actualBlockWidth, 0));
        }
        else if (direction == Vector2.right)
        {
            blocks[blockLoc.row, blockLoc.column] = blocks[blockLoc.row, blockLoc.column + 1];
            blocks[blockLoc.row, blockLoc.column + 1] = temp;
            blocks[blockLoc.row, blockLoc.column + 1].transform.position += new Vector3(actualBlockWidth, 0, 0);
            blocks[blockLoc.row, blockLoc.column].transform.position -= new Vector3(actualBlockWidth, 0, 0);
            blocks[blockLoc.row, blockLoc.column].playSwipeAnimation(new Vector2(-1 * actualBlockWidth, 0));
            blocks[blockLoc.row, blockLoc.column + 1].playSwipeAnimation(new Vector2(actualBlockWidth, 0));
        }
    }

    void CheckRowMatchBoard(Point blockLoc, Vector2 direction)
    {
        if (direction == Vector2.up)
        {
            CheckRowMatchRow(blockLoc.row);
            CheckRowMatchRow(blockLoc.row + 1);
        }
        else if (direction == Vector2.down)
        {
            CheckRowMatchRow(blockLoc.row);
            CheckRowMatchRow(blockLoc.row - 1);
        }
    }

    void CheckRowMatchRow(int row)
    {
        bool rowMatched = true;
        ColorType firstColor = blocks[row, 0].GetColor();
        for (int i = 1; i < boardWidth; i++)
        {
            if (blocks[row, i].GetColor() != firstColor)
            {
                rowMatched = false;
            }
        }
        if (rowMatched)
        {
            for (int i =0; i < boardWidth; i++)
            {
                blocks[row, i].SetIsMatched();
            }
            UpdatePointsForColor(firstColor);
            UpdatePartitions(row, firstColor);
        }
    }

    void UpdatePointsForColor(ColorType color)
    {
        if (color == ColorType.RED)
        {
            points += 100;
        }
        else if (color == ColorType.GREEN)
        {
            points += 150;
        }
        else if (color == ColorType.BLUE)
        {
            points += 200;
        }
        else if (color == ColorType.YELLOW)
        {
            points += 250;
        }
    }

    void UpdatePartitions(int row, ColorType color)
    {
        for (int i = 0; i < partitions.Count; i++)
        {
            if (partitions[i].firstRow <= row && partitions[i].secondRow >= row)
            {
                if (partitions[i].firstRow == partitions[i].secondRow)
                {
                    partitions.RemoveAt(i);
                }
                else if (partitions[i].firstRow == row)
                {
                    partitions[i].setFirstRow(row + 1);
                    UpdateColorCountPartition(partitions[i], color);
                    
                }
                else if (partitions[i].secondRow == row)
                {
                    partitions[i].setSecondRow(row - 1);
                    UpdateColorCountPartition(partitions[i], color);
                }
                else
                {
                    Partition newPartition = new Partition(partitions[i].firstRow, row - 1);
                    CountColorsPartition(newPartition);
                    partitions.Add(newPartition);
                    partitions[i].setFirstRow(row + 1);
                    CountColorsPartition(partitions[i]);
                }
                return;
            }
        }
    }

    void UpdateColorCountPartition(Partition partition, ColorType color)
    {
        if (color == ColorType.RED)
        {
            partition.decreaseCountBy(ColorType.RED, boardWidth);
        }
        else if (color == ColorType.GREEN)
        {
            partition.decreaseCountBy(ColorType.GREEN, boardWidth);
        }
        else if (color == ColorType.BLUE)
        {
            partition.decreaseCountBy(ColorType.BLUE, boardWidth);
        }
        else if (color == ColorType.YELLOW)
        {
            partition.decreaseCountBy(ColorType.YELLOW, boardWidth);
        }
    }

    void CountColorsPartition(Partition partition)
    {
        partition.cleanCounts();
        for (int i = partition.firstRow; i <= partition.secondRow; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                if (blocks[i, j].GetColor() == ColorType.RED)
                {
                    partition.increaseCount(ColorType.RED);
                }
                else if (blocks[i, j].GetColor() == ColorType.GREEN)
                {
                    partition.increaseCount(ColorType.GREEN);
                }
                else if (blocks[i, j].GetColor() == ColorType.BLUE)
                {
                    partition.increaseCount(ColorType.BLUE);
                }
                else if (blocks[i, j].GetColor() == ColorType.YELLOW)
                {
                    partition.increaseCount(ColorType.YELLOW);
                }
            }
        }
    }

    bool CheckMorePointsPossible()
    {
        for (int i = 0; i < partitions.Count; i++)
        {
            if (CheckMorePointsPossiblePartition(partitions[i]))
            {
                return true;
            }
        }
        return false;
    }

    bool CheckMorePointsPossiblePartition(Partition partition)
    {
        bool redPossible = partition.redCount >= boardWidth;
        bool greenPossible = partition.greenCount >= boardWidth;
        bool bluePossible = partition.blueCount >= boardWidth;
        bool yellowPossible = partition.yellowCount >= boardWidth;
        List<Point> redPoints = new List<Point>();
        List<Point> greenPoints = new List<Point>();
        List<Point> bluePoints = new List<Point>();
        List<Point> yellowPoints = new List<Point>();


        if (!redPossible && !greenPossible && !bluePossible && !yellowPossible)
        {
            return false;
        }

        if (redPossible)
        {
            redPoints = CreatePointsList(partition, ColorType.RED);
        }
        if (greenPossible)
        {
            greenPoints = CreatePointsList(partition, ColorType.GREEN);
        }
        if (bluePossible)
        {
            bluePoints = CreatePointsList(partition, ColorType.BLUE);
        }
        if (yellowPossible)
        {
            yellowPoints = CreatePointsList(partition, ColorType.YELLOW);
        }

        bool redFound = false;
        bool greenFound = false;
        bool blueFound = false;
        bool yellowFound = false;
        for (int i = partition.firstRow; i <= partition.secondRow
            && (!(redFound || greenFound || blueFound || yellowFound)); i++)
        {
            if (redPossible)
            {
                redFound = CheckMorePointsPossibleColoredRow(i, ColorType.RED, redPoints);
            }
            if (greenPossible)
            {
                greenFound = CheckMorePointsPossibleColoredRow(i, ColorType.GREEN, greenPoints);
            }
            if (bluePossible)
            {
                blueFound = CheckMorePointsPossibleColoredRow(i, ColorType.BLUE, bluePoints);
            }
            if (yellowPossible)
            {
                yellowFound = CheckMorePointsPossibleColoredRow(i, ColorType.YELLOW, yellowPoints);
            }
        }

        return (redFound || greenFound || blueFound || yellowFound);
    }

    bool CheckMorePointsPossibleColoredRow(int row, ColorType color, List<Point> blockPoints)
    {
        List<List<int>> requiredMoves = new List<List<int>>();

        for (int i = 0; i < boardWidth; i++)
        {
            if (blocks[row, i].GetColor() != color)
            {
                List<int> newSpot = new List<int>();
                for (int j = 0; j < blockPoints.Count; j++)
                {
                    if (blockPoints[j].row == row)
                    {
                        newSpot.Add(1000); // can't use this block to fill
                    }
                    else
                    {
                        newSpot.Add(Math.Abs(blockPoints[j].row - row) + Math.Abs(blockPoints[j].column - i));
                    }
                }
                requiredMoves.Add(newSpot);
            }
        }
        // at this point we have calculated required moves for
        // each empty spot and block pair
        int greedyCalculation = 0;
        int spotCount = requiredMoves.Count;
        List<List<int>> temp = new List<List<int>>(requiredMoves);

        for (int i = 0; i < spotCount; i++)
        {
            int lowestFound = 1000;
            int foundFirstIndex = 0;
            int foundSecondIndex = 0;
            for (int j = 0; j < requiredMoves.Count; j++)
            {
                for (int k = 0; k < requiredMoves[j].Count; k++)
                {
                    if (requiredMoves[j][k] < lowestFound)
                    {
                        lowestFound = requiredMoves[j][k];
                        foundFirstIndex = j;
                        foundSecondIndex = k;
                    }
                }
            }
            greedyCalculation += lowestFound;
            requiredMoves.RemoveAt(foundFirstIndex);
            for (int j = 0; j < requiredMoves.Count; j++)
            {
                requiredMoves[j].RemoveAt(foundSecondIndex);
            }
        }
        requiredMoves.Clear();
        requiredMoves = temp;
        int newLowest = greedyCalculation;

        // TODO: brute force it

        return (moveCount > newLowest);
    }

    List<Point> CreatePointsList(Partition partition, ColorType color)
    {
        List<Point> points = new List<Point>();
        for (int i = partition.firstRow; i <= partition.secondRow; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                if (blocks[i, j].GetColor() == color)
                {
                    points.Add(new Point(i, j));
                }
            }
        }
        return points;
    }

    public GameState GetGameState()
    {
        return currentState;
    }

    public void SetGameState(GameState state)
    {
        currentState = state;
    }

    // Update is called once per frame
    void Update()
    {
        if (startSceneEnd)
        {
            if (sceneEndCounter > 0)
            {
                sceneEndCounter--;
            }
            else
            {
                SceneManager.LoadScene("MenuScene");
            }
        }
    }
}
