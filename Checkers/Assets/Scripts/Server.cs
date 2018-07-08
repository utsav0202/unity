using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server : MonoBehaviour
{
    public int port = 6543;

    private List<ServerClient> clients;
    private List<ServerClient> disconnectedClients;

    private TcpListener server;
    private bool isServerStarted;

    public void Init ()
    {
        DontDestroyOnLoad(gameObject);
        clients = new List<ServerClient>();
        disconnectedClients = new List<ServerClient>();

        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            StartListening();
            isServerStarted = true;
        }
        catch (Exception e)
        {
            Debug.Log("Socket error : " + e.Message);
        }
    }

    private void StartListening()
    {
        server.BeginAcceptTcpClient(AcceptTcpClient, server);
    }

    private void AcceptTcpClient(IAsyncResult ar)
    {
        TcpListener listener = (TcpListener)ar.AsyncState;

        ServerClient client = new ServerClient(listener.EndAcceptTcpClient(ar));
        clients.Add(client);

        StartListening();

        Debug.Log("Somebody has connected");
    }

    private void Update ()
    {
        if (!isServerStarted)
            return;

        foreach (ServerClient c in clients)
        {
            if (!IsConnected (c.tcp))
            {
                c.tcp.Close();
                disconnectedClients.Add(c);
                continue;
            }
            else
            {
                NetworkStream s = c.tcp.GetStream();
                if (s.DataAvailable)
                {
                    StreamReader reader = new StreamReader(s);
                    string data = reader.ReadLine();

                    if (data != null)
                    {
                        OnIncomingData(c, data);
                    }
                }
            }
        }

        for (int i = 0; i < disconnectedClients.Count; i++)
        {
            // tell al somebody has disconnected 
            clients.Remove(disconnectedClients[i]);
            disconnectedClients.RemoveAt(i);
        }
		
	}

    private bool IsConnected (TcpClient c)
    {
        try
        {
            if (c != null && c.Client != null && c.Client.Connected)
            {
                if (c.Client.Poll(0, SelectMode.SelectRead))
                    return !(c.Client.Receive(new byte[1], SocketFlags.Peek) == 0);

                return true;
            }
            else
                return false;
        }
        catch
        {
            return false;
        }
    }
    private void OnIncomingData (ServerClient c, string data)
    {
        Debug.Log(c.clientName + " : " + data);
    }
}

public class ServerClient
{
    public string clientName;
    public TcpClient tcp;

    public ServerClient (TcpClient tcp)
    {
        this.tcp = tcp;
    }
}