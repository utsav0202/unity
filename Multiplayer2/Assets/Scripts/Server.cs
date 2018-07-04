using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerClient
{
    public string name;
    public int connId;
}

public class Server : MonoBehaviour {

    private const int MAX_CONNECTION = 100;
    private int port = 5701;

    private int hostId;
    private int webHostId;

    private int reliableChannel;
    private int unreliableChannel;

    private bool isStarted = false;
    private byte error;

    private List<ServerClient> clients = new List<ServerClient>();

	// Use this for initialization
	void Start ()
    {
        Debug.Log("Server : Start");
        NetworkTransport.Init();
        ConnectionConfig cc = new ConnectionConfig();

        reliableChannel = cc.AddChannel(QosType.Reliable);
        unreliableChannel = cc.AddChannel(QosType.Unreliable);

        HostTopology topo = new HostTopology(cc, MAX_CONNECTION);
        hostId = NetworkTransport.AddHost(topo, port, null);
        webHostId = NetworkTransport.AddWebsocketHost(topo, port, null);

        isStarted = true;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (!isStarted)
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
            case NetworkEventType.ConnectEvent:
                Debug.Log("Player connectionId " + connectionId + " has connected");
                OnConnection(connectionId);
                break;
            case NetworkEventType.DataEvent:
                string msg = Encoding.Unicode.GetString(recBuffer, 0, dataSize);
                Debug.Log("Receiving from " + connectionId + " : " + msg);
                OnData(msg, connectionId);
                break;
            case NetworkEventType.DisconnectEvent:
                Debug.Log("Player connectionId " + connectionId + " has disconnected");
                break;

            case NetworkEventType.BroadcastEvent:
                break;
        }
    }

    private void OnData (string msg, int connId)
    {
        string[] data = msg.Split('|');

        switch (data[0])
        {
            case "NAMEIS":
                OnNameIs(data[1], connId);
                break;

            default:
                break;
        }
    }

    private void OnConnection (int connId)
    {
        ServerClient sc = new ServerClient();
        sc.name = "TEMP";
        sc.connId = connId;
        clients.Add(sc);

        string message = "ASKNAME|" + connId;
        foreach (ServerClient c in clients)
        {
            message += "|" + c.name + "%" + c.connId;
        }

        Send(message, reliableChannel, connId);
    }

    private void OnNameIs (string name, int connId)
    {
        // save name
        clients.Find(x => x.connId == connId).name = name;

        // inform all
        Send("CNN|" + name + "|" + connId, reliableChannel, clients);
    }

    private void Send (string msg, int channel, int connId)
    {
        List<ServerClient> c = new List<ServerClient>();
        c.Add(clients.Find(x => x.connId == connId));
        Send(msg, channel, c);
    }

    private void Send(string msg, int channel, List<ServerClient> cs)
    {
        Debug.Log("Sending : " + msg);
        byte[] message = Encoding.Unicode.GetBytes(msg);

        foreach (ServerClient sc in cs)
        {
            NetworkTransport.Send(hostId, sc.connId, channel, message, msg.Length * sizeof(char), out error);
        }
    }
}
