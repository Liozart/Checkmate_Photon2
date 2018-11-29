using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerManager : MonoBehaviourPunCallbacks
{
    public static int chosenGameMode;
    public static int chosenCharacter;
    public static string chosenName;
    public bool isTeamBlack;

    public GameObject[] playerPrefabsWhite;
    public GameObject[] playerPrefabsBlack;
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    public int playersNumber;
    public string[] teamWhite;
    public string[] teamBlack;

    public byte updateTeamsEvenCode = 0;

    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        teamWhite = new string[6];
        teamBlack = new string[6];
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    //Call server for a team
    public void Start()
    {
        PhotonView photonView = PhotonView.Get(this);
        photonView.RPC("GetTeam", RpcTarget.MasterClient, chosenName);
    }

    //Server set a team for the player and send updates
    [PunRPC]
    public void GetTeam(string name, PhotonMessageInfo info)
    {
        PhotonView photonView = PhotonView.Get(this);
        playersNumber++;
        if ((playersNumber % 2) == 1)
        {
            photonView.RPC("AddToTeam", info.Sender, true);
            for (int i = 0; i < 6; i++)
                if (string.IsNullOrEmpty(teamBlack[i]))
                {
                    teamBlack[i] = name;
                    i = 6;
                }
        }
        else
        {
            photonView.RPC("AddToTeam", info.Sender, false);
            for (int i = 0; i < 6; i++)
                if (string.IsNullOrEmpty(teamWhite[i]))
                {
                    teamWhite[i] = name;
                    i = 6;
                }
        }
        SendUpdateTeamsEvent();
    }

    public void SendUpdateTeamsEvent()
    {
        for (int i = 0; i < 6; i++)
            Debug.Log("white: " + teamWhite[i]);

        for (int i = 0; i < 6; i++)
            Debug.Log("black: " + teamBlack[i]);

        object[] content = new object[] { playersNumber,
            teamWhite[0], teamWhite[1], teamWhite[2],
            teamWhite[3], teamWhite[4], teamWhite[5],
            teamBlack[0], teamBlack[1], teamBlack[2],
            teamBlack[3], teamBlack[4], teamBlack[5]};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(updateTeamsEvenCode, content, raiseEventOptions, sendOptions);
    }

    //Recieve teams and player number updates
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == updateTeamsEvenCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            playersNumber = (int)data[0];
            for (int i = 0; i < 6; i++)
                teamWhite[i] = (string)data[1 + i];
            for (int i = 0; i < 6; i++)
                teamBlack[i] = (string)data[7 + i];

            for (int i = 0; i < 6; i++)
                Debug.Log("white: " + teamWhite[i]);

            for (int i = 0; i < 6; i++)
                Debug.Log("black: " + teamBlack[i]);
        }
    }

    //Recieve a team from the server
    [PunRPC]
    public void AddToTeam(bool black)
    {
        isTeamBlack = black;

        //Finally instancing the player
        if (PlayerSystem.LocalPlayerInstance == null)
        {
            if (isTeamBlack)
                PhotonNetwork.Instantiate(playerPrefabsBlack[chosenCharacter].name, spawnPointTeamBlack.transform.position, spawnPointTeamBlack.transform.rotation, 0);
            else
                PhotonNetwork.Instantiate(playerPrefabsWhite[chosenCharacter].name, spawnPointTeamWhite.transform.position, spawnPointTeamWhite.transform.rotation, 0);
        }
        else
            Debug.Log("Ignoring scene load for player");
    }

    /*
     * Back Button action in game scene
     */
    public void OnClickBackButton()
    {
        PhotonNetwork.Disconnect();
        SceneManager.LoadScene("SelectionScene");
    }
}
