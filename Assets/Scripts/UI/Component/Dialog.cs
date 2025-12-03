using Gameplay.Manager;
using System;
using System.Collections;
using UnityEngine;

namespace UI.Component {
    [RequireComponent(typeof(CanvasGroup))]
    public class Dialog : MonoBehaviour {
        [SerializeField] bool hasEffect;
        [SerializeField] AnimationCurve openAnimCurve;
        [SerializeField] float effectTime;
        [SerializeField] float scaleFactor;
        [SerializeField] RectTransform effectedRect;

        protected CanvasGroup canvasGroup;

        public event Action OnShown;
        public event Action OnHidden;

        protected virtual void Awake() {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Start() {
            if (DialogManager.Instance != null) {
                DialogManager.Instance.RegisterDialog(this);
            }
        }

        public virtual void Show() {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            Time.timeScale = 0f;
            OnShown?.Invoke();

            if (!hasEffect || effectedRect == null) return;
            StartCoroutine(OpenEffect());
        }

        public virtual void Hide() {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            Time.timeScale = 1f;
            OnHidden?.Invoke();
        }

        private IEnumerator OpenEffect() {
            float timer = 0;

            while (timer < effectTime) {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / effectTime);
                float scale = openAnimCurve.Evaluate(t) * scaleFactor;
                effectedRect.localScale = new Vector3(scale, scale, scale);

                yield return null;
            }

            effectedRect.localScale = Vector3.one;
        }
    }
}
