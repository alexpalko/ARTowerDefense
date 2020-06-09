﻿using UnityEngine;
using System.Collections;
using ARTowerDefense.Enemies;

public class EnemyBullet : MonoBehaviour
{

    public float Speed;
    public Transform target;
    public GameObject impactParticle; // Ammo impact    
    public Vector3 impactNormal; 
    Vector3 lastBulletPosition; 
    public Enemy twr;
    float i = 0.05f; // delay time of Ammo destruction


    void Update()
    {

        // Bullet move

        if (target)
        {

            transform.LookAt(target);
            transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * Speed); 
            lastBulletPosition = target.transform.position; 

        }

        // Move Ammo ( enemy was disapeared )

        else
        {

            transform.position = Vector3.MoveTowards(transform.position, lastBulletPosition, Time.deltaTime * Speed); 

            if (transform.position == lastBulletPosition)
            {
                Destroy(gameObject, i);

                // Bullet hit ( enemy was disapeared )

                if (impactParticle != null) // poison tower showed error
                {
                    impactParticle = Instantiate(impactParticle, transform.position, Quaternion.FromToRotation(Vector3.up, impactNormal)) as GameObject;
                    Destroy(impactParticle, 3);
                    return;
                }
            }

        }



    }

    // Bullet hit

    void OnTriggerEnter(Collider other) 
    {
        if (other.gameObject.transform == target)
        {
            //Target.GetComponent<TowerHP>().Dmg_2(Tower.Creature_Damage);
            Destroy(gameObject, i); // destroy Ammo
            impactParticle = Instantiate(impactParticle, transform.position, Quaternion.FromToRotation(Vector3.up, impactNormal)) as GameObject;
            impactParticle.transform.parent = target.transform;
            Destroy(impactParticle, 3);
            return;
        }
    }

}




