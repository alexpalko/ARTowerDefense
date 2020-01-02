using System.Collections;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public Transform ShootElement;
    public Transform LookAtObj;
    public GameObject Ammo;
    public Transform Target;
    public int Damage = 10;
    public float ShootDelay;
    protected bool m_IsShooting;
    protected float m_HomeY;

    protected virtual void Start()
    {
        m_HomeY = LookAtObj.transform.localRotation.eulerAngles.y;
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
            Quaternion home = new Quaternion(0, m_HomeY, 0, 1);
            LookAtObj.transform.rotation = Quaternion.Slerp(LookAtObj.transform.rotation, home, Time.deltaTime);
        }

        // Shooting
        if (!m_IsShooting)
        {
            StartCoroutine(shoot());
        }
    }

    protected virtual IEnumerator shoot()
    {
        m_IsShooting = true;
        yield return new WaitForSeconds(ShootDelay);

        if (Target)
        {
            GameObject b = GameObject.Instantiate(Ammo, ShootElement.position, Quaternion.identity) as GameObject;
            b.GetComponent<TowerBullet>().target = Target;
            b.GetComponent<TowerBullet>().twr = this;
        }

        m_IsShooting = false;
    }
}
