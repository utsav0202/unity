using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour
{
    public bool isWhite;
    public bool isKing;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public bool ValidMove (Piece[,] board, int x1, int y1, int x2, int y2)
    {
        // destination should be blank
        // 1 diagonal is valid
        // 2 diagonal is valid if other color in between

        if (board[x2, y2] != null)
            return false;

        int xDiff = Mathf.Abs(x2 - x1);
        int yDiff = y2 - y1;

        if (isWhite)
        {
            if (xDiff == 1)
            {
                if (yDiff == 1)
                    return true;
            }
            else if (xDiff == 2)
            {
                if (yDiff == 2)
                {
                    Piece p = board[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null && isWhite != p.isWhite)
                        return true;
                }
            }
        }

        if (!isWhite)
        {
            if (xDiff == 1)
            {
                if (yDiff == -1)
                    return true;
            }
            else if (xDiff == 2)
            {
                if (yDiff == -2)
                {
                    Piece p = board[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null && isWhite != p.isWhite)
                        return true;
                }
            }
        }

        return false;
    }

    public bool CanKill (Piece[,] board, int x, int y)
    {
        if (isWhite)
        {
            //top left
            if (x >= 2 && y <= 5)
            {
                if (board[x - 2, y + 2] == null
                    && board[x - 1, y + 1] != null
                    && board[x - 1, y + 1].isWhite != isWhite)
                    return true;
            }

            //top right
            if (x <= 5 && y <= 5)
            {
                if (board[x + 2, y + 2] == null
                    && board[x + 1, y + 1] != null
                    && board[x + 1, y + 1].isWhite != isWhite)
                    return true;
            }
        }

        if (!isWhite)
        {
            //bottom left
            if (x >= 2 && y >= 2)
            {
                if (board[x - 2, y - 2] == null
                    && board[x - 1, y - 1] != null
                    && board[x - 1, y - 1].isWhite != isWhite)
                    return true;
            }

            //top right
            if (x <= 5 && y >= 2)
            {
                if (board[x + 2, y - 2] == null
                    && board[x + 1, y - 1] != null
                    && board[x + 1, y - 1].isWhite != isWhite)
                    return true;
            }
        }

        return false;
    }
}
