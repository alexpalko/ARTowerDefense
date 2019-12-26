using ARTowerDefense;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    public float MovementSpeed;

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
    }

    void Update()
    {
        if (m_WaypointIndex >= m_Waypoints.Length)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = m_Target - transform.position;
        transform.Translate(dir.normalized * MovementSpeed * Time.deltaTime, Space.World);
        
        if (Vector3.Distance(transform.position, m_Target) <= .04f)
        {
            _GetNextWaypoint();
            transform.LookAt(m_Target);
        }
    }

    private void _GetNextWaypoint()
    {
        m_WaypointIndex++;
        m_Target = m_Waypoints[m_WaypointIndex].position;
    }
}
