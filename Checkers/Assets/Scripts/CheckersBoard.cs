using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckersBoard : MonoBehaviour
{
    public static CheckersBoard Instance { set; get; }

    public Piece[,] pieces = new Piece[8, 8];
    public GameObject whitePiecePrefab;
    public GameObject blackPiecePrefab;
    public bool isWhite;

    private Vector3 boardOffset = new Vector3(-4.0f, 0, -4.0f);
    private Vector3 pieceOffset = new Vector3(0.5f, 0, 0.5f);

    private Vector2 mouseOver;
    private Vector2 oldMO;

    private Piece selectedPiece;
    private Vector2 startDrag;
    private Vector2 endDrag;

    private bool isWhiteTurn;
    bool hasKilled;

    private Client client;

    private List<Piece> killerPieces = new List<Piece>();

	// Use this for initialization
	void Start ()
    {
        Instance = this;
        client = FindObjectOfType<Client>();
        isWhite = client.isHost;
        GenerateBoard();
        isWhiteTurn = true;
	}
	
	// Update is called once per frame
	void Update ()
    {
        UpdateMouseOver();

        if (mouseOver != oldMO)
        {
            oldMO = mouseOver;
            //Debug.Log(mouseOver);
        }

        if (isWhite ? isWhiteTurn : !isWhiteTurn)
        {
            int x = (int)mouseOver.x;
            int y = (int)mouseOver.y;

            if (selectedPiece != null)
                LiftAndDragPiece(selectedPiece);

            if (Input.GetMouseButtonDown(0)
                && selectedPiece == null)
            {
                SelectPiece(x, y);
            }

            if (Input.GetMouseButtonUp (0)
                 && selectedPiece != null)
            {
                TryMove((int)startDrag.x,
                        (int)startDrag.y,
                        x, y);
            }
        }
    }

    private void LiftAndDragPiece (Piece p)
    {
        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                            out hit, 25.0f, LayerMask.GetMask("Board")))
        {
            p.transform.position = hit.point + Vector3.up;
        }
    }

    public void TryMove (int x1, int y1, int x2, int y2)
    {
        // for multiplayer support
        startDrag = new Vector2(x1, y1);
        endDrag = new Vector2(x2, y2);
        selectedPiece = pieces[x1, y1];

        hasKilled = false;

        if (x2 < 0 || x2 >= 8 || y2 < 0 || y2 >= 8)
        {
            if (selectedPiece != null)
                MovePiece(selectedPiece, x1, y1);

            //startDrag = Vector2.zero;
            startDrag.x = startDrag.y = -1;
            selectedPiece = null;
            return;
        }

        if (selectedPiece != null)
        {
            if (startDrag == endDrag)
            {
                MovePiece(selectedPiece, x1, y1);
                //startDrag = Vector2.zero;
                startDrag.x = startDrag.y = -1;
                selectedPiece = null;
                return;
            }


            ReadKillerPieces();

            bool validMove = selectedPiece.ValidMove(pieces, x1, y1, x2, y2);
            //Debug.Log(x2 + "," + y2 + " is " + validMove + " move");
            if (validMove)
            {
                if (Mathf.Abs(x2 - x1) == 2)
                {
                    Piece p = pieces[(x1 + x2) / 2, (y1 + y2) / 2];
                    if (p != null)
                    {
                        pieces[(x1 + x2) / 2, (y1 + y2) / 2] = null;
                        Destroy((p as MonoBehaviour).gameObject, 0.2f);
                        hasKilled = true;
                    }
                }

                if (killerPieces.Count > 0 && !hasKilled)
                {
                    MovePiece(selectedPiece, x1, y1);
                    //startDrag = Vector2.zero;
                    startDrag.x = startDrag.y = -1;
                    selectedPiece = null;
                    return;
                }

                pieces[x1, y1] = null;
                pieces[x2, y2] = selectedPiece;
                MovePiece(selectedPiece, x2, y2);

                EndTurn();
            }
            else
            {
                MovePiece(selectedPiece, x1, y1);
                //startDrag = Vector2.zero;
                startDrag.x = startDrag.y = -1;
                selectedPiece = null;
                return;
            }
        }
    }

    private void EndTurn()
    {
        int x = (int)endDrag.x;
        int y = (int)endDrag.y;

        // make king
        if ((selectedPiece.isWhite && !selectedPiece.isKing && y == 7)
            || (!selectedPiece.isWhite && !selectedPiece.isKing && y == 0))
        {
            selectedPiece.isKing = true;
            selectedPiece.transform.Rotate(Vector3.right * 180);
        }

        string msg = "CMOV"
                      + "|" + startDrag.x.ToString()
                      + "|" + startDrag.y.ToString()
                      + "|" + endDrag.x.ToString()
                      + "|" + endDrag.y.ToString();
        client.Send(msg);

        // multi move
        if (hasKilled && selectedPiece.CanKill(pieces, x, y))
        {
            startDrag = endDrag;
            return;
        }

        selectedPiece = null;
        //startDrag = Vector2.zero;
        startDrag.x = startDrag.y = -1;
        isWhiteTurn = !isWhiteTurn;

        CheckVictory();
    }

    private void CheckVictory ()
    {
        bool hasWhite = false;
        bool hasBlack = false;
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Piece p = pieces[x, y];
                if (p != null && p.isWhite)
                     hasWhite = true;
                else if (p != null && !p.isWhite)
                    hasBlack = true;
            }
        }

        if (!hasBlack)
            Victory(true);
        else if (!hasWhite)
            Victory(false);
    }

    private void Victory (bool whiteWon)
    {
        if (whiteWon)
            Debug.Log("White won");
        else
            Debug.Log("Black won");
    }

    private void SelectPiece (int x, int y)
    {
        // check bound
        if (x < 0 || x >= 8 || y < 0 || y >= 8)
            return;

        Piece p = pieces[x, y];
        if (p != null
            && p.isWhite == isWhiteTurn)
            //&& (killerPieces.Count == 0 || killerPieces.Find(fp => fp == p)))
        {
            selectedPiece = p;
            startDrag = mouseOver;
            Debug.Log(p.name + " at " + startDrag + " is selected");
        }
    }

    private void UpdateMouseOver()
    {
        // if it is my turn

        if (!Camera.main)
        {
            Debug.Log("Unable to find main camera");
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
                            out hit, 25.0f, LayerMask.GetMask ("Board")))
        {
            mouseOver.x = (int)(hit.point.x - boardOffset.x);
            mouseOver.y = (int)(hit.point.z - boardOffset.z);
        }
        else
        {
            mouseOver.x = -1;
            mouseOver.y = -1;
        }
    }
    
    private void GenerateBoard ()
    {
        for (int y = 0; y < 3; y++)
        {
            int shift = (y % 2 == 0) ? 0 : 1;
            for (int x = 0; x < 8; x+=2)
            {
                GeneratePiece(x+shift, y);
            }
        }

        for (int y = 7; y > 4; y--)
        {
            int shift = (y % 2 == 0) ? 0 : 1;
            for (int x = 0; x < 8; x += 2)
            {
                GeneratePiece(x + shift, y);
            }
        }
    }

    private void GeneratePiece (int x, int y)
    {
        GameObject prefab = y > 4 ? blackPiecePrefab : whitePiecePrefab;
        GameObject go = Instantiate(prefab) as GameObject;
        go.transform.SetParent(transform);
        Piece p = go.GetComponent<Piece>();
        pieces[x, y] = p;
        MovePiece(p, x, y);
    }

    private void MovePiece (Piece p, int x, int y)
    {
        p.transform.position = (Vector3.right * x)
                                + (Vector3.forward * y)
                                + boardOffset
                                + pieceOffset;
    }

    private void ReadKillerPieces ()
    {
        killerPieces = new List<Piece>();

        for (int x = 0; x < 8; x++)
            for (int y = 0; y < 8; y++)
                if (pieces[x, y] != null
                    && pieces[x, y].isWhite == isWhiteTurn
                    && pieces[x, y].CanKill(pieces, x, y))
                    killerPieces.Add(pieces[x, y]);
    }
}
