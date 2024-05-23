#if UNITY_EDITOR
using UnityEngine;

namespace RedeevEditor.Utilities
{
    public class FastPlacerCollider : MonoBehaviour
    {
        private SphereCollider sphereCollider;
        private int originalLayer;

        public void Activate(bool value, float radius, LayerMask layer)
        {
            if (value)
            {
                if (!sphereCollider && !TryGetComponent(out sphereCollider))
                {
                    sphereCollider = gameObject.AddComponent<SphereCollider>();
                }

                sphereCollider.radius = radius;
                sphereCollider.isTrigger = true;

                originalLayer = gameObject.layer;
                gameObject.layer = (int)Mathf.Log(layer.value, 2);
            }
            else
            {
                if (sphereCollider) DestroyImmediate(sphereCollider);

                gameObject.layer = originalLayer;
            }
        }
    }
}
#endif