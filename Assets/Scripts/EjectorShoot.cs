// ----------------------------------------------------------------------------  
// EjectorShoot.cs  
// <summary>  
// Shoot a bullet prefab
// </summary>  
// <author>Léo Pichat</author>  
// ---------------------------------------------------------------------------- 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EjectorShoot : MonoBehaviour {

	public GameObject bullet;

    /// <summary> Instantiate a bullet gameobject with character's properties and throw it forward </summary>
    /// <param name="bulletdamage">the damage the bullet do</param>
    /// <param name="bulletspeed">the speed of the bullet</param>
	public void Fire(float bulletspeed, int bulletdamage){
		GameObject b = Instantiate(bullet, transform.position, transform.rotation);
		b.GetComponent<Bullet>().SetDamage(bulletdamage);
		b.GetComponent<Rigidbody>().velocity = transform.TransformDirection(Vector3.forward * bulletspeed);
	}
}
