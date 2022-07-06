using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalArea : MonoBehaviourPun
{
    [SerializeField] private string team = "Red";
    [SerializeField] private string ballTag = "Ball";

    GameManager gm;

    private void Start()
    {
        gm = FindObjectOfType<GameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(ballTag))
        {
            if (PhotonNetwork.IsMasterClient)
                photonView.RPC("Goal", RpcTarget.All, photonView.ViewID);
        }
    }

    [PunRPC]
    private void Goal(int areaID)
    {
        if (PhotonNetwork.IsMasterClient && photonView.ViewID == areaID)
        {
            gm.SetGoal(team);
        }
    }
}
