using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;

public class Client : MonoBehaviour
{
    public string clientName;
    public bool isHost;

    private bool socketReady;
    private TcpClient socket;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    private List<GameClient> players = new List<GameClient>();

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!socketReady)
            return;

        if (stream.DataAvailable)
        {
            string data = reader.ReadLine();
            if (data != null)
                OnIncomingData(data);
        }
    }

    public bool ConnectToServer (string host, int port)
    {
        if (socketReady)
            return socketReady;

        try
        {
            socket = new TcpClient(host, port);
            stream = socket.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            socketReady = true;
        }
        catch (Exception e)
        {
            Debug.Log("Client Socket Error: " +  e.Message);
        }

        return socketReady;
    }

    private void OnIncomingData(string data)
    {
        Debug.Log("Received : " + data);
        string[] aData = data.Split('|');

        switch (aData[0])
        {
            case "SWHO":
                for (int i = 1; i < aData.Length; i++)
                {
                    UserConnected(name, false);
                }
                Send("CWHO|" + clientName + "|" + (isHost ? 1 : 0).ToString());
                break;

            case "SCNN":
                UserConnected(aData[1], false);
                break;

            case "SMOV":
                int x1 = int.Parse(aData[1]);
                int y1 = int.Parse(aData[2]);
                int x2 = int.Parse(aData[3]);
                int y2 = int.Parse(aData[4]);
                CheckersBoard.Instance.TryMove(x1, y1, x2, y2);
                break;
        }
    }

    private void UserConnected (string name, bool isHost)
    {
        GameClient c = new GameClient();
        c.name = name;
        c.isHost = isHost;
        players.Add(c);

        if (players.Count == 2)
        {
            GameManager.Instance.StartGame();
        }
    }
    public void Send (string data)
    {
        if (!socketReady)
            return;

        writer.WriteLine(data);
        writer.Flush();
    }
    private void OnAppliactionQuit ()
    {
        CloseSocket();
    }
    private void OnDisable ()
    {
        CloseSocket();
    }
    private void CloseSocket ()
    {
        if (!socketReady)
            return;

        reader.Close();
        writer.Close();
        stream.Close();
        socketReady = false;
    }
}

public class GameClient
{
    public string name;
    public bool isHost;
}
