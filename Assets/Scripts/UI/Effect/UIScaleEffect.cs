using UnityEngine;

namespace UI.Effect {
    [RequireComponent(typeof(RectTransform))]
    public class UIScaleEffect : MonoBehaviour {
        [SerializeField] AnimationCurve curve;
        [SerializeField] float scaleFactor = 1f;
        [SerializeField] float circleTime;

        RectTransform rectTransform;
        float timer = 0f;

        private void Awake() {
            rectTransform = GetComponent<RectTransform>();
        }

        private void Update() {
            timer += Time.deltaTime;
            if (timer > circleTime) timer = 0f;

            float scale = curve.Evaluate(timer / circleTime) * scaleFactor;
            rectTransform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
