using ARTowerDefense;
using UnityEngine;

public class EnemyMovementTest : MonoBehaviour
{
    public float MovementSpeed;
    public float RotationSpeed;
    public GameObject Model;
    private Transform Home;

    private int m_WaypointIndex;
    public GameObject[] m_Waypoints;
    private Vector3 m_Target;

    // Start is called before the first frame update
    void Start()
    {
        m_Target = m_Waypoints[m_WaypointIndex].transform.position;
    }

    private bool rotate;

    // Update is called once per frame
    void Update()
    {
        if (m_WaypointIndex >= m_Waypoints.Length)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = m_Target - transform.position;
        transform.Translate(dir.normalized * MovementSpeed * Time.deltaTime, Space.World);
        Model.transform.LookAt(m_Target, Vector3.up);

        if (Vector3.Distance(transform.position, m_Target) <= .04f)
        {
            _GetNextWaypoint();
        }
    }

    private void _GetNextWaypoint()
    {
        m_WaypointIndex++;
        m_Target = m_Waypoints[m_WaypointIndex].transform.position;
    }
}