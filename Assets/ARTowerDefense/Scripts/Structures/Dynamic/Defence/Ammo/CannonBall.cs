using ARTowerDefense.Enemies;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense.Ammo
{
    public class CannonBall : MonoBehaviour
    {
        public float Speed;
        public Transform target;
        public GameObject impactParticle;

        public Vector3 impactNormal;
        Vector3 lastBulletPosition;
        public Tower twr;
        readonly float i = 0.05f;

        void Update()
        {
            if (target)
            {
                transform.LookAt(target);
                transform.position = Vector3.MoveTowards(transform.position, target.position, Time.deltaTime * Speed);
                lastBulletPosition = target.transform.position;
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, lastBulletPosition, Time.deltaTime * Speed);
                if (transform.position == lastBulletPosition)
                {
                    Destroy(gameObject, i);

                    if (impactParticle != null)
                    {
                        impactParticle = Instantiate(impactParticle, transform.position,
                            Quaternion.FromToRotation(Vector3.up, impactNormal));
                        Destroy(impactParticle, 3);
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.transform == target)
            {
                target.GetComponent<EnemyHealth>().DoDamage(twr.Damage);
                Destroy(gameObject, i); 
                impactParticle = Instantiate(impactParticle, target.transform.position, Quaternion.FromToRotation(Vector3.up, impactNormal));
                impactParticle.transform.parent = target.transform;
                Destroy(impactParticle, 3);
            }

            // Special Cannon Ball Behavior
            var enemies = GameObject.FindGameObjectsWithTag("enemyBug");
            foreach (var enemy in enemies)
            {
                if (enemy.transform == target) continue;

                if (Vector3.Distance(enemy.transform.parent.position, target.position) < .2f)
                {
                    enemy.GetComponent<EnemyHealth>().DoDamage(5);
                }
            }
        }
    }
}