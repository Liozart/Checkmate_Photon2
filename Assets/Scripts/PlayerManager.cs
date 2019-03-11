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
    // Custom Event 0: Used as "MoveUnitsToTargetPosition" event
    const byte evCodeUpdateInfos = 0;
    const byte evCodeStartRound = 1;
    const byte evCodeWinBlack = 2;
    const byte evCodeWinWhite = 3;
    const byte evCodeUpdateScores = 4;

    string freeTeamSlot = "§§§Free§§§";

    //Host selected gamemode
    public static int chosenGameMode;
    //Selected character
    public static int chosenCharacter;
    public static string chosenName;

    //0 : king, 1 : queen, 2 : pawn
    public GameObject[] playerPrefabsWhite;
    public GameObject[] playerPrefabsBlack;
    public GameObject playerPrefab;

    //Respawn points position
    public GameObject spawnPointTeamWhite;
    public GameObject spawnPointTeamBlack;

    public int playersNumber;
    public int playersReady;
    // teams lists
    public string[] teamWhite;
    public string[] teamBlack;

    public int playerAliveWhite;
    public int playerAliveBlack;

    //Scores
    public int scorewhite;
    public int scoreblack;

    PlayState state;

    /// <summary>  
    /// GameObject Awake
    /// </summary>  
    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
        //PhotonNetwork.AddCallbackTarget(this);
        PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
    }

    /// <summary>  
    /// GameObject Start
    /// </summary>  
    public void Start()
    {
        state = PlayState.Waiting;
        teamWhite = new string[6];
        teamBlack = new string[6];
        SetTeamListSendable();
        playersNumber = 0;
        scoreblack = 0;
        scorewhite = 0;
        playersReady = 0;
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
        //Add alternatively the new player in a team
        if ((playersNumber % 2) == 1)
        {
            photonView.RPC("SpawnWithTeam", info.Sender, true);
            for (int i = 0; i < 6; i++)
                if (teamBlack[i].Equals(freeTeamSlot))
                {
                    teamBlack[i] = name;
                    i = 6;
                }
        }
        else
        {
            photonView.RPC("SpawnWithTeam", info.Sender, false);
            for (int i = 0; i < 6; i++)
                if (teamWhite[i].Equals(freeTeamSlot))
                {
                    teamWhite[i] = name;
                    i = 6;
                }
        }

        //Send updated infos to clients
        SendTeamUpdateEvent();
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
                playerPrefab = PhotonNetwork.Instantiate(playerPrefabsBlack[chosenCharacter].name, spawnPointTeamBlack.transform.position, spawnPointTeamBlack.transform.rotation, 0);
            else
                playerPrefab = PhotonNetwork.Instantiate(playerPrefabsWhite[chosenCharacter].name, spawnPointTeamWhite.transform.position, spawnPointTeamWhite.transform.rotation, 0);
        }
        else
            Debug.Log("Ignoring scene load for player");
    }

    // Server/Client function
    /// <summary> Recieve and execute events </summary> 
    /// <param name="photonEvent"> contains the events infos </param>
    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            //Update game infos
            case evCodeUpdateInfos:
                object[] data = (object[])photonEvent.CustomData;
                teamWhite = (string[])data[0];
                teamBlack = (string[])data[1];
                playersNumber = (int)data[2];
                break;
            
            //Round start
            case evCodeStartRound:
                playerPrefab.GetComponent<PlayerSystem>().StartNewRound();
                break;

            //Win events
            //Black wins
            case evCodeWinBlack:
                playerPrefab.GetComponent<PlayerSystem>().WinBlackTeam();
                state = PlayState.Waiting;
                StartCoroutine(WaitForNextRound());
                break;

            //White wins
            case evCodeWinWhite:
                playerPrefab.GetComponent<PlayerSystem>().WinWhiteTeam();
                state = PlayState.Waiting;
                StartCoroutine(WaitForNextRound());
                break;

            //Update scores
            case evCodeUpdateScores:
                object[] data2 = (object[])photonEvent.CustomData;
                scorewhite = (int)data2[0];
                scoreblack = (int)data2[1];
                playerPrefab.GetComponent<PlayerSystem>().UpdateScoreCanvas();
                break;
        }
    }

    // Server function
    /// <summary> Send an event with updated teams infos </summary> 
    public void SendTeamUpdateEvent()
    {
        //Object : [0]->teamWhite, [1]->teamBlack, [2]->players number
        object[] content = new object[] { teamWhite, teamBlack, playersNumber };
        //Send to others PlayerManagers
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCodeUpdateInfos, content, raiseEventOptions, sendOptions);
    }

    // Server function
    /// <summary> Send an event with updated scores </summary> 
    public void SendScoreUpdateEvent()
    {
        //Object : [0]->scoresWhite, [1]->scoresBlack
        object[] content = new object[] { scorewhite, scoreblack };
        //Send to others PlayerManagers
        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCodeUpdateScores, content, raiseEventOptions, sendOptions);

        //Update player's scores canvas
        playerPrefab.GetComponent<PlayerSystem>().UpdateScoreCanvas();
    }

    // Client function
    /// <summary> Call ready function on server </summary> 
    public void SetPlayerReady()
    {
        photonView.RPC("PlayerReady", RpcTarget.MasterClient);
    }

    // Client function
    /// <summary> Call player dead function on server </summary> 
    public void SetPlayerDead()
    {
        photonView.RPC("PlayerDead", RpcTarget.MasterClient, PlayerManager.chosenName);
    }

    // Server function
    /// <summary> Recieve "ready players" signal and launch a new round </summary> 
    [PunRPC]
    public void PlayerReady()
    {
        playersReady++;
        //When a second player comes, launch the first round
        if (playersReady > 1 && state == PlayState.Waiting)
        {
            RaiseEventOptions raiseEventOptions2 = new RaiseEventOptions { Receivers = ReceiverGroup.All };
            SendOptions sendOptions = new SendOptions { Reliability = true };
            PhotonNetwork.RaiseEvent(evCodeStartRound, null, raiseEventOptions2, sendOptions);
            state = PlayState.Playing;

            //All players are alive at start
            playerAliveBlack = CountPlayerInTeam(true);
            playerAliveWhite = CountPlayerInTeam(false);
        }
    }

    // Server function
    /// <summary> Recieve "ready players" signal and launch a new round </summary> 
    [PunRPC]
    public void PlayerDead(string name)
    {
        bool ok = false;
        //Check in which team the player is
        for (int i = 0; i < 6; i++)
            if (teamBlack[i].Equals(name))
            {
                playerAliveBlack--;
                ok = true;
                Debug.Log("BLACKS DED");
            }
        if (!ok)
        {
            playerAliveWhite--;
            Debug.Log("WHITE DED");
         }

        //Additionnal gamemode checks
        switch (chosenGameMode)
        {
            //Deathmatch
            //Check if a team is dead, send to players the winner
            //and wait 5 seconds before starting a new round
            case 0:
                if (playerAliveBlack == 0 || playerAliveWhite == 0)
                {
                    state = PlayState.Waiting;
                    RaiseEventOptions raiseEventOptions3 = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                    SendOptions sendOptions = new SendOptions { Reliability = true };
                    if (playerAliveBlack == 0)
                    {
                        scorewhite++;
                        PhotonNetwork.RaiseEvent(evCodeWinWhite, null, raiseEventOptions3, sendOptions);
                        Debug.Log("WHITE WINS");
                    }
                    else
                    {
                        scoreblack++;
                        PhotonNetwork.RaiseEvent(evCodeWinBlack, null, raiseEventOptions3, sendOptions);
                        Debug.Log("BLACK WINS");
                    }

                    //Send scores
                    SendScoreUpdateEvent();

                }
                break;
        }
    }

    // Server function
    /// <summary> return true if player's team is black </summary> 
    /// <param name="name"> the name of the player </param>
    /// <returns> true if in black team </returns>
    public bool IsPlayerTeamBlack(string playername)
    {
        for (int i = 0; i < 6; i++)
            if (teamBlack[i] == playername)
                return true;
        return false;
    }
    
    /// <summary> return true if player's team is black </summary> 
    /// <param name="name"> the name of the player </param>
    /// <returns> true if in black team </returns>
    public bool AreInSameTeam(string p1, string p2)
    {
        if (IsPlayerTeamBlack(p1) && IsPlayerTeamBlack(p2) ||
            !IsPlayerTeamBlack(p1) && !IsPlayerTeamBlack(p2))
            return true;
        else return false;
    }
    
    /// <summary> Get black spawn point </summary> 
    /// <returns> The transform of the black spawn point </returns>
    public Transform GetBlackSpawnTransform()
    {
        return spawnPointTeamBlack.transform;
    }
    
    /// <summary> Get white spawn point </summary> 
    /// <returns> The transform of the white spawn point </returns>
    public Transform GetWhiteSpawnTransform()
    {
        return spawnPointTeamWhite.transform;
    }

    // Server function
    /// <summary> Set the null in team arrays to "§free§" in order to the arrays to be sendables </summary> 
    void SetTeamListSendable()
    {
        for (int i = 0; i < 6; i++)
            if (string.IsNullOrEmpty(teamBlack[i]))
                teamBlack[i] = freeTeamSlot;
        for (int i = 0; i < 6; i++)
            if (string.IsNullOrEmpty(teamWhite[i]))
                teamWhite[i] = freeTeamSlot;
    }

    /// <summary>  
    //Returns the number of players in a team
    /// <param name="black"/>true to count players in black team, false for white team</param>
    /// <returns> the number of players </returns>
    /// </summary>
    public int CountPlayerInTeam(bool black)
    {
        int cnt = 0;
        if (black)
        {
            for (int i = 0; i < 6; i++)
                if (!teamBlack[i].Equals(freeTeamSlot))
                    cnt++;
        }
        else
        {
            for (int i = 0; i < 6; i++)
                if (!teamWhite[i].Equals(freeTeamSlot))
                    cnt++;
        }
        return cnt;
    }

    //Wait a bit when a team wins then starts a new round
    IEnumerator WaitForNextRound()
    {
        yield return new WaitForSeconds(3.0f);
        RaiseEventOptions raiseEventOptions4 = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        SendOptions sendOptions = new SendOptions { Reliability = true };
        PhotonNetwork.RaiseEvent(evCodeStartRound, null, raiseEventOptions4, sendOptions);

        playerAliveBlack = CountPlayerInTeam(true);
        playerAliveWhite = CountPlayerInTeam(false);
        state = PlayState.Playing;
    }
}
