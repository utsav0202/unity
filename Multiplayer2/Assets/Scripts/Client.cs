using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Player
{
    public string name;
    public int connId;
    public GameObject avatar;
}

public class Client : MonoBehaviour {

    private const int MAX_CONNECTION = 100;
    private int port = 5701;

    private int hostId;
    private int connectionId;

    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private bool isConnected = false;
    private byte error;

    private float connectTime;
    private string playerName;
    private int myConnId;

    public GameObject playerPrefab;

    private Dictionary<int, Player> players = new Dictionary<int, Player>();
        
	// Update is called once per frame
	void Update ()  
    {
        if (!isConnected)
            return;

        int recHostId;
        int connectionId;
        int channelId;
        byte[] recBuffer = new byte[1024];
        int bufferSize = 1024;
        int dataSize;
        byte error;
        NetworkEventType recData = NetworkTransport.Receive(out recHostId,
                                                            out connectionId,
                                                            out channelId,
                                                            recBuffer,
                                                            bufferSize,
                                                            out dataSize,
                                                            out error);
        switch (recData)
        {
            case NetworkEventType.DataEvent:
                string message = Encoding.Unicode.GetString(recBuffer);
                Debug.Log("Receiving : " + message);
                OnData(message);
                break;

            case NetworkEventType.BroadcastEvent:

                break;
        }
    }

    private void OnData(string message)
    {
        string[] data = message.Split('|');

        switch (data[0])
        {
            case "ASKNAME":
                OnAskName(data);
                break;

            case "CNN":
                SpawnPlayer(data[1], int.Parse(data[2]));
                break;

            case "DCN":
                RemovePlayer(int.Parse(data[1]));
                break;

            case "ASKPOSITION":
                OnAskPosition(data);
                break;

            default:
                Debug.Log("Unknown message: " + data[0]);
                break;
        }
    }

    public void Connect ()
    {
        string pName = GameObject.Find("NameInput").GetComponent<InputField>().text;
        if (pName == "")
        {
            Debug.Log("You must enter a name");
            return;
        }

        playerName = pName;
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
        hostId = NetworkTransport.AddHost(topo, 0);
        connectionId = NetworkTransport.Connect(hostId, "127.0.0.1", port, 0, out error);

        connectTime = Time.time;

        isConnected = true;
    }

    private void OnAskName(string[] data)
    {
        myConnId = int.Parse(data[1]);

        Send("NAMEIS|" + playerName, reliableChannel);

        for (int i = 2; i < data.Length; i++)
        {
            string[] d = data[i].Split('%');
            SpawnPlayer(d[0], int.Parse(d[1]));
        }
    }

    private void OnAskPosition (string [] data)
    {
        for (int i = 1; i < data.Length; i++)
        {
            string[] d = data[i].Split('%');
            int connId = int.Parse(d[0]);

            if (connId != myConnId)
            {
                Vector3 pos = new Vector3();
                pos.x = float.Parse(d[1]);
                pos.y = float.Parse(d[2]);
                players[connId].avatar.GetComponent<Transform>().position = pos;
            }
        }

        Vector3 p = players[myConnId].avatar.GetComponent<Transform>().position;
        string msg = "MYPOSITION|" + p.x.ToString() + "|" + p.y.ToString();
        Send(msg, unreliableChannel);
    }

    private void SpawnPlayer (string name,int connId)
    {
        GameObject go = Instantiate(playerPrefab) as GameObject;

        if (connId == myConnId)
        {
            go.AddComponent<PlayerMotor>();
            GameObject.Find("Canvas").SetActive(false);
            isStarted = true;
        }        

        Player p = new Player();
        p.avatar = go;
        p.name = name;
        p.connId = connId;
        p.avatar.GetComponentInChildren<TextMesh>().text = name;
        players.Add(connId, p);
    }

    private void RemovePlayer(int connId)
    {
        Destroy(players[connId].avatar);
        players.Remove(connId);
    }

    private void Send(string msg, int channel)
    {
        Debug.Log("Sending : " + msg);
        byte[] message = Encoding.Unicode.GetBytes(msg);

        NetworkTransport.Send(hostId, connectionId, reliableChannel, message, msg.Length * sizeof(char), out error);
    }
}
