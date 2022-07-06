using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun.UtilityScripts;
using TMPro;
using Photon.Realtime;

public class GameManager : MonoBehaviourPun
{
    [Header("Spawnable Prefabs")]
    [SerializeField] private GameObject redPlayerPrefab;
    [SerializeField] private GameObject bluePlayerPrefab;
    [SerializeField] private GameObject ballPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform redPlayerSpawnPoint;
    [SerializeField] private Transform bluePlayerSpawnPoint;
    [SerializeField] private Transform ballSpawnPoint;

    [Header("Goal Area")]
    [SerializeField] private Vector2 minPoint;
    [SerializeField] private Vector2 maxPoint;
    [SerializeField] private GameObject redGoalAreaPrefab;
    [SerializeField] private GameObject blueGoalAreaPrefab;

    [Header("Game")]
    [SerializeField] private int maxPoints = 3;

    [Header("UI")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private TextMeshProUGUI winnerText;

    private int redTeamPoints = 0;
    private int blueTeamPoints = 0;
    private bool canScorePoints;
    private List<PhotonView> playerViews = new List<PhotonView>();
    private GameObject ball;

    private string localPlayerTeam;
    private bool gameEnded;


    #region Unity Methods
    private void Awake()
    {
        NetworkManager.OnRoomLeft += OnLeftRoom;
        NetworkManager.OnPlayerDisconnected += OnPlayerDisconnected;

    }

    private void Start()
    {
        NetworkManager.InGame = true;

        GameObject spawnObject = NetworkManager.GetLocalPlayer().GetPhotonTeam().Name == "Red" 
            ? redPlayerPrefab : bluePlayerPrefab;
        GameObject player = NetworkManager.Instantiate(spawnObject);
        photonView.RPC("SpawnPlayer", RpcTarget.All, player.GetPhotonView().ViewID);
        localPlayerTeam = NetworkManager.GetLocalPlayer().GetPhotonTeam().Name;

        if (PhotonNetwork.IsMasterClient)
        {
            SpawnArea(redGoalAreaPrefab);
            SpawnArea(blueGoalAreaPrefab);

            ball = NetworkManager.Instantiate(ballPrefab, ballSpawnPoint.position, ballSpawnPoint.rotation);
            
        }
    }

    private void OnDisable()
    {
        NetworkManager.OnRoomLeft -= OnLeftRoom;
        NetworkManager.OnPlayerDisconnected -= OnPlayerDisconnected;
    }
    #endregion

    #region PUN Callbacks
    private void OnPlayerDisconnected(Player player)
    {
        string winningTeam = "";

        if (player.GetPhotonTeam().Name == "Red")
            winningTeam = "Blue";
        else if (player.GetPhotonTeam().Name == "Blue")
            winningTeam = "Red";

        for (int i = 0; i < 3; i++)
        {
            SetGoal(winningTeam);
        }
    }

    private void OnLeftRoom()
    {
        SetWinner(localPlayerTeam);
    }
    #endregion

    private void SpawnArea(GameObject areaPrefab)
    {
        float randX = Random.Range(minPoint.x, maxPoint.x);
        float randZ = Random.Range(minPoint.y, maxPoint.y);

        Vector3 position = new(randX, 0, randZ);
        GameObject goalArea = NetworkManager.Instantiate(areaPrefab, position);
    }

    public void SetGoal(string team)
    {
        if (canScorePoints)
        {
            canScorePoints = false;
            if (team == "Red")
            {
                redTeamPoints++;
            }
            else
            {
                blueTeamPoints++;
            }

            CheckEndGame();
        }
    }

    private void CheckEndGame()
    {
        if (redTeamPoints >= maxPoints)
        {
            photonView.RPC("SetWinner", RpcTarget.All, "Red");
        }
        else if (blueTeamPoints >= maxPoints)
        {
            photonView.RPC("SetWinner", RpcTarget.All, "Blue");
        }
        else
        {
            foreach (var item in playerViews)
            {
                photonView.RPC("ResetPlayerPosition", RpcTarget.All, item.ViewID);
            }
        }
    }

    #region Buttons
    public void GoToMenu()
    {
        NetworkManager.InMatchmaking = false;
        NetworkManager.instance.EndGame();
    }

    public void StartNewGame()
    {
        NetworkManager.InMatchmaking = true;
        NetworkManager.instance.EndGame();
    }
    #endregion

    #region PUN RPCs

    [PunRPC]
    public void SpawnPlayer(int playerId)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            playerViews.Add(NetworkManager.GetPlayer(playerId));
        }
        photonView.RPC("ResetPlayerPosition", RpcTarget.All, playerId);
    }

    [PunRPC]
    public void ResetPlayerPosition(int playerID)
    {
        PhotonView player = NetworkManager.GetPlayer(playerID);
        Transform spawnPoint = player.Owner.GetPhotonTeam().Name == "Red" ? redPlayerSpawnPoint : bluePlayerSpawnPoint;
        GameObject playerObject = player.gameObject;
        playerObject.transform.position = spawnPoint.position;
        canScorePoints = true;
    }

    [PunRPC]
    public void ResetBall()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ball.transform.position = ballSpawnPoint.transform.position;
        }
    }

    [PunRPC]
    private void SetWinner(string winningTeam)
    {
        if (!gameEnded)
        {
            winUI.SetActive(true);
            winnerText.text = $"{winningTeam} win!";
            gameEnded = true;
        }
    }
    #endregion
}
