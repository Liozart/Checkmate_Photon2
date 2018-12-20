// ----------------------------------------------------------------------------  
// GameSetup.cs  
// <summary>  
// Manage the different Canvas in NetworkScene, handle the server creation/connexion
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

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

    // the lobby is a singleton
    public static GameSetup lobby;
    public static string roomName = "room1";
    public static string levelName = "GameScene";
    private byte playerNumber = 12;

    /// <summary>  
    /// GameObject Awake  
    /// </summary>  
    void Awake () {
        lobby = this;
    }

    /// <summary>  
    /// GameObject Start  
    /// </summary>  
    void Start()
    {
        //Connect to photon Cloud servers
        PhotonNetwork.ConnectUsingSettings();
    }

    /// <summary>  
    /// Callback for connexion to Photon servers
    /// </summary>  
    public override void OnConnectedToMaster()
    {
        networkCanvas.gameObject.SetActive(true);
    }

    /// <summary>  
	/// "Rejoindre" button function, connect the client to a room (there's only one anyway)
	/// </summary>
    public void SetupClient()
    {
        PhotonNetwork.JoinRandomRoom();
        DisplaySelection(false);
    }

    /// <summary>  
	/// "Créer partie" button function, create a room with options
	/// </summary>  
    public void SetupServerAndClient()
    {
        RoomOptions roomOpts = new RoomOptions() { IsVisible = true, IsOpen = true, MaxPlayers = playerNumber };
        PhotonNetwork.CreateRoom(roomName, roomOpts);
        DisplaySelection(true);
    }

    /// <summary> Display the selection Canvas if host else directly the character Canvas </summary>  
	/// <param name="serv">true if player is the host</param>
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

    /// <summary> Set the chosen gamemode and enable next canvas </summary>  
	/// <param name="m">picked gamemode</param>
    public void SetMode(int m)
    {
        PlayerManager.chosenGameMode = m;
        modeCanvas.gameObject.SetActive(false);
        characterCanvas.gameObject.SetActive(true);
    }

    /// <summary> Set the chosen character and load the scene </summary>  
	/// <param name="c">picked character</param>
    public void SetCharacter(int c)
    {
        PlayerManager.chosenCharacter = c;
        PlayerManager.chosenName = nameText.text;
        PhotonNetwork.LoadLevel(levelName);
    }

    IEnumerator WaitUntilLoaded()
    {
        yield return new WaitForSeconds(1f);
    }
}
