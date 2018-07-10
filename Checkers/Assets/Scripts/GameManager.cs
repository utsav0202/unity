using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { set; get; }
    public GameObject mainMenu;
    public GameObject serverMenu;
    public GameObject connectMenu;
    public InputField nameInput;

    public GameObject serverPrefab;
    public GameObject clientPrefab;

	private void Start ()
    {
        Instance = this;
        mainMenu.SetActive(true);
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);
        DontDestroyOnLoad(gameObject);
	}

    public void OnConnectButton()
    {
        Debug.Log("Connect");
        mainMenu.SetActive(false);
        connectMenu.SetActive(true);

    }

    public void OnHostButton ()
    {
        Debug.Log("Host");

        try
        {
            Server s = Instantiate(serverPrefab).GetComponent<Server>();
            s.Init();

            Client c = Instantiate(clientPrefab).GetComponent<Client>();
            c.clientName = nameInput.text;
            c.isHost = true;
            c.ConnectToServer("localhost", 6543);
        }
        catch (Exception e)
        {
            Debug.Log("OnHostButton: " + e.Message);
        }

        mainMenu.SetActive(false);
        serverMenu.SetActive(true);
    }

    public void OnConnectedToServerButton()
    {
        string hostAdderess = GameObject.Find("HostInput").GetComponent<InputField>().text;
        if (hostAdderess == "")
            hostAdderess = "localhost";

        try
        {
            Client c = Instantiate(clientPrefab).GetComponent<Client>();
            if (c.ConnectToServer(hostAdderess, 6543))
            {
                connectMenu.SetActive(false);
                c.clientName = nameInput.text;
            }
        }
        catch (Exception e)
        {
            Debug.Log("OnConnectedToServerButton: " + e.Message);
        }
    }

    public void OnBackButton ()
    {
        mainMenu.SetActive(true);
        serverMenu.SetActive(false);
        connectMenu.SetActive(false);

        Server s = FindObjectOfType<Server>();
        if (s != null)
            Destroy(s.gameObject);

        Client c = FindObjectOfType<Client>();
        if (c != null)
            Destroy(c.gameObject);
    }

    public void StartGame ()
    {
        SceneManager.LoadScene("Game");		
	}
}
