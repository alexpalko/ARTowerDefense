using System;
using UnityEngine;

public class TorusCage : MonoBehaviour
{
    public Transform TorusCageTransform;
    private float m_ChangeVerticalDirCooldown;
    private float m_BeforeChangeCooldown;
    private Vector3 m_VerticalMoveDirection = Vector3.up * .1f;

    void Update()
    {
        TorusCageTransform.Rotate(5 * Time.deltaTime, -5 * Time.deltaTime, 5 * Time.deltaTime);

        if (m_ChangeVerticalDirCooldown > 1.5)
        {
            m_ChangeVerticalDirCooldown = 0;
            m_VerticalMoveDirection = -m_VerticalMoveDirection;
            m_BeforeChangeCooldown = .1f;
        }

        if (m_BeforeChangeCooldown > 0)
        {
            m_BeforeChangeCooldown -= Time.deltaTime;
        }
        else
        {
            transform.Translate(m_VerticalMoveDirection * Time.deltaTime);
            m_ChangeVerticalDirCooldown += Time.deltaTime;
        }
    }
}
