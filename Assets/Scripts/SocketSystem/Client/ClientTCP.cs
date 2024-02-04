using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientTCP : MonoBehaviour
{
    #region Unity Public
    public NetworkManager networkManager;
    public ExecuteOnMainThread MainThread;
    public Socket Client;

    public int ServerPort;
    public string ServerName;
    public bool EndStart;

    public Dictionary<string, Tuple<string, string>> AllClient = new Dictionary<string, Tuple<string, string>>();
    #endregion

    #region VisualStudio Private
    private Thread ClientThread;
    private Thread ClientListenerThread;
    private bool IsActive = false;
    #endregion

    void Start()
    {
        ServerPort = networkManager.ServerPort;
        ServerName = networkManager.ServerName;

        try
        {
            ClientThread = new Thread(() => ConnectToServer());
            ClientThread.IsBackground = true;
            ClientThread.Start();
            IsActive = true;
        }
        catch (Exception error)
        {
            Debug.LogError("Error : " + error.ToString());
        }
    }

    private void ConnectToServer()
    {
        ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { EndStart = false; });

        IPHostEntry host = Dns.GetHostEntry(networkManager.ServerIP);
        IPAddress ipAddress = host.AddressList[0];
        IPEndPoint ServerIP = new IPEndPoint(ipAddress, ServerPort);

        Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try { Client.Connect(ServerIP); }
        catch
        {
            try
            {
                Client.Connect(networkManager.ServerIP,ServerPort);
                SendDataToServer("&&PLY_NAME=" + networkManager.PlayerName);

                Byte[] bytes = new Byte[999999999];

                ClientListenerThread = new Thread(() => ListenForData(bytes));
                ClientListenerThread.IsBackground = true;
                ClientListenerThread.Start();

                Debug.Log("Connected");
            }
            catch (SocketException socketError)
            {
                Client.Close();
                Debug.LogError("Error : " + socketError.ToString());
            }
        }
    }

    public void ListenForData(Byte[] bytes)
    {
        string LastPrintedMess = "";

        SendDataToServer("&&PLY_NAME=" + networkManager.PlayerName);
        while (true)
        {
            try
            {
                int Incoming = Client.Receive(bytes);
                List<string> AllServerData = new List<string>();
                string ServerData = string.Empty;

                for (int i = 0; i < Incoming; i++)
                {
                    ServerData = ServerData + Convert.ToChar(bytes[i]);
                }

                foreach (string data in ServerData.Split('%'))
                {
                    AllServerData.Add(data);
                }
                print(ServerData);
                foreach (string ServerMessage in AllServerData)
                {
                    if (ServerMessage != "")
                    {
                        if (ServerMessage.Split('=')[0] == "&&SERVERNAME")
                        {
                            ServerName = ServerMessage.Split('=')[1];
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat(ServerName); });
                            Debug.Log(ServerMessage);
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.ServerName = ServerName; });
                            SendDataToServer("&&GET_PL_NUM");
                        }
                        else if (ServerMessage.Split('=')[0] == "&&NUM_OF_PL")
                        {
                            networkManager.NumberOfPlayer = Convert.ToInt32(ServerMessage.Split('=')[1]);
                            SendDataToServer("&&GET_OTHER_PLY");
                        }
                        else if (ServerMessage.Split('=')[0] == "&&ISREADY")
                        {
                            print(ServerMessage.Split('=')[1]);
                        }
                        else if (ServerMessage == "&&SERV_DISC")
                        {
                            SendDataToServer("&&DISCON");
                            Client.Close();
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { SceneManager.LoadScene(0); });
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { Destroy(networkManager.gameObject); });
                            break;
                        }
                        else if (ServerMessage.Split('=')[0] == "&&SERVISREADY")
                        {
                            string NewState = ServerMessage.Split('=')[1];

                            AllClient[networkManager.ServerIP] = Tuple.Create(AllClient[networkManager.ServerIP].Item1, NewState);
                        }
                        else if (ServerMessage.Split('=')[0] == "&&ALL_OTHER_PLY")
                        {
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => {
                                foreach (string otherPlayer in ServerMessage.Split('=')[1].Split(';'))
                                {
                                    if (AllClient.Count == 0)
                                    {
                                        print(otherPlayer);
                                        AllClient.Add(otherPlayer.Split('&')[1], Tuple.Create(otherPlayer.Split('&')[0], otherPlayer.Split('&')[2]));
                                    }
                                    foreach (string client in AllClient.Keys)
                                    {
                                        if (client != otherPlayer.Split('&')[1])
                                        {
                                            AllClient.Add(otherPlayer.Split('&')[1], Tuple.Create(otherPlayer.Split('&')[0], otherPlayer.Split('&')[2]));
                                        }
                                    }
                                }
                            });
                        }
                        else
                        {
                            if (ServerMessage != LastPrintedMess)
                            {
                                LastPrintedMess = ServerMessage;
                                ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat(ServerMessage); });
                            }
                        }
                        print(ServerMessage);
                    }
                }
                AllServerData.Clear();
            }
            catch (SocketException socketError)
            {
                print(socketError);
                Client.Close();
                break;
            }
        }
    }

    bool CheckConnectionState(Socket Server)
    {
        bool part1 = Server.Poll(1000, SelectMode.SelectRead);
        bool part2 = (Server.Available == 0);
        if (part1 && part2)
        {
            return false;
        }
        return true;
    }

    public void Update()
    {

    }

    public void KillAllThread()
    {
        ClientThread.Abort();
        ClientListenerThread.Abort();
    }

    public void SendDataToServer(string Data)
    {
        try
        {
            if (Data != "")
            {
                Client.Send(Encoding.ASCII.GetBytes("%" + Data));
            }
        }
        catch (SocketException socketError)
        {
            Client.Close();
            Debug.LogError("socketError : " + socketError.ToString());
        }
    }

    private void OnApplicationQuit()
    {
        if (IsActive)
        {
            SendDataToServer("&&DISCON");

            Client.Close();
            KillAllThread();
        }
    }
}