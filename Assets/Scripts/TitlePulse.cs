using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitlePulse : MonoBehaviour {

    int cnt = 0, timer = 46;
    public Text title;
    Color[] colors = new Color[] { Color.green, Color.cyan, Color.blue, Color.black, Color.red, Color.magenta, Color.red, Color.yellow };

	// Use this for initialization
	void Start () {
        Random.InitState(420);
    }
	
	// Update is called once per frame
	void Update () {
        cnt++;
        if (cnt >= timer)
        {
            int tmp = (int)(Random.value * 100 % 8);
            title.color = colors[tmp];
            cnt = 0;
        }
	}
}
