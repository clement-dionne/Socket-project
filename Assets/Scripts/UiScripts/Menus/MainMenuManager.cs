using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    #region Unity Public
    public NetworkManager networkManager;

    public GameObject SettingsPanel;
    public GameObject MainPanel;

    public GameObject NetworkPanel;
    public GameObject ConnectPanel;
    public GameObject CreateServerPanel;
    public GameObject ChoicePanel;

    public Button NextButton;
    public Text NameInput;
    public Text ServerName;
    public Text ServerIP;
    #endregion

    private bool join;
    void Start()
    {
        SettingsPanel.SetActive(false);
        MainPanel.SetActive(true);
        NetworkPanel.SetActive(false);
        ConnectPanel.SetActive(false);
        CreateServerPanel.SetActive(false);
        ChoicePanel.SetActive(false);
    }

    void Update()
    {
        if (NameInput.text == "" || NameInput.text == null)
        {
            NextButton.interactable = false;
        }
        else
        {
            NextButton.interactable = true;
        }
    }

    public void SettingsOpen(bool Open)
    {
        SettingsPanel.SetActive(Open);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Next()
    {
        MainPanel.SetActive(false);
        NetworkPanel.SetActive(true);
        ConnectPanel.SetActive(false);
        CreateServerPanel.SetActive(false);
        ChoicePanel.SetActive(true);
        networkManager.PlayerName = NameInput.text;
    }

    public void Host()
    {
        ConnectPanel.SetActive(false);
        CreateServerPanel.SetActive(true);
        ChoicePanel.SetActive(false);
        networkManager.ServerIP = networkManager.GetLocalIpAddress();
        networkManager.ServerName = ServerName.text;
    }

    public void Join()
    {
        ConnectPanel.SetActive(true);
        CreateServerPanel.SetActive(false);
        ChoicePanel.SetActive(false);
        networkManager.ServerName = "";
        join = true;
    }

    public void Back()
    {
        MainPanel.SetActive(true);
        NetworkPanel.SetActive(false);
        ConnectPanel.SetActive(false);
        CreateServerPanel.SetActive(false);
        ChoicePanel.SetActive(false);
    }

    public void Connect(string SceneName)
    {
        SceneManager.LoadScene(SceneName);
        networkManager.ServerName = ServerName.text;
        if (join)
        {
            networkManager.ServerIP = ServerIP.text;
        }
    }
}
