using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Photon.Pun.UtilityScripts;
using System;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("UI Menus")]
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject lobby;

    [Header("Buttons")]
    [SerializeField] private Button connectButton;

    [Header("Lobby")]
    [SerializeField] private Button joinRedButton;
    [SerializeField] private Button joinBlueButton;
    [SerializeField] private Button leaveTeamButton;
    [SerializeField] private Button readyButton;

    [Header("Player List")]
    [SerializeField] private GameObject playerName;
    [SerializeField] private Transform neutralPlayerNames;
    [SerializeField] private Transform redPlayerNames;
    [SerializeField] private Transform bluePlayerNames;

    #region Unity Methods
    private void Awake()
    {
        NetworkManager.OnConnectedToLobby += ConnectToLobby;
        NetworkManager.OnRoomJoined += OnRoomJoined;
        NetworkManager.PlayerTeamsUpdated += UpdatePlayerTeams;
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            ConnectToLobby();
        }
        else
        {

        }
    }

    private void Update()
    {
        statusText.text = NetworkManager.Status;
    }

    private void OnDisable()
    {
        NetworkManager.OnRoomJoined -= OnRoomJoined;
        NetworkManager.OnConnectedToLobby -= ConnectToLobby;
        NetworkManager.PlayerTeamsUpdated -= UpdatePlayerTeams;
    }
    #endregion

    #region Buttons
    public void ConnectButton()
    {
        connectButton.interactable = false;
        if (NetworkManager.ConnectToServer())
        {
            // Connected to the server
        }
        else
        {

        }
    }

    public void QuitGame()
    {
        if (NetworkManager.DisconnectFromServer())
        {
            Application.Quit();
        }
    }
    public void SearchGame()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    // Team management
    public void SetTeam(string teamName)
    {
        NetworkManager.JoinTeam(teamName);
    }
    public void LeaveTeam()
    {
        NetworkManager.LeaveTeam();
    }

    public void SetReady()
    {
        if (NetworkManager.GetLocalPlayer().GetPhotonTeam() != null)
        {
            Hashtable table = NetworkManager.GetLocalPlayer().CustomProperties;
            string readyKey = NetworkManager.instance.GetReadyKey();

            if (!table.ContainsKey(readyKey))
            {
                table.Add(readyKey, "True");
            }
            else
            {
                table[readyKey] = "True";
            }
            NetworkManager.GetLocalPlayer().SetCustomProperties(table);

            joinRedButton.gameObject.SetActive(false);
            joinBlueButton.gameObject.SetActive(false);
            leaveTeamButton.gameObject.SetActive(false);
            readyButton.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Callback Methods
    private void ConnectToLobby()
    {
        startScreen.SetActive(false);
        mainMenu.SetActive(true);
    }
    private void OnRoomJoined()
    {
        mainMenu.SetActive(false);
        lobby.SetActive(true);
    }
    #endregion

    private void UpdatePlayerTeams()
    {
        Player[] neutralPlayers = NetworkManager.instance.GetNeutralPlayers();
        Player[] redPlayers = NetworkManager.instance.GetRedPlayers();
        Player[] bluePlayers = NetworkManager.instance.GetBluePlayers();

        PopulatePlayerList(neutralPlayers, neutralPlayerNames);
        PopulatePlayerList(redPlayers, redPlayerNames);
        PopulatePlayerList(bluePlayers, bluePlayerNames);
    }

    private void PopulatePlayerList(Player[] players, Transform playerList)
    {
        for (int i = 0; i < playerList.childCount; i++)
        {
            Destroy(playerList.GetChild(i).gameObject);
        }

        foreach (var item in players)
        {
            GameObject player = Instantiate(playerName, playerList);
            player.GetComponent<TextMeshProUGUI>().text = item.NickName;
        }
    }
}
