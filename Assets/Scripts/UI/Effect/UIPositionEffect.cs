using UnityEngine;

namespace UI.Effect {
    [RequireComponent(typeof(RectTransform))]
    public class UIPositionEffect : MonoBehaviour {
        [SerializeField] AnimationCurve curve;
        [SerializeField] float moveAmount = 10f;
        [SerializeField] float circleTime = 1f;

        RectTransform rectTransform;
        Vector3 initialPosition;
        float timer = 0f;

        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
            initialPosition = rectTransform.anchoredPosition;
        }

        private void Update() {
            timer += Time.unscaledDeltaTime;
            if (timer > circleTime) timer = 0f;

            float offsetY = curve.Evaluate(timer / circleTime) * moveAmount;
            rectTransform.anchoredPosition = initialPosition + new Vector3(0f, offsetY);
        }
    }
}
