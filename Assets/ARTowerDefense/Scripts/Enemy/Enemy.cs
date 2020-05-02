using UnityEngine;

public class Enemy : MonoBehaviour
{
    //public Transform shootElement;
    //public GameObject bullet;
    public GameObject Enemybug;
    //public int Creature_Damage = 10;
    //public float Speed;
    // 
    //public Transform[] waypoints;
    //int curWaypointIndex = 0;
    //public float previous_Speed;
    public Animator m_Animator;
    public EnemyHealth EnemyHealth;
    //public Transform target;
    //public GameObject EnemyTarget;


    void Start()
    {
        m_Animator = GetComponent<Animator>();
        EnemyHealth = Enemybug.GetComponent<EnemyHealth>();
        //previous_Speed = Speed;
    }

    // Attack

    //void OnTriggerEnter(Collider other)

    //{
    //    if (other.tag == "Castle")
    //    {

    //        Speed = 0;
    //        EnemyTarget = other.gameObject;
    //        target = other.gameObject.transform;
    //        Vector3 targetPosition = new Vector3(EnemyTarget.transform.position.x, transform.position.y, EnemyTarget.transform.position.z);
    //        transform.LookAt(targetPosition);
    //        m_Animator.SetBool("RUN", false);
    //        m_Animator.SetBool("Attack", true);

    //    }

    //}

    //// Attack
    //void Shooting()
    //{
    //    //if (EnemyTarget)
    //    // {           
    //    GameObject с = GameObject.Instantiate(bullet, shootElement.position, Quaternion.identity) as GameObject;
    //    с.GetComponent<EnemyBullet>().target = target;
    //    с.GetComponent<EnemyBullet>().twr = this;
    //    // }  

    //}



    //void GetDamage()
    //{
    //    EnemyTarget.GetComponent<TowerHP>().Dmg_2(Creature_Damage);
    //}

    void Update()
    {


        //Debug.Log("Animator  " + m_Animator);


        // MOVING

        //if (curWaypointIndex < waypoints.Length)
        //{
        //    transform.position = Vector3.MoveTowards(transform.position, waypoints[curWaypointIndex].position, Time.deltaTime * Speed);

        //    if (!EnemyTarget)
        //    {
        //        transform.LookAt(waypoints[curWaypointIndex].position);
        //    }

        //    if (Vector3.Distance(transform.position, waypoints[curWaypointIndex].position) < 0.5f)
        //    {
        //        curWaypointIndex++;
        //    }
        //}

        //else
        //{
        //    m_Animator.SetBool("Victory", true);  // Victory
        //}

        // DEATH

        if (EnemyHealth.EnemyHP <= 0)
        {
            //Speed = 0;
            Destroy(gameObject, 5f);
            m_Animator.SetBool("Death", true);
        }

        //// Attack to Run
        //if (EnemyTarget)
        //{
        //    if (EnemyTarget.CompareTag("Castle_Destroyed")) // get it from BuildingHp
        //    {
        //        m_Animator.SetBool("Attack", false);
        //        m_Animator.SetBool("RUN", true);
        //        Speed = previous_Speed;
        //        EnemyTarget = null;
        //    }
        //}


    }
}
