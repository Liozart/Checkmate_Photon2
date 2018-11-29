using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.FirstPerson;
using Photon.Pun;
using Photon.Realtime;

public class PlayerSystem : MonoBehaviourPun
{
    //Local instance
    public static GameObject LocalPlayerInstance;

    public int character;

	public int baseHealth = 100;
	public int baseSpeed = 100;
	public int baseDamage = 10;
    public int baseBulletSpeed = 150;
    public float baseFireRate = 0.18f;

    int health;
    int speed;
    int damage;
	int bulletSpeed;
    float fireRate;

	float nextFire = 0;

    bool showCursor = false;

	EjectorShoot ejector;
    ParticleSystem particles;
    public FirstPersonController FPController;
    
    public Text statsText;

    public void Awake()
    {
        if (photonView.IsMine)
        {
            PlayerSystem.LocalPlayerInstance = this.gameObject;
            Debug.Log("IsMine");
        }
        DontDestroyOnLoad(this.gameObject);
    }

    // Use this for initialization
    void Start()
    {
        character = PlayerManager.chosenCharacter;
        InitChosenCharacter();

        ejector = gameObject.GetComponentInChildren<EjectorShoot>();
        particles = gameObject.GetComponentInChildren<ParticleSystem>();
        particles.Stop();

        if (!photonView.IsMine)
        {
            gameObject.GetComponentInChildren<Camera>().gameObject.SetActive(false);
            gameObject.GetComponentInChildren<Canvas>().gameObject.SetActive(false);
            gameObject.GetComponent<FirstPersonController>().enabled = false;
        }
    }

	// Update is called once per frame
	void Update ()
    {
        if (!photonView.IsMine)
            return;

        //Display Cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            showCursor = !showCursor;
            FPController.ChangeCursorState(showCursor);
        }

        //Shoot
        if (Input.GetButton("Fire1")) {
			if (Time.time > nextFire) {
				nextFire = Time.time + fireRate;
				Fire();
			}
		}

        //UI
        UpdateUI();
	}

    //Change stats for the selected character
    void InitChosenCharacter()
    {
        switch (character)
        {
            case 0:
                baseHealth = 200;
                baseSpeed = 80;
                baseDamage = 20;
                baseBulletSpeed = 40;
                break;
            case 1:
                baseHealth = 110;
                baseSpeed = 110;
                baseBulletSpeed = 200;
                break;
            case 2:
                baseHealth = 80;
                baseSpeed = 140;
                baseDamage = 5;
                baseFireRate = 0.1f;
                break;
        }

        health = baseHealth;
        speed = baseSpeed;
        damage = baseDamage;
        bulletSpeed = baseBulletSpeed;
        fireRate = baseFireRate;

        FPController.SetCharacterSpeed(speed);
    }

    void UpdateUI()
    {
        statsText.text = "| " + health + "\n| " + speed + "\n| " + damage +
                        "\n| " + fireRate + "\n| " + bulletSpeed;
    }

    //Call the ejector to fire a projectile
    void Fire()
    {
        particles.Play();
        ejector.Fire(bulletSpeed, damage);
	}

    //Get damages
    public void Damage(int dam)
    {
        health -= dam;
        if (health <= 0)
        {
            Debug.Log("Dead");
        }
    }
}
