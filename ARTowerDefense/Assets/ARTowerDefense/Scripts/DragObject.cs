using UnityEngine;

public class DragObject : MonoBehaviour
{
    private Vector3 m_TouchPosition;
    private Rigidbody m_Rigidbody;
    private Vector3 m_Direction;

    // Start is called before the first frame update
    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            m_TouchPosition = Camera.main.ScreenToWorldPoint(touch.position);
            m_TouchPosition.y = 0;
            m_Direction = (m_TouchPosition - transform.position);
            m_Rigidbody.velocity = new Vector3(m_Direction.x, m_Direction.y, m_Direction.z);

            if (touch.phase == TouchPhase.Ended)
            {
                m_Rigidbody.velocity = Vector3.zero;
            }
        }
    }
}
