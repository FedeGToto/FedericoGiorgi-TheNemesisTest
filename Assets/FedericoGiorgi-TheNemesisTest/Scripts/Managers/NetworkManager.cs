using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Photon.Pun.UtilityScripts;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager instance;
    public static string Status;
    public static bool InMatchmaking;
    public static bool InGame;

    [SerializeField] private int sendRate;
    [SerializeField] private int serializationRate;

    [Space]
    [SerializeField] private string readyKey = "isReady";

    [Header("Gameplay")]
    [SerializeField] private string menuScene;
    [SerializeField] private string gameplayScene;

    [Header("Teams")]
    [SerializeField] private PhotonTeamsManager teamsManager;
    [SerializeField] private byte redTeamByteCode = 1;
    [SerializeField] private byte blueTeamByteCode = 2;

    #region Events
    public static UnityAction OnConnectedToServer;
    public static UnityAction OnConnectedToLobby;
    public static UnityAction OnRoomJoined;
    public static UnityAction OnRoomLeft;
    public static UnityAction<Player> OnPlayerDisconnected;
    public static UnityAction RestartMatchmaking;
    public static UnityAction PlayerTeamsUpdated;
    public static UnityAction<string> OnDisconnect;
    #endregion

    private List<Player> neutralPlayers = new List<Player>();
    private List<Player> redTeamPlayer = new List<Player>();
    private List<Player> blueTeamPlayer = new List<Player>();

    private bool isLoading;
    

    #region Unity Methods
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            PhotonTeamsManager.PlayerJoinedTeam += PlayerJoinedTeam;
            PhotonTeamsManager.PlayerLeftTeam += PlayerLeftTeam;

            PhotonNetwork.SerializationRate = serializationRate;
            PhotonNetwork.SendRate = sendRate;
        }
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }
    #endregion

    #region PUN Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster was called. Successfully connected to master server.");
        OnConnectedToServer?.Invoke();
        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby(TypedLobby.Default);
        Status = "Connecting to lobby...";
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("OnJoinedLobby called.");
        Status = "Connected.";
        OnConnectedToLobby?.Invoke();

        InGame = false;

        if (InMatchmaking)
        {
            RestartMatchmaking?.Invoke();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        OnDisconnect?.Invoke("Disconnected from the server.\nCause: " + cause.ToString());
        Debug.LogFormat("OnDisconnected was called. Disconnected with reason: {0}", cause);
    }

    public override void OnCreatedRoom()
    {
        PopulatePlayersLists();
        PlayerTeamsUpdated?.Invoke();
    }

    public override void OnJoinedRoom()
    {
        Status = "Joined a room";
        OnRoomJoined?.Invoke();
        Debug.Log("OnJoinedRoom called. Successfully joined room.");

        Hashtable hashtable = PhotonNetwork.LocalPlayer.CustomProperties;
        if (!hashtable.ContainsKey(readyKey))
        {
            hashtable.Add(readyKey, "False");
        }
        else
        {
            hashtable[readyKey] = "False";
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(hashtable);
        PhotonNetwork.LocalPlayer.LeaveCurrentTeam();

        PopulatePlayersLists();
        PlayerTeamsUpdated?.Invoke();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        PopulatePlayersLists();
        PlayerTeamsUpdated?.Invoke();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogFormat("{0} left the room.", otherPlayer.NickName);
        OnPlayerDisconnected?.Invoke(otherPlayer);

        PhotonNetwork.LeaveRoom();

        if (InGame)
        {

        }
        else
        {
            
            InMatchmaking = true;
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom called. Successfully left room.");

        OnRoomLeft?.Invoke();

        neutralPlayers.Clear();
        redTeamPlayer.Clear();
        blueTeamPlayer.Clear();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarningFormat("Failed joining room with error code {0}: {1}. Creating a room...", returnCode, message);

        string roomName = PhotonNetwork.NickName + "'s Room";

        RoomOptions ro = new RoomOptions();
        ro.MaxPlayers = 2;

        PhotonNetwork.CreateRoom(roomName, ro);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        // There was an error. Handle it
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        PopulatePlayersLists();
        PlayerTeamsUpdated?.Invoke();

        CheckStartPlay();
    }

    #endregion

    private void PlayerJoinedTeam(Player player, PhotonTeam photonTeam)
    {
        PopulatePlayersLists();

        CheckStartPlay();
        PlayerTeamsUpdated?.Invoke();

        Debug.LogFormat("{0} joined team {1}", player.NickName, photonTeam.Name);
    }
    private void PlayerLeftTeam(Player player, PhotonTeam photonTeam)
    {
        PopulatePlayersLists();

        CheckStartPlay();
        PlayerTeamsUpdated?.Invoke();

        Debug.LogFormat("{0} Left team {1}", player.NickName, photonTeam.Name);
    }

    #region Team API
    public static void JoinTeam(string teamName)
    {
        if (PhotonTeamsManager.Instance.GetTeamMembersCount(teamName) <= 0)
        {
            if (PhotonNetwork.LocalPlayer.GetPhotonTeam() != null)
            {
                PhotonNetwork.LocalPlayer.SwitchTeam(teamName);
            }
            else
            {
                PhotonNetwork.LocalPlayer.JoinTeam(teamName);
            }
        }
    }
    public static void LeaveTeam()
    {
        PhotonNetwork.LocalPlayer.LeaveCurrentTeam();
    }
    #endregion

    #region Static API
    public static bool ConnectToServer()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.NickName = "Player" + Random.Range(0, 10000).ToString("0000");
            PhotonNetwork.GameVersion = Application.version;
            PhotonNetwork.OfflineMode = false;
            Debug.Log("Connecting to the server...");
            PhotonNetwork.AutomaticallySyncScene = true;
            Status = "Connecting to master server...";
            PhotonNetwork.ConnectUsingSettings();

            return true;
        }
        else
        {
            Debug.LogWarning("Already connected to the server!");
            return false;
        }
    }

    public static bool DisconnectFromServer()
    {
        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
            return true;
        }
        else
        {
            Debug.Log("There was an issue disconnecting from the server");
            return false;
        }
    }
    #endregion

    private void PopulatePlayersLists()
    {
        // Get all players on Neutral
        neutralPlayers.Clear();
        foreach (var item in PhotonNetwork.CurrentRoom.Players)
        {
            neutralPlayers.Add(item.Value);
        }

        // Get players in Blue
        blueTeamPlayer.Clear();
        Player[] blueTeam;
        teamsManager.TryGetTeamMembers(blueTeamByteCode, out blueTeam);

        foreach (var item in blueTeam)
        {
            neutralPlayers.Remove(item);
            blueTeamPlayer.Add(item);
        }

        // Get players in Red
        redTeamPlayer.Clear();
        Player[] readTeam;
        teamsManager.TryGetTeamMembers(redTeamByteCode, out readTeam);

        foreach (var item in readTeam)
        {
            neutralPlayers.Remove(item);
            redTeamPlayer.Add(item);
        }
    }

    private void CheckStartPlay()
    {
        Debug.Log("Check Start Play");
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Is Master Client");
            if (neutralPlayers.Count <= 0 && (blueTeamPlayer.Count == 1 && redTeamPlayer.Count == 1))
            {
                Debug.Log("Player requirements meet");
                Dictionary<int, Player> players = PhotonNetwork.CurrentRoom.Players;
                bool areAllReady = true;

                foreach (var player in players)
                {
                    if ((string)(player.Value.CustomProperties[readyKey]) != "True")
                    {
                        areAllReady = false;
                        break;
                    }
                }

                if (areAllReady && !isLoading)
                {
                    InGame = true;
                    PhotonNetwork.LoadLevel(gameplayScene);
                    isLoading = true;
                }
            }
            
        }
    }

    public void EndGame()
    {

            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LeaveRoom();

            SceneManager.LoadScene(menuScene);

        InGame = false;
        isLoading = false;
    }

    #region Getters and Setters
    public Player[] GetNeutralPlayers()
    {
        return neutralPlayers.ToArray();
    }

    public Player[] GetRedPlayers()
    {
        return redTeamPlayer.ToArray();
    }

    public Player[] GetBluePlayers()
    {
        return blueTeamPlayer.ToArray();
    }

    public string GetReadyKey()
    {
        return readyKey;
    }

    public static Player GetLocalPlayer()
    {
        return PhotonNetwork.LocalPlayer;
    }

    public static PhotonView GetPlayer(int playerID)
    {
        return PhotonNetwork.GetPhotonView(playerID);
    }

    public static GameObject Instantiate(GameObject spawnObject, Vector3 position = default, Quaternion rotation = default, byte group = 0, object[] data = null)
    {
        return PhotonNetwork.Instantiate(spawnObject.name, position, rotation, group, data);
    }
    #endregion
}
