// ----------------------------------------------------------------------------  
// Bullet.cs  
// <summary>  
// Attached to the Bullet prefab, manage its collisions and lifetime and applies damages
// </summary>  
// <author>Léo Pichat</author>  
// ----------------------------------------------------------------------------  

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    int bulletDamage;

    /// <summary>   
    /// GameObject Start, the gameObject is destroyed after 3 seconds   
    /// (enough time for the slowest bullet to cross over the map)  
    /// </summary>  
    void Start()
    {
        StartCoroutine(DestroyWithTime());
    }

    /// <summary>Set the damage the bullet is gonna do</summary>
    /// <param name="SetDamage">the damage the bullet do</param>
    public void SetDamage(int damage)
    {
        bulletDamage = damage;
    }

    /// <summary>   
    /// GameObject OnTriggerEnter
    /// </summary> 
    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "King" || col.tag == "Queen" || col.tag == "Pawn")
            col.gameObject.GetComponent<PlayerSystem>().Damage(bulletDamage);

        if (col.tag != "Gun")
            Destroy(gameObject);
    }

    IEnumerator DestroyWithTime()
    {
        yield return new WaitForSeconds(2.0f);
        Destroy(gameObject);
    }
}
