using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour {

    int bulletDamage;

    void Start()
    {
        StartCoroutine(DestroyWithTime());
    }

    public void SetDamage(int damage)
    {
        bulletDamage = damage;
    }

    void OnTriggerEnter(Collider col)
    {
        /*if (col.tag == "Player")
        {
            col.gameObject.GetComponent<PlayerSystem>().Damage(bulletDamage);
        }
        else if (col.tag == "Cobaye")
            Destroy(col.gameObject);

        if (col.tag != "Gun")
            Destroy(gameObject);*/
    }

    IEnumerator DestroyWithTime()
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }
}
