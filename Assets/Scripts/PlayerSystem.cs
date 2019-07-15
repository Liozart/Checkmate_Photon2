// ----------------------------------------------------------------------------  
// PlayerSystem.cs  
// <summary>  
// Attached to a player prefab, it manages its properties, UI, movement and shooting input  
// </summary>  
// <author>Léo Pichat</author>  
// ---------------------------------------------------------------------------- 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

//Play state : Waiting for the next round to begin, or playing
public enum PlayState
{
    Waiting, Playing
}

public class PlayerSystem: MonoBehaviourPun, IPunObservable
{
    //Local instance of the player
    public static GameObject LocalPlayerInstance;

    //chosen character
    public int character;
    public string pseudo;

    //character stats, modified in init
    public int baseHealth = 100;
    public int baseSpeed = 100;
    public int baseDamage = 10;
    public int baseBulletSpeed = 150;
    public float baseFireRate = 0.18f;

    //current stats
    int health;
    int speed;
    int damage;
    int bulletSpeed;
    float fireRate;

    float nextFire = 0;
    bool isFiring = false;

    // Shooting object 
    public GameObject pistol;
    EjectorShoot ejector;
    public ParticleSystem particlesFire;
    public ParticleSystem particlesAbilityTargeted;
    // Attached FirstPersonController script  
    public FirstPersonController FPController;
    public PlayerManager playerManager;
    //Top camera for waiting players
    public GameObject waitCamera;

    //Cooldown bool to limit ability use
    bool canUseAbility;

    //State of the current player
    public PlayState playState;

    // UI text
    //Stats
    public Text statsText;
    //Win labels
    public Text winWhiteText;
    public Text winBlackText;
    //Score
    public Text scoresCanvas;

    bool onGameScene = false;

    /// <summary>  
    /// Photon OnPhotonSerializeView
    /// </summary>  
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Send updates
        if (stream.IsWriting)
        {
            stream.SendNext(pistol.transform.rotation);
            stream.SendNext(isFiring);
            stream.SendNext(pseudo);
        }
        //Recieve updates (In the same send order)
        else
        {
            Quaternion f = (Quaternion)stream.ReceiveNext();
            pistol.transform.rotation = f;
            isFiring = (bool)stream.ReceiveNext();
            pseudo = (string)stream.ReceiveNext();
            
        }
    }

    /// <summary>  
    /// GameObject Awake
    /// </summary>  
    public void Awake()
    {
        if (photonView.IsMine)
        {
            //We keep intance of players when the scene is loading for new players
            PlayerSystem.LocalPlayerInstance = this.gameObject;
        }
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>  
    /// GameObject Start  
    /// </summary>  
    void Start()
    {
        //Waiting at first
        playState = PlayState.Waiting;

        //Set character stats
        character = PlayerManager.chosenCharacter;
        InitChosenCharacter();
        pseudo = PlayerManager.chosenName;

        //retrieve components
        ejector = gameObject.GetComponentInChildren<EjectorShoot>();
        pistol = FPController.gameObject.GetComponentInChildren<MeshRenderer>().gameObject;
        //Particles when firing
        particlesFire.Stop();
        //Particles when targeted by ability
        particlesAbilityTargeted.Stop();

        //Disabling a few non-local components
        if (!photonView.IsMine)
        {
            //Disable first Person components
            gameObject.GetComponent<FirstPersonController>().enabled = false;
            gameObject.GetComponentInChildren<Camera>().enabled = false;
            gameObject.GetComponentInChildren<AudioListener>().enabled = false;
            gameObject.GetComponentInChildren<FlareLayer>().enabled = false;
            //UI
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
        }
        else
        {
            //Also disabling the local mesh gameobject
            MeshRenderer[] robjs = gameObject.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < robjs.Length; i++)
                if (robjs[i].gameObject.name == "MeshModel")
                    robjs[i].gameObject.SetActive(false);

            playerManager = GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>();

            canUseAbility = true;
        }
    }

    /// <summary>  
    /// GameObject Update  
    /// </summary>  
    void Update()
    {
        // Because players objects are already loaded when a player join a room, 
        // it get disabled until this player is in the game scene.
        if (!onGameScene)
            if (SceneManager.GetActiveScene().name == "GameScene")
            {
                onGameScene = true;
                gameObject.SetActive(true);

                if (photonView.IsMine)
                {
                    //Set wait camera
                    waitCamera = GameObject.FindGameObjectWithTag("WaitCamera");
                    SwitchPlayState(playState);

                    //Send ready RPC to master when player finished inits
                    playerManager.SetPlayerReady();
                }
            }

        //Input and UI udpates are only local
        if (photonView.IsMine)
        {
            //No inputs if waiting
            if (playState == PlayState.Waiting)
                return;

            //Shoot
            if (Input.GetKeyDown(KeyCode.Mouse0))
                isFiring = true;
            else if (Input.GetKeyUp(KeyCode.Mouse0))
                isFiring = false;

            // Get special ability input
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                if (canUseAbility)
                {
                    //Raycast radius is large so players don't have to be exactly precise
                    float castingRadius = 1.5f;
                    float castingDistance = 20f;
                    RaycastHit hit;
                    Vector3 ray = transform.position + gameObject.GetComponent<CharacterController>().center;
                    //if ability hit someone
                    if (Physics.SphereCast(ray, castingRadius, transform.forward, out hit, castingDistance))
                    {
                        //Apply effect on player depending the team
                        if (playerManager.AreInSameTeam(pseudo, hit.transform.gameObject.GetComponent<PlayerSystem>().pseudo))
                            hit.transform.gameObject.GetPhotonView().RPC("GetAbilityEffect_Healing", RpcTarget.All);
                        else
                            hit.transform.gameObject.GetPhotonView().RPC("GetAbilityEffect_Stopping", RpcTarget.All);
                        //start cooldown
                        StartCoroutine(AbilityCooldown());
                        canUseAbility = false;
                    }
                }
            }

            //Display scoreboard
            if (Input.GetKeyDown(KeyCode.Tab))
                scoresCanvas.gameObject.SetActive(true);
            if (Input.GetKeyUp(KeyCode.Tab))
                scoresCanvas.gameObject.SetActive(false);

            UpdateUI();
        }

        //Fire is remote or local streamed
        if (isFiring)
        {
            //Fire rate
            if (Time.time > nextFire)
            {
                nextFire = Time.time + fireRate;
                Fire();
            }
        }
    }

    /// <summary>   
    /// Get called when the player is targeted by a friendly player ability
    /// </summary>  
    [PunRPC]
    public void GetAbilityEffect_Healing()
    {
        health = baseHealth;
        particlesAbilityTargeted.startColor = Color.green;
        particlesAbilityTargeted.Play();

    }

    /// <summary>  
    /// Get called when the player is targeted by a enemy player ability
    /// </summary>  
    [PunRPC]
    public void GetAbilityEffect_Stopping()
    {
        speed = 20;
        FPController.ChangeCharacterSpeedTemporary(speed);
        particlesAbilityTargeted.startColor = Color.red;
        particlesAbilityTargeted.Play();
        StartCoroutine(RechangeSpeed());
    }

    /// <summary>  
    /// Change the base player properties for the selected character 
    /// </summary>  
    void InitChosenCharacter()
    {
        switch (character)
        {
            //King
            case 0:
                baseHealth = 200;
                baseSpeed = 80;
                baseDamage = 20;
                baseBulletSpeed = 40;
                break;
            //Queen
            case 1:
                baseHealth = 110;
                baseSpeed = 110;
                baseBulletSpeed = 200;
                break;
            //Pawn
            case 2:
                baseHealth = 80;
                baseSpeed = 140;
                baseDamage = 5;
                baseFireRate = 0.1f;
                break;
        }

        //Set character stats
        health = baseHealth;
        speed = baseSpeed;
        damage = baseDamage;
        bulletSpeed = baseBulletSpeed;
        fireRate = baseFireRate;

        //Call to a created FPConttroller function
        FPController.SetCharacterSpeed(speed);
    }

    /// <summary>  
    /// Set the text UI with player properties  
    /// </summary>  
    void UpdateUI()
    {
        statsText.text = "| " + health + "\n| " + speed;
        if (canUseAbility)
            statsText.text += "\nAbility Available";
    }

    /// <summary>  
    /// Set players ready to play and start round
    /// </summary>  
    public void StartNewRound()
    {
        if (photonView.IsMine)
        {
            winWhiteText.gameObject.SetActive(false);
            winBlackText.gameObject.SetActive(false);
            playState = PlayState.Playing;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Call the ejector's fire function and particles  
    /// </summary> 
    void Fire()
    {
        particlesFire.Play();
        ejector.Fire(bulletSpeed, damage);
    }

    /// <summary>  
    /// Get called by a bullet when hit  
    /// </summary>
    public void Damage(int dam)
    {
        if (photonView.IsMine)
        {
            health -= dam;
            if (health <= 0)
            {
                health = baseHealth;
                playerManager.SetPlayerDead();
                //Player is waiting until new round
                playState = PlayState.Waiting;
                SwitchPlayState(playState);
            }
        }
    }

    /// <summary>  
    /// Called by server when the black team wins the current round
    /// </summary>
    public void WinBlackTeam()
    {
        if (photonView.IsMine)
        {
            //Display win text
            winBlackText.gameObject.SetActive(true);
            //Wait until next round
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// Called by server when the white team wins the current round
    /// </summary>
    public void WinWhiteTeam()
    {
        if (photonView.IsMine)
        {
            //Display win text
            winWhiteText.gameObject.SetActive(true);
            //Wait until next round
            playState = PlayState.Waiting;
            SwitchPlayState(playState);
        }
    }

    /// <summary>  
    /// updates the scoreboard, called by server when score update event is handled
    /// </summary>
    public void UpdateScoreCanvas()
    {
        //Update scoreboard
        scoresCanvas.text = "White team : " + playerManager.scorewhite + "\nBlack team : " + playerManager.scoreblack;
    }

    /// <summary>  
    /// Set the properties of the player for it's current state
    /// <param name="state"/>The state to switch to</param>
    /// </summary>
    void SwitchPlayState(PlayState state)
    {
        if (photonView.IsMine)
        {
            //Player is waiting for round
            if (state == PlayState.Waiting)
            {
                //Enable spectator camera and disable inputs
                waitCamera.SetActive(true);
                gameObject.GetComponent<FirstPersonController>().enabled = false;
                gameObject.GetComponentInChildren<Camera>().enabled = false;
                gameObject.GetComponentInChildren<AudioListener>().enabled = false;
                isFiring = false;

                //Place player on respective spawn
                if (playerManager.IsPlayerTeamBlack(PlayerManager.chosenName))
                    gameObject.transform.position = playerManager.GetBlackSpawnTransform().position;
                else
                    gameObject.transform.position = playerManager.GetWhiteSpawnTransform().position;
            }
            else
            {
                //Enable player object camera and enable inputs
                waitCamera.SetActive(false);
                gameObject.GetComponent<FirstPersonController>().enabled = true;
                gameObject.GetComponentInChildren<Camera>().enabled = true;
                gameObject.GetComponentInChildren<AudioListener>().enabled = true;
            }
        }
    }

    IEnumerator AbilityCooldown()
    {
        yield return new WaitForSeconds(3.0f);
        canUseAbility = true;
    }

    IEnumerator RechangeSpeed()
    {
        yield return new WaitForSeconds(4.0f);
        speed = baseSpeed;
    }
}
