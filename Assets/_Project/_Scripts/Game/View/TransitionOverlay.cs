using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

namespace KNLVN.Game
{
    /// <summary>
    /// Procedural singleton overlay for smooth black screen transitions.
    /// Survives scene changes automatically.
    /// </summary>
    public class TransitionOverlay : MonoBehaviour
    {
        private static TransitionOverlay _instance;
        public static TransitionOverlay Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("TransitionOverlay");
                    _instance = go.AddComponent<TransitionOverlay>();
                    DontDestroyOnLoad(go);
                    _instance.Init();
                }
                return _instance;
            }
        }

        private Image _blackScreen;

        private void Init()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // Always on top
            
            var go = new GameObject("BlackScreen");
            go.transform.SetParent(transform, false);
            _blackScreen = go.AddComponent<Image>();
            _blackScreen.color = new Color(0, 0, 0, 0); // start clear
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            
            _blackScreen.raycastTarget = false;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Always fade to clear when a new scene finishes loading
            FadeToClear();
        }

        public bool IsTransitioning { get; private set; }

        public void PlayTransition(Action onMidpoint)
        {
            if (IsTransitioning) return;
            IsTransitioning = true;
            StartCoroutine(TransitionRoutine(onMidpoint));
        }

        private IEnumerator TransitionRoutine(Action onMidpoint)
        {
            _blackScreen.raycastTarget = true; // Block clicks

            // Fade to black
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, t / 0.25f));
                yield return null;
            }
            _blackScreen.color = Color.black;

            // Trigger actual scene/level load
            try { onMidpoint?.Invoke(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TransitionOverlay] onMidpoint threw: {e.Message}");
            }

            // Wait a tiny bit for processing
            yield return new WaitForSeconds(0.1f);

            // Fade to clear
            t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / 0.25f));
                yield return null;
            }
            _blackScreen.color = new Color(0, 0, 0, 0);
            _blackScreen.raycastTarget = false;
            IsTransitioning = false;
        }

        public void FadeToBlack(Action onComplete)
        {
            StartCoroutine(FadeToBlackRoutine(onComplete));
        }

        private IEnumerator FadeToBlackRoutine(Action onComplete)
        {
            _blackScreen.raycastTarget = true;
            float t = 0;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, t / 0.25f));
                yield return null;
            }
            _blackScreen.color = Color.black;
            try { onComplete?.Invoke(); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TransitionOverlay] onComplete threw: {e.Message}");
            }
        }

        public void FadeToClear()
        {
            StartCoroutine(FadeToClearRoutine());
        }

        private IEnumerator FadeToClearRoutine()
        {
            // Give the engine a frame or two to render the newly loaded scene
            yield return null;
            yield return null;

            float t = 0;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _blackScreen.color = new Color(0, 0, 0, Mathf.Lerp(1, 0, t / 0.35f));
                yield return null;
            }
            _blackScreen.color = new Color(0, 0, 0, 0);
            _blackScreen.raycastTarget = false;
        }
    }
}
