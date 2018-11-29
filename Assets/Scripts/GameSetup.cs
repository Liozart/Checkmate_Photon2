 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class GameSetup : MonoBehaviourPunCallbacks {

    public Canvas networkCanvas;
    public Canvas modeCanvas;
    public Canvas characterCanvas;
    public Text nameText;
    public Text adressText;

    // Lobby is singleton
    public static GameSetup lobby;
    public static string roomName = "room1";
    public static string levelName = "GameScene";
    private byte playerNumber = 12;

    private bool isRoomLoaded;

    void Awake () {
        lobby = this;
        //PhotonNetwork.AutomaticallySyncScene = true;
        isRoomLoaded = false;
    }

    void Start()
    {
        //Connect to photon servers
        PhotonNetwork.ConnectUsingSettings();
    }

    // Create a client and connect to the server port
    public void SetupClient()
    {
        PhotonNetwork.JoinRandomRoom();
        DisplaySelection(false);
    }

    // Create a a server and local client and connect to the local server
    public void SetupServerAndClient()
    {
        RoomOptions roomOpts = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = playerNumber };
        PhotonNetwork.CreateRoom(roomName, roomOpts);
        DisplaySelection(true);
    }

    public override void OnJoinedRoom()
    {
        isRoomLoaded = true;
    }

    //Display mode selection if host
    void DisplaySelection(bool serv)
    {
        //ManagerSpawner.myClient = myClient;
        networkCanvas.gameObject.SetActive(false);
        if (serv)
        {
            modeCanvas.gameObject.SetActive(true);
        }
        else
            characterCanvas.gameObject.SetActive(true);
    }

    //When gamemode is set, change menu
    public void SetMode(int m)
    {
        PlayerManager.chosenGameMode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
    }

    //Then load the gamescene
    public void SetCharacter(int c)
    {
        PlayerManager.chosenCharacter = c;
        PlayerManager.chosenName = nameText.text;

        while (!isRoomLoaded)
        {
            StartCoroutine(WaitUntilLoaded());
        }
        
        PhotonNetwork.LoadLevel(levelName);
    }

    IEnumerator WaitUntilLoaded()
    {
        yield return new WaitForSeconds(1f);
    }
}
