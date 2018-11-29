using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EjectorShoot : MonoBehaviour {

	public GameObject bullet;

	public void Fire(float bulletspeed, int bulletdamage){
		GameObject b = Instantiate(bullet, transform.position, transform.rotation);
		b.GetComponent<Bullet>().SetDamage(bulletdamage);
		b.GetComponent<Rigidbody>().velocity = transform.TransformDirection(Vector3.forward * bulletspeed);
	}
}
