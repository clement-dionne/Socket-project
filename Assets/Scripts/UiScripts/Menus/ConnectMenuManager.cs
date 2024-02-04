using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConnectMenuManager : MonoBehaviour
{
    #region Unity Public
    public NetworkManager networkManager;
    public Text TitleText;
    public bool IsReady;
    #endregion

    public void Start()
    {
        networkManager = GameObject.FindWithTag("NetworkManager").gameObject.GetComponent<NetworkManager>();

        if (networkManager.ServerName != "")
        {
            TitleText.text = networkManager.ServerName;
            networkManager.Server.enabled = true;
        }
        else
        {
            TitleText.text = networkManager.ServerIP;
            networkManager.Client.enabled = true;
        }
    }

    public void Update()
    {
        if (networkManager.ServerName != "")
        {
            TitleText.text = networkManager.ServerName;
        }
    }

    public void ReadyButton()
    {
        if (IsReady) IsReady = false;
        else IsReady = true;

        if (networkManager.Client.isActiveAndEnabled) networkManager.Client.SendDataToServer("&&ISREADY=" + IsReady.ToString());
        else networkManager.Server.SendData("&&SERVISREADY=" + IsReady.ToString());
    }
}
