// ----------------------------------------------------------------------------  
// PlayerManager.cs  
// <summary>  
// Client/Server player management script : Assign team and player spawning with RPCs
// and update teams lists with an event
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class PlayerManager : MonoBehaviourPunCallbacks
{

    //Host selected gamemode
    public static int chosenGameMode;
    //Selected character
    public static int chosenCharacter;
    public static string chosenName;
    //Is this local player in black team
    public bool isTeamBlack;

    //0 : king, 1 : queen, 2 : pawn
    public GameObject[] playerPrefabsWhite;
    public GameObject[] playerPrefabsBlack;

    //Respawn points position
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    public int playersNumber;
    // teams lists
    public string[] teamWhite;
    public string[] teamBlack;

    // EVENTS CODES
    public byte updateTeamsEvenCode = 0;

    /// <summary>  
    /// GameObject Awake
    /// </summary>  
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        teamWhite = new string[6];
        teamBlack = new string[6];
        //Adding callback on event recept
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    /// <summary>  
    /// GameObject Start
    /// </summary>  
    public void Start()
    {
        PhotonView photonView = PhotonView.Get(this);
        photonView.RPC("GetTeam", RpcTarget.MasterClient, chosenName);
    }

    // Server function
    /// <summary> set a team for the player who called and send team updates </summary>  
    /// <param name="name">the name of the new player</param>
    [PunRPC]
    public void GetTeam(string name, PhotonMessageInfo info)
    {
        PhotonView photonView = PhotonView.Get(this);
        playersNumber++;
        //Add alternatively the new player in each team
        if ((playersNumber % 2) == 1)
        {
            photonView.RPC("SpawnWithTeam", info.Sender, true);
            for (int i = 0; i < 6; i++)
                if (string.IsNullOrEmpty(teamBlack[i]))
                {
                    teamBlack[i] = name;
                    i = 6;
                }
        }
        else
        {
            photonView.RPC("SpawnWithTeam", info.Sender, false);
            for (int i = 0; i < 6; i++)
                if (string.IsNullOrEmpty(teamWhite[i]))
                {
                    teamWhite[i] = name;
                    i = 6;
                }
        }
        //Update the team lists
        SendUpdateTeamsEvent();
    }

    // Server function
    /// <summary>  
    /// Update the team lists sending an event to every players
    /// </summary> 
    public void SendUpdateTeamsEvent()
    {
        //Send the lists as an unique object
        object[] content = new object[] { playersNumber,
            teamWhite[0], teamWhite[1], teamWhite[2],
            teamWhite[3], teamWhite[4], teamWhite[5],
            teamBlack[0], teamBlack[1], teamBlack[2],
            teamBlack[3], teamBlack[4], teamBlack[5]};
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(updateTeamsEvenCode, content, raiseEventOptions, sendOptions);
    }

    // Client function
    /// <summary>  
    /// Photon event callback function
    /// </summary> 
    public void OnEvent(EventData photonEvent)
    {
        //Recieve teams and player number updates
        if (photonEvent.Code == updateTeamsEvenCode)
        {
            object[] data = (object[])photonEvent.CustomData;
            playersNumber = (int)data[0];
            for (int i = 0; i < 6; i++)
                teamWhite[i] = (string)data[1 + i];
            for (int i = 0; i < 6; i++)
                teamBlack[i] = (string)data[7 + i];
        }
    }

    // Client function
    /// <summary> Instantiate a new player to a team spawn </summary> 
    /// <param name="black"> true if the player team is black </param>
    [PunRPC]
    public void SpawnWithTeam(bool black)
    {
        //Only instancing new player
        if (PlayerSystem.LocalPlayerInstance == null)
        {
            if (black)
                PhotonNetwork.Instantiate(playerPrefabsBlack[chosenCharacter].name, spawnPointTeamBlack.transform.position, spawnPointTeamBlack.transform.rotation, 0);
            else
                PhotonNetwork.Instantiate(playerPrefabsWhite[chosenCharacter].name, spawnPointTeamWhite.transform.position, spawnPointTeamWhite.transform.rotation, 0);
        }
        else
            Debug.Log("Ignoring scene load for player");
    }

    // Server function
    /// <summary> return true if player's team is black </summary> 
    /// <param name="name"> the name of the player </param>
    /// <returns> true if in black team </returns>
    public bool IsPlayerTeamBlack(string name)
    {
        for (int i = 0; i < 6; i++)
            if (teamBlack[i] == name)
                return true;
        return false;
    }

    // Server function
    /// <summary> Get black spawn point </summary> 
    /// <returns> The transform of the black spawn point </returns>
    public Transform getBlackSpawnTransform()
    {
        return spawnPointTeamBlack.transform;
    }

    // Server function
    /// <summary> Get white spawn point </summary> 
    /// <returns> The transform of the white spawn point </returns>
    public Transform getWhiteSpawnTransform()
    {
        return spawnPointTeamWhite.transform;
    }
}
