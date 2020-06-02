using System.Collections;
using ARTowerDefense.Structures.Dynamic.Defense.Ammo;
using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic.Defense
{
    public class Tower : MonoBehaviour
    {
        public Transform ShootPosition;
        public Transform LookAtObj;
        public GameObject Ammo;
        public Transform Target;
        public int Damage = 10;
        public float ShotDelay;
        protected bool IsShooting;
        protected float HomeY;

        protected virtual void Start()
        {
            HomeY = LookAtObj.transform.localRotation.eulerAngles.y;
        }

        protected virtual void Update()
        {
            // Rotation
            if (Target)
            {
                Vector3 dir = Target.transform.position - LookAtObj.transform.position;
                dir.y = 0;
                Quaternion rot = Quaternion.LookRotation(dir);
                LookAtObj.transform.rotation = Quaternion.Slerp(LookAtObj.transform.rotation, rot, 5 * Time.deltaTime);
            }
            else
            {
                Quaternion home = new Quaternion(0, HomeY, 0, 1);
                LookAtObj.transform.rotation = Quaternion.Slerp(LookAtObj.transform.rotation, home, Time.deltaTime);
            }

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
                b.GetComponent<CannonBall>().target = Target;
                b.GetComponent<CannonBall>().twr = this;
            }

            IsShooting = false;
        }
    }
}