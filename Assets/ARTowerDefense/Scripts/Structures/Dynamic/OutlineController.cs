using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic
{
    class OutlineController: MonoBehaviour
    {
        private readonly Color m_HoverOutlineColor = Color.yellow;
        private readonly Color m_SelectedOutlineColor = Color.red;
        private readonly float m_OutlineThickness = 5;

        private MeshRenderer[] m_Renderers;

        void Start()
        {
            m_Renderers = GetComponentsInChildren<MeshRenderer>();
        }

        public void ShowHoverOutline()
        {
            // Fixes null exception thrown when placing a new structure
            if (m_Renderers == null) return; 
            foreach (var meshRenderer in m_Renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.SetColor("_OutlineColor", m_HoverOutlineColor);
                    material.SetFloat("_Outline", m_OutlineThickness);
                }
            }
        }

        public void ShowSelectedOutline()
        {
            if (m_Renderers == null) return;
            foreach (var meshRenderer in m_Renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.SetColor("_OutlineColor", m_SelectedOutlineColor);
                    material.SetFloat("_Outline", m_OutlineThickness);
                }
            }
        }

        public void HideOutline()
        {
            if (m_Renderers == null) return;
            foreach (var meshRenderer in m_Renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.SetFloat("_Outline", 0);
                }
            }
        }
    }
}
