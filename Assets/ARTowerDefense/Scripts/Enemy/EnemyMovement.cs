using System.Collections.Generic;
using System.Linq;
using ARTowerDefense;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public EnemyHealth Health;
    public float MovementSpeed;
    public Queue<float> MovementDebuffQueue { get; set; }

    private int m_WaypointIndex;
    private Transform[] m_Waypoints;
    private Vector3 m_Target;

    void Start()
    {
        m_Waypoints = Master.PathWaypoints;
        if (m_Waypoints == null)
        {
            Debug.LogError("Enemy spawned before the path was generated. It will be destroyed.");
            Destroy(gameObject);
            return;
        }

        m_Target = m_Waypoints[m_WaypointIndex].position;
        MovementDebuffQueue = new Queue<float>();
    }

    void Update()
    {
        if (Health.EnemyHP <= 0)
        {
            return;
        }

        if (m_WaypointIndex >= m_Waypoints.Length)
        {
            Master.EnemyReachedBase = true;
            return;
        }

        Vector3 dir = m_Target - transform.position;
        transform.Translate(dir.normalized * (MovementSpeed - MovementDebuffQueue.FirstOrDefault() * MovementSpeed) * Time.deltaTime, Space.World);
        
        if (Vector3.Distance(transform.position, m_Target) <= .04f)
        {
            _GetNextWaypoint();
            transform.LookAt(m_Target);
        }
    }

    private void _GetNextWaypoint()
    {
        m_WaypointIndex++;
        if (m_WaypointIndex < m_Waypoints.Length)
        {
            m_Target = m_Waypoints[m_WaypointIndex].position;
        }

    }
}
