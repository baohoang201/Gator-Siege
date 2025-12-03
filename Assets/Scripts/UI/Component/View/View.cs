using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Component.View {
    [RequireComponent(typeof(CanvasGroup))]
    public class View : MonoBehaviour {
        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 0.5f; // Thời gian fade (giây)
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Đường cong fade

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;

        private void Awake() {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) {
                Debug.LogError($"{name}: CanvasGroup component not found!");
            }
        }

        /// <summary>
        /// Hiển thị View với hiệu ứng fade-in.
        /// </summary>
        /// <param name="duration">Thời gian fade (mặc định sử dụng _fadeDuration)</param>
        /// <param name="onComplete">Callback khi hoàn thành</param>
        /// <returns>IEnumerator để chờ hoàn thành</returns>
        public IEnumerator Show(float duration = -1f, UnityAction onComplete = null) {
            // Dừng coroutine hiện tại (nếu có)
            if (_fadeCoroutine != null) {
                StopCoroutine(_fadeCoroutine);
            }

            // Sử dụng duration mặc định nếu không chỉ định
            duration = duration < 0f ? _fadeDuration : duration;

            // Kích hoạt gameObject và CanvasGroup
            gameObject.SetActive(true);
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;

            // Nếu duration <= 0, hiển thị ngay lập tức
            if (duration <= 0f) {
                _canvasGroup.alpha = 1f;
                onComplete?.Invoke();
                yield break;
            }

            // Bắt đầu fade-in
            float t = 0f;
            float startAlpha = _canvasGroup.alpha;
            while (t < duration) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, _fadeCurve.Evaluate(k));
                yield return null;
            }

            // Đảm bảo alpha = 1 khi hoàn thành
            _canvasGroup.alpha = 1f;
            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Ẩn View với hiệu ứng fade-out.
        /// </summary>
        /// <param name="duration">Thời gian fade (mặc định sử dụng _fadeDuration)</param>
        /// <param name="onComplete">Callback khi hoàn thành</param>
        /// <returns>IEnumerator để chờ hoàn thành</returns>
        public IEnumerator Hide(float duration = -1f, UnityAction onComplete = null) {
            // Dừng coroutine hiện tại (nếu có)
            if (_fadeCoroutine != null) {
                StopCoroutine(_fadeCoroutine);
            }

            // Sử dụng duration mặc định nếu không chỉ định
            duration = duration < 0f ? _fadeDuration : duration;

            // Nếu duration <= 0, ẩn ngay lập tức
            if (duration <= 0f) {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
                gameObject.SetActive(false);
                onComplete?.Invoke();
                yield break;
            }

            // Bắt đầu fade-out
            float t = 0f;
            float startAlpha = _canvasGroup.alpha;
            while (t < duration) {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, _fadeCurve.Evaluate(k));
                yield return null;
            }

            // Đảm bảo alpha = 0 khi hoàn thành
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Hiển thị ngay lập tức mà không có hiệu ứng.
        /// </summary>
        public void ShowImmediate() {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        /// <summary>
        /// Ẩn ngay lập tức mà không có hiệu ứng.
        /// </summary>
        public void HideImmediate() {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }

        private void OnEnable() {
            // Đảm bảo CanvasGroup được khởi tạo đúng
            if (_canvasGroup == null) {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void OnDisable() {
            // Dừng coroutine khi bị vô hiệu hóa
            if (_fadeCoroutine != null) {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }
    }
}