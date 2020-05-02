using UnityEngine;

namespace Assets.ARTowerDefense
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
