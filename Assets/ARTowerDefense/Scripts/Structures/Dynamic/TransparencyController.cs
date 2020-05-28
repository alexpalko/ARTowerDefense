using UnityEngine;

namespace ARTowerDefense.Structures.Dynamic
{
    class TransparencyController : MonoBehaviour
    {
        private readonly Color m_ValidPlacementColor = new Color(0, 1, 0, .4f);
        private readonly Color m_InvalidPlacementColor = new Color(1, 0, 0, .4f);

        public void ShowValidPlacementColor()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var meshRenderer in renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.SetColor("_Color", m_ValidPlacementColor);
                }
            }
        }

        public void ShowInvalidPlacementColor()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var meshRenderer in renderers)
            {
                foreach (var material in meshRenderer.materials)
                {
                    material.SetColor("_Color", m_InvalidPlacementColor);
                }
            }
        }
    }
}
