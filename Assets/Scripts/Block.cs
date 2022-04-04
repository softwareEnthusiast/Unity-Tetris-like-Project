/*
 * Ömer Fatih Çelik RowMatch 06.02.2022
 * Block.cs
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DigitalRuby.Tween;

public enum ColorType
{
    RED = 0,
    GREEN = 1,
    BLUE = 2,
    YELLOW = 3,
}

public class Block : MonoBehaviour
{
    public const float SWIPE_RESIST = 0.25f;

    public GameObject block;

    private int id;
    private bool isMatched;
    private ColorType color;

    private Vector2 downTouchPosition;
    private Vector2 breakTouchPosition;
    private float swipeAngle;

    private Board board;

    // Start is called before the first frame update
    void Start()
    {
        isMatched = false;
        board = FindObjectOfType<Board>();
        block = gameObject;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnMouseDown()
    {
        Debug.Log("var: " + isMatched + " Func: " + IsMatched() + " Game: " + board.GetGameState());
        if ((board.GetGameState() == GameState.MOVE) && !IsMatched())
        {
            downTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void OnMouseUp()
    {
        if (board.GetGameState() == GameState.WAIT)
        {
            return;
        }
        Vector2 direction = Vector2.up;

        breakTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (DiffOverSwipeResist(downTouchPosition, breakTouchPosition))
        {
            swipeAngle = Mathf.Atan2(breakTouchPosition.y - downTouchPosition.y,
                         breakTouchPosition.x - downTouchPosition.x);
            swipeAngle = swipeAngle * 180 / Mathf.PI;

            if ((swipeAngle > -45) && (swipeAngle <= 45))
            {
                direction = Vector2.right;
            }
            else if ((swipeAngle > 45) && (swipeAngle <= 135))
            {
                direction = Vector2.up;
            }
            else if ((swipeAngle > 135) || (swipeAngle <= -135))
            {
                direction = Vector2.left;
            }
            else if ((swipeAngle < -45) && (swipeAngle >= -135))
            {
                direction = Vector2.down;
            }
            board.SwapPieces(id, direction);
        }
    }

    bool DiffOverSwipeResist(Vector2 downPoint, Vector2 upPoint)
    {
        return ((Mathf.Abs(upPoint.y - downPoint.y) > SWIPE_RESIST) ||
            (Mathf.Abs(upPoint.x - downPoint.x) > SWIPE_RESIST));
    }

    // priority can be 1 or 2. this will set layer order
    public void playSwipeAnimation(Vector2 direction)
    {
        Vector2 startPos = (Vector2)transform.position;
        Vector2 endPos = startPos + direction;
        block.Tween(null, startPos, endPos, 5.2f, TweenScaleFunctions.Linear, null);
    }

    public ColorType GetColor()
    {
        return color;
    }

    public void SetColor(ColorType c)
    {
        color = c;
    }

    public bool IsMatched()
    {
        return isMatched;
    }

    public void SetIsMatched()
    {
        isMatched = true;
    }

    public int GetId()
    {
        return id;
    }

    public void SetId(int newId)
    {
        id = newId;
    }
}