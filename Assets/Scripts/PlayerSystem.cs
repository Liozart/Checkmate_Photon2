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

public class PlayerSystem: MonoBehaviourPun, IPunObservable
{
    //Local instance of the player
    public static GameObject LocalPlayerInstance;

    //chosen character
    public int character;

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

    public GameObject pistol;
    EjectorShoot ejector;
    ParticleSystem particles;
    public FirstPersonController FPController;
    public PlayerManager playerManager;

    public Text statsText;

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
            Debug.Log("SEND " + info.Sender + " : " + isFiring);
        }
        //Recieve updates (In the same send order)
        else
        {
            Quaternion f = (Quaternion)stream.ReceiveNext();
            pistol.transform.rotation = f;
            isFiring = (bool)stream.ReceiveNext();
            Debug.Log("GET " + info.Sender + " : " + isFiring);
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
        //Set character stats
        character = PlayerManager.chosenCharacter;
        InitChosenCharacter();

        //retrieve components
        ejector = gameObject.GetComponentInChildren<EjectorShoot>();
        pistol = FPController.gameObject.GetComponentInChildren<MeshRenderer>().gameObject;
        particles = gameObject.GetComponentInChildren<ParticleSystem>();
        particles.Stop();

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
                playerManager = GameObject.FindGameObjectWithTag("PlayerManager").GetComponent<PlayerManager>();
                gameObject.SetActive(true);
                onGameScene = true;
            }

        //Input and UI udpates are only local
        if (photonView.IsMine)
        {
            //Shoot
            if (Input.GetKeyDown(KeyCode.Mouse0))
                isFiring = true;
            else if (Input.GetKeyUp(KeyCode.Mouse0))
                isFiring = false;
            
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
        statsText.text = "| " + health + "\n| " + speed + "\n| " + damage +
                        "\n| " + fireRate + "\n| " + bulletSpeed;
    }

    /// <summary>  
    /// Call the ejector's fire function and particles  
    /// </summary> 
    void Fire()
    {
        particles.Play();
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
                //Set player position to spawn at death
                if (playerManager.IsPlayerTeamBlack(PlayerManager.chosenName))
                    gameObject.transform.position = playerManager.getBlackSpawnTransform().position;
                else
                    gameObject.transform.position = playerManager.getWhiteSpawnTransform().position;
            }
        }
    }
}
