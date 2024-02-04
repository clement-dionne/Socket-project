using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using UnityEngine;
using System.Text;

public class ServerTCP : MonoBehaviour
{
    #region Unity Public
    public NetworkManager networkManager;
    public ExecuteOnMainThread MainThread;

    public int ServerPort;
    public string ServerName;
    public IPEndPoint ServerIP;

    public Dictionary<Socket, Tuple<string,Thread,bool>> AllClient = new Dictionary<Socket, Tuple<string, Thread, bool>>();
    #endregion

    #region VisualStudio Private
    private Socket Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private Thread ServerStart;
    private int LastNumberOfPlayer = 0;
    #endregion

    void Start()
    {
        ServerPort = networkManager.ServerPort;
        ServerName = networkManager.ServerName;

        bool error = false;
        try
        {
            IPHostEntry host = Dns.GetHostEntry(networkManager.GetLocalIpAddress());
            ServerIP = new IPEndPoint(IPAddress.Parse(networkManager.GetLocalIpAddress()), ServerPort);

            Debug.Log(ServerIP);

            Server.Bind(ServerIP);
            Server.Listen(ServerPort);

            Debug.Log("Server Listening on Port : " + ServerPort.ToString());
        }

        catch (Exception Error)
        {
            Debug.LogError("Error : " + Error.ToString());
            networkManager.PrintToChat("Error:" + Error.ToString());
            error = true;
        }

        if (!error)
        {
            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat("Server Listening on Port : " + ServerPort.ToString() + " IP : " + ServerIP.ToString().Split(':')[0]); });
            ServerStart = new Thread(() => WaitForClient());
            ServerStart.IsBackground = true;
            ServerStart.Start();
        }
    }

    private void Update()
    {
        if (networkManager.NumberOfPlayer != LastNumberOfPlayer)
        {
            LastNumberOfPlayer = networkManager.NumberOfPlayer;
            BrodCastSendToAllClient("&&NUM_OF_PL=" + networkManager.NumberOfPlayer.ToString());
        }
        //foreach (Socket clientSocket in AllClient.Keys)
        //{
        //    if (!CheckConnectionState(clientSocket))
        //    {
        //        DisconectClient(clientSocket);
        //    }
        //}
    }

    private void WaitForClient()
    {
        while (true)
        {
            try
            {
                Byte[] bytes = new Byte[999999999];
                Socket Client = Server.Accept();
                Debug.Log("New Client : " + ((IPEndPoint) Client.RemoteEndPoint).Address.ToString());

                // ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { AllClient.Add(Client,null); });

                Thread NewClient = new Thread(() => ReadClientMesssages(Client, bytes));
                NewClient.IsBackground = true;
                NewClient.Start();
            }
            catch (SocketException socketError)
            {
                Debug.LogError("socketError : " + socketError.ToString());
            }
        }
    }

    public void ReadClientMesssages(Socket Client, Byte[] bytes)
    {
        string LastprintedMess = "";

        string PlayerName = "";
        while (true)
        {
            try
            {
                int Incoming = Client.Receive(bytes);
                List<string> AllClientData = new List<string>();
                string ClientData = string.Empty;

                for (int i = 0; i < Incoming; i++)
                {
                    ClientData = ClientData + Convert.ToChar(bytes[i]);
                }
                print(ClientData);
                foreach (string data in ClientData.Split('%'))
                {
                    AllClientData.Add(data);
                }

                foreach (string ClientMessage in AllClientData)
                {
                    if (ClientMessage != "")
                    {
                        if (ClientMessage.Split('=')[0] == "&&PLY_NAME")
                        {
                            PlayerName = ClientMessage.Split('=')[1];
                            AllClient[Client] = Tuple.Create(PlayerName, Thread.CurrentThread, false);
                            Debug.Log(ClientMessage + "Join The Server");
                            ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat(PlayerName + " Join The Server"); });

                            BrodCastSendToAllClient(PlayerName + " Join The Server");
                            SendToClientSocket(Client, "&&SERVERNAME=" + ServerName);
                        }
                        else if (ClientMessage.Split('=')[0] == "&&ISREADY")
                        {
                            bool IsReady = Convert.ToBoolean(ClientMessage.Split('=')[1]);
                            AllClient[Client] = Tuple.Create(AllClient[Client].Item1, AllClient[Client].Item2, IsReady);
                            // BrodCastSendToAllClient("&&CLIENTCHANGEISREADYSTATE" + "=" + Client.RemoteEndPoint.ToString().Split(':')[0].ToString() + ";" + IsReady.ToString());
                            string allClient = networkManager.PlayerName + "&" + Client.RemoteEndPoint.ToString().Split(':')[0] + "&" + IsReady.ToString();
                            foreach (Socket client in AllClient.Keys)
                            {
                                if (client != Client)
                                {
                                    allClient += ";" + AllClient[client].Item1.ToString() + "&" + client.RemoteEndPoint.ToString().Split(':')[0] + "&" + AllClient[client].Item3.ToString();
                                }
                            }
                            BrodCastSendToAllClient("&&ALL_OTHER_PLY=" + allClient);
                        }
                        else if (ClientMessage == "&&DISCON")
                        {
                            DisconectClient(Client);
                            break;
                        }
                        else if (ClientMessage == "&&GET_PL_NUM")
                        {
                            BrodCastSendToAllClient("&&NUM_OF_PL=" + networkManager.NumberOfPlayer.ToString());
                        }
                        else if (ClientMessage == "&&GET_OTHER_PLY")
                        {
                            string allClient = networkManager.PlayerName + "&" + Client.RemoteEndPoint.ToString().Split(':')[0] + "&" + AllClient[Client].Item3.ToString();
                            foreach (Socket client in AllClient.Keys)
                            {
                                if (client != Client)
                                {
                                    allClient += ";" + AllClient[client].Item1.ToString() + "&" + client.RemoteEndPoint.ToString().Split(':')[0] + "&" + AllClient[client].Item3.ToString();
                                }
                            }
                            BrodCastSendToAllClient("&&ALL_OTHER_PLY=" + allClient);
                        }
                        else
                        {
                            if (LastprintedMess != ClientMessage)
                            {
                                LastprintedMess = ClientMessage;
                                ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat(PlayerName + " : " + ClientMessage); });
                            }
                            BrodCastSendToAllClient(PlayerName + " : " + ClientMessage);
                        }
                        print(PlayerName + " : " + ClientMessage);
                    }
                }
                AllClientData.Clear();
            }
            catch (Exception error)
            {
                Debug.LogError("Error : " + error.ToString());
                DisconectClient(Client);
                break;
            }
        }
    }

    bool CheckConnectionState(Socket client)
    {
        bool part1 = client.Poll(1000, SelectMode.SelectRead);
        bool part2 = (client.Available == 0);
        if (part1 && part2)
        {
            return false;
        }
        return true;
    }

    public void KillAllThread()
    {
        ServerStart.Abort();
        foreach (Socket clientSocket in AllClient.Keys)
        {
            AllClient[clientSocket].Item2.Abort();
        }
    }

    public void EndServer()
    {
        ExecuteOnMainThread.RunOnMainThread.Enqueue(() =>
        {
            foreach (Socket clientSocket in AllClient.Keys)
            {
                DisconectClient(clientSocket);
            }
        });

        Server.Close();
    }

    public void OnApplicationQuit()
    {
        BrodCastSendToAllClient("&&SERV_DISC");
        EndServer();
        KillAllThread();
    }

    public void SendData(string DataToSend)
    {
        LaMemeChose(DataToSend);
    }

    public void BrodCastSendToAllClient(string DataToSend)
    {
        LaMemeChose(DataToSend);
    }

    public void SendToClientSocket(Socket Client, string DataToSend)
    {
        try
        {
            if (DataToSend != "")
            {
                Client.Send(Encoding.ASCII.GetBytes("%" + DataToSend));
            }
        }
        catch (SocketException socketError)
        {
            Debug.LogError("socketError : " + socketError.ToString());
            DisconectClient(Client);
        }
    }

    private void LaMemeChose(string DataToSend)
    {
        try
        {
            foreach (Socket Client in AllClient.Keys)
            {
                try
                {
                    if (DataToSend != "")
                    {
                        Client.Send(Encoding.ASCII.GetBytes("%" + DataToSend));
                    }
                }
                catch (SocketException socketError)
                {
                    Debug.LogError("socketError : " + socketError.ToString());
                    DisconectClient(Client);
                }
            }
        }
        catch (Exception error)
        {
            Debug.LogError("Error : " + error.ToString());
        }
    }

    public void DisconectClient(Socket clientSocket)
    {
        string n = AllClient[clientSocket].Item1.ToString();
        foreach (Socket c in AllClient.Keys)
        {
            if (c != clientSocket)
            {
                SendToClientSocket(c, n + " Left The Server");
            }
        }
        ExecuteOnMainThread.RunOnMainThread.Enqueue(() => { networkManager.PrintToChat(n + " Left The Server"); });

        clientSocket.Close();
        AllClient.Remove(clientSocket);
    }
}