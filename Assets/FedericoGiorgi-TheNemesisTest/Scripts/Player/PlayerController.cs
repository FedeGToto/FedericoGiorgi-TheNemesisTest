using Photon.Pun;
using Rewired;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviourPun
{
    [Header("Stats")]
    [SerializeField] private float moveSpeed = 2;

    [Header("Input")]
    [SerializeField] private string horizontalMoveAxis;
    [SerializeField] private string verticalMoveAxis;

    private Rigidbody rb;
    private Player playerControls;
    private float horizontalMove;
    private float verticalMove;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerControls = ReInput.players.GetPlayer(0);
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            GetInput();
            Move();
        }
        else
        {
            // It's not my player, do nothing
        }
    }

    private void GetInput()
    {
        horizontalMove = playerControls.GetAxis(horizontalMoveAxis);
        verticalMove = playerControls.GetAxis(verticalMoveAxis);
    }

    private void Move()
    {
        var x = horizontalMove;
        var z = verticalMove;

        Vector3 moveDirection = new(x, 0, z);

        if (moveDirection.sqrMagnitude > 0.0f)
            transform.rotation = Quaternion.LookRotation(moveDirection);

        rb.velocity = moveDirection.normalized * moveSpeed;
    }
}
