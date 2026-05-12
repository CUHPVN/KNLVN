using System.Collections;
using UnityEngine;

namespace KNLVN.Game
{
    /// <summary>
    /// Pooled equation popup text. 
    /// Inherits from GameUnit to work with SimplePool.
    /// </summary>
    public class EquationPopup : GameUnit
    {
        [SerializeField] private TextMesh _textMesh;
        [SerializeField] private MeshRenderer _meshRenderer;

        private Coroutine _animationCoroutine;

        private void Awake()
        {
            if (_textMesh == null) _textMesh = GetComponent<TextMesh>();
            if (_meshRenderer == null) _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Setup(string text, Font font, int fontSize)
        {
            // If already animating, stop and reset immediately
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            _textMesh.text = text;
            _textMesh.fontSize = fontSize;
            _textMesh.characterSize = 0.045f; // Balanced world-space size (approx 0.45 units tall)
            _textMesh.color = new Color(1f, 0.95f, 0.2f, 1f);

            if (font != null)
            {
                _textMesh.font = font;
                _meshRenderer.material = font.material;
            }

            _animationCoroutine = StartCoroutine(PopupRoutine());
        }

        private IEnumerator PopupRoutine()
        {
            // Reset state
            transform.localScale = Vector3.one * 0.1f;
            Vector3 startPos = transform.position;
            
            const float popDuration = 0.15f;
            const float rideDuration = 1.65f;
            const float riseHeight = 1.2f;

            // Phase 1 - Pop in
            float elapsed = 0f;
            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float s = Mathf.LerpUnclamped(0f, 1.2f, t) * (1f - 0.2f * Mathf.Max(0f, t - 0.7f));
                transform.localScale = Vector3.one * Mathf.Max(0.01f, s);
                yield return null;
            }
            transform.localScale = Vector3.one;

            // Phase 2 - Rise and fade
            elapsed = 0f;
            while (elapsed < rideDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / rideDuration;
                transform.position = startPos + new Vector3(0f, riseHeight * t, 0f);
                float alpha = Mathf.Clamp01(1f - t * 1.1f);
                _textMesh.color = new Color(1f, 0.95f, 0.2f, alpha);
                yield return null;
            }

            // Return to pool
            SimplePool.Despawn(this);
            _animationCoroutine = null;
        }

        private void OnDisable()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }
    }
}
