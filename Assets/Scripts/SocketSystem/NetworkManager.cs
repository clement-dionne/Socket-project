using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class Player
{
    public Socket ClientSocket = null;
    public string IP = "";
    public string PlayerName = "";
    public bool IsReady = false;
    public GameObject ObjectInList = null;

    public bool AlreadyShow;
}

public class NetworkManager : MonoBehaviour
{
    #region VisualStudio Public
    public ClientTCP Client;
    public ServerTCP Server;

    public string PlayerName;
    public string ServerName;
    public string ServerIP;
    public int ServerPort = 4912;

    public Text Chat;
    public Text MessageInput;
    public InputField MessageInputField;

    public GameObject TextPrefab;
    public GameObject ChatContent;

    public int NumberOfPlayer = 1;

    public Text NumberOfPlayerText;
    public GameObject PlayersListObject;
    public GameObject PlayerListContent;
    public GameObject p;
    public GameObject ServerPanel;
    public GameObject GameSelector;

    public List<Player> Players = new List<Player>();

    public Player Self = new Player();

    public Color Ready;
    public Color NotReady;
    public Color Error;
    #endregion

    private int lastNumOfPly = 0;
    private int LastGameselectorValue = 0;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        MessageInput = GameObject.FindWithTag("MessageInput").GetComponent<Text>();
        MessageInputField = GameObject.FindWithTag("InputFieldMessage").GetComponent<InputField>();
        ChatContent = GameObject.FindWithTag("ChatContent");
    }

    public string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        Debug.LogError("cant got loacal ip address");
        return "127.0.0.1";
    }

    public void PrintToChat(string Data)
    {
        Chat = Instantiate(TextPrefab, ChatContent.transform).GetComponent<Text>();
        Chat.text = Data;
        Chat = null;
    }

    public void KickPlayer(Socket Client)
    {
        Server.SendToClientSocket(Client, "&&SERV_DISC");
        print("Kicked " + Server.AllClient[Client].Item1);
    }

    public void ResetListOfPlayer()
    {
        foreach (Player player in Players)
        {
            Destroy(player.ObjectInList);
        }
        Players.Clear();
    }

    public void Update()
    {
        if (NumberOfPlayer != lastNumOfPly)
        {
            lastNumOfPly = NumberOfPlayer;
            ResetListOfPlayer();
        }

        bool AlreadyInList = false;
        if (Server.isActiveAndEnabled)
        {
            NumberOfPlayer = Server.AllClient.Count + 1;
            print(NumberOfPlayer);

            foreach (Socket client in Server.AllClient.Keys)
            {
                foreach (Player c in Players)
                {
                    if (c.ClientSocket == client)
                    {
                        AlreadyInList = true;
                        if (Server.AllClient[client].Item3 != c.IsReady)
                        {
                            ResetListOfPlayer();
                            break;
                        }
                    }
                }

                if (!AlreadyInList)
                {
                    Player newPlayer = new Player();
                    string a = Server.AllClient[client].Item1.ToString();
                    newPlayer.PlayerName = a;
                    newPlayer.IsReady = Server.AllClient[client].Item3;
                    newPlayer.ClientSocket = client;
                    newPlayer.IP = newPlayer.ClientSocket.RemoteEndPoint.ToString().Split(':')[0];

                    GameObject p = Instantiate(PlayersListObject, PlayerListContent.transform);
                    if (!newPlayer.IsReady)
                    {
                        p.transform.GetChild(0).GetComponent<Image>().color = NotReady;
                    }
                    else
                    {
                        p.transform.GetChild(0).GetComponent<Image>().color = Ready;
                    }
                    p.GetComponent<Text>().text = a;
                    p.transform.GetChild(1).GetComponent<Button>().onClick.AddListener(() => KickPlayer(client));

                    newPlayer.ObjectInList = p;
                    newPlayer.AlreadyShow = true;

                    Players.Add(newPlayer);
                }
                AlreadyInList = false;
            }
        }

        AlreadyInList = false;
        if (Client.isActiveAndEnabled)
        {
            if (NumberOfPlayer != lastNumOfPly)
            {
                lastNumOfPly = NumberOfPlayer;
                ResetListOfPlayer();
            }

            foreach (string PlayerIP in Client.AllClient.Keys)
            {
                foreach (Player player in Players)
                {
                    if (player.IP == PlayerIP)
                    {
                        AlreadyInList = true;
                        if (Convert.ToBoolean(Client.AllClient[PlayerIP].Item2) != player.IsReady)
                        {
                            ResetListOfPlayer();
                            break;
                        }
                    }
                }

                if (!AlreadyInList)
                {
                    Player newPlayer = new Player();
                    newPlayer.PlayerName = Client.AllClient[PlayerIP].Item1;
                    newPlayer.IsReady = Convert.ToBoolean(Client.AllClient[PlayerIP].Item2);
                    newPlayer.ClientSocket = null;
                    newPlayer.IP = PlayerIP;

                    GameObject p = Instantiate(PlayersListObject, PlayerListContent.transform);
                    if (!newPlayer.IsReady)
                    {
                        p.transform.GetChild(0).GetComponent<Image>().color = NotReady;
                    }
                    else
                    {
                        p.transform.GetChild(0).GetComponent<Image>().color = Ready;
                    }
                    p.GetComponent<Text>().text = newPlayer.PlayerName;
                    p.transform.GetChild(1).GetComponent<Button>().interactable = false;

                    newPlayer.ObjectInList = p;
                    newPlayer.AlreadyShow = true;

                    Players.Add(newPlayer);
                }
                AlreadyInList = false;
            }
        }

        if (Input.GetKey(KeyCode.Return) && MessageInput.text != "")
        {
            if (Client.enabled)
            {
                Client.SendDataToServer(MessageInput.text);
            }
            if (Server.enabled)
            {
                Server.SendData(PlayerName + " : " + MessageInput.text);
                PrintToChat(PlayerName + " : " + MessageInput.text);
            }
            MessageInputField.text = "";
        }

        if (Client.enabled || Server.enabled)
        {
            if (ServerPanel == null)
            {
                ServerPanel = GameObject.FindGameObjectWithTag("ServerPanel");
            }

            if (GameSelector == null)
            {
                GameSelector = GameObject.FindGameObjectWithTag("GameSelector");
            }

            if (NumberOfPlayerText == null)
            {
                NumberOfPlayerText = GameObject.FindGameObjectWithTag("NumberOfPlayerText").GetComponent<Text>();
            }
            else NumberOfPlayerText.text = NumberOfPlayer.ToString();

            if (PlayerListContent == null)
            {
                PlayerListContent = GameObject.FindGameObjectWithTag("PlayerListContent");
            }

            if (!Self.AlreadyShow)
            {
                Self.PlayerName = PlayerName;
                Self.IsReady = GameObject.FindGameObjectWithTag("HUDConnectMenu").GetComponent<ConnectMenuManager>().IsReady;

                p = Instantiate(PlayersListObject, PlayerListContent.transform);
                if (!Self.IsReady)
                {
                    p.transform.GetChild(0).GetComponent<Image>().color = NotReady;
                }
                else
                {
                    p.transform.GetChild(0).GetComponent<Image>().color = Ready;
                }
                p.GetComponent<Text>().text = PlayerName;
                p.GetComponent<Text>().color = new Color(0f,0.6f,0f,255f);
                p.transform.GetChild(1).GetComponent<Button>().interactable = false;

                Self.ObjectInList = p;
                Self.AlreadyShow = true;
            }

            Self.IsReady = GameObject.FindGameObjectWithTag("HUDConnectMenu").GetComponent<ConnectMenuManager>().IsReady;
            if (!Self.IsReady)
            {
                p.transform.GetChild(0).GetComponent<Image>().color = NotReady;
            }
            else
            {
                p.transform.GetChild(0).GetComponent<Image>().color = Ready;
            }
        }

        if (MessageInput == null)
        {
            MessageInput = GameObject.FindWithTag("MessageInput").GetComponent<Text>();
        }

        if (ChatContent == null)
        {
            ChatContent = GameObject.FindWithTag("ChatContent");
        }

        if (MessageInputField == null)
        {
            MessageInputField = GameObject.FindWithTag("InputFieldMessage").GetComponent<InputField>();
        }

        if (Server.isActiveAndEnabled && ServerPanel != null)
        {
            ServerPanel.SetActive(true);
        }
        else if (Client.isActiveAndEnabled)
        {
            ServerPanel.SetActive(false);
        }

        if (GameSelector != null)
        {
            if (GameSelector.GetComponent<Dropdown>().value != LastGameselectorValue)
            {
                print("GameChange");
                LastGameselectorValue = GameSelector.GetComponent<Dropdown>().value;
            }
        }
    }
}