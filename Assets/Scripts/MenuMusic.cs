using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuMusic : MonoBehaviour {

    public AudioSource audioSource;
    bool sceneChanged = false;
    int fadeTime = 5;

	// Use this for initialization
	void Start () {
        DontDestroyOnLoad(gameObject);
        audioSource.loop = true;
        audioSource.Play();
	}
	
	// Update is called once per frame
	void Update () {
		if (SceneManager.GetActiveScene().name == "GameScene")
        {
            if (!sceneChanged)
            {
                sceneChanged = true;
                StartCoroutine(RemoveMusic());
            }
        }
	}

    IEnumerator RemoveMusic()
    {
        float t = fadeTime;
        while (t > 0)
        {
            yield return null;
            t -= Time.deltaTime;
            audioSource.volume = t / fadeTime;
        }
        Destroy(gameObject);
        yield break;
    }
}
