using ARTowerDefense.Enemies;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense.Ammo
{
    public class CrossbowBolt : MonoBehaviour
    {
        public float Speed;
        public Transform target;

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
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.transform == target)
            {
                target.GetComponent<Enemy>().DoDamage(twr.Damage);
                Destroy(gameObject, i);
            }
        }
    }
}