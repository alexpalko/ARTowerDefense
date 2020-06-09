using System.Collections;
using ARTowerDefense.Structures.Dynamic.Defense.Ammo;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense
{
    public class Tower : MonoBehaviour
    {
        public Transform ShootPosition;
        public Transform Shooter;
        public GameObject Ammo;
        public Transform Target;
        public int Damage = 10;
        public float ShotDelay;
        protected bool IsShooting;
        protected float HomeY;

        protected virtual void Start()
        {
            HomeY = Shooter.transform.localRotation.eulerAngles.y;
        }

        protected virtual void Update()
        {
            // Rotation
            if (Target)
            {
                Vector3 dir = Target.transform.position - Shooter.transform.position;
                dir.y = 0;
                Shooter.transform.rotation = Quaternion.Slerp(Shooter.transform.rotation,
                    Quaternion.LookRotation(dir), 5 * Time.deltaTime);
            }
            //else
            //{
            //    Shooter.transform.rotation = Quaternion.Slerp(Shooter.transform.rotation,
            //        new Quaternion(0, HomeY, 0, 1), Time.deltaTime);
            //}

            // Shooting
            if (!IsShooting)
            {
                StartCoroutine(Shoot());
            }
        }

        protected virtual IEnumerator Shoot()
        {
            IsShooting = true;
            yield return new WaitForSeconds(ShotDelay);
            
            if (Target)
            {
                GameObject b = Instantiate(Ammo, ShootPosition.position, Quaternion.identity);
                b.GetComponent<CannonBall>().Target = Target;
                b.GetComponent<CannonBall>().Tower = this;
            }
            
            IsShooting = false;
        }
    }
}