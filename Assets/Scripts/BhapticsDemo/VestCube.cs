using System.Collections;
using UnityEngine;

namespace BhapticsDemo
{
    [RequireComponent(typeof(MeshRenderer))]
    public class VestCube : MonoBehaviour
    {
        [SerializeField] private int motorIndex = -1;

        private MeshRenderer meshRenderer;
        private Material runtimeMaterial;
        private Color baseColor;
        private static readonly Color HighlightColor = new Color(0.2f, 0.85f, 0.35f, 1f);

        public int MotorIndex => motorIndex;

        public void Configure(int index)
        {
            motorIndex = index;
        }

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            runtimeMaterial = meshRenderer.material;
            baseColor = runtimeMaterial.color;
        }

        public void FlashHighlight(float duration = 0.2f)
        {
            StopAllCoroutines();
            StartCoroutine(HighlightRoutine(duration));
        }

        private IEnumerator HighlightRoutine(float duration)
        {
            runtimeMaterial.color = HighlightColor;
            yield return new WaitForSeconds(duration);
            runtimeMaterial.color = baseColor;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
