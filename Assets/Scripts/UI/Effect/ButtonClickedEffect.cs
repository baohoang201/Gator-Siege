using Gameplay.Manager;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Effect {
    [RequireComponent(typeof(Button))]
    public class ButtonClickedEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler {
        private RectTransform rectTrans;
        private Button button;
        [SerializeField] float effectSpeed = 20f;
        [SerializeField] float effectScale = 0.85f;
        private bool isPointerDown = false;

        private void Awake() {
            rectTrans = GetComponent<RectTransform>();
            button = GetComponent<Button>();
        }

        private void Start() {
            button.transition = Selectable.Transition.None;
        }

        public void OnPointerDown(PointerEventData eventData) {
            isPointerDown = true;
            StopAllCoroutines();
            StartCoroutine(ClickedEffect());
        }

        public void OnPointerUp(PointerEventData eventData) {
            isPointerDown = false;
            SoundManager.Instance.PlaySoundFX(SoundType.Click);
        }

        IEnumerator ClickedEffect() {
            float timer = (1 - rectTrans.localScale.x) / (1 - effectScale);
            float startScale = rectTrans.localScale.x;
            float targetScale = effectScale;

            // Thu nhỏ
            while (timer < 1) {
                timer += Time.unscaledDeltaTime * effectSpeed;
                float scale = Mathf.Lerp(startScale, targetScale, timer);
                rectTrans.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            rectTrans.localScale = new Vector3(effectScale, effectScale, effectScale);

            // Đợi nhả nút
            yield return new WaitUntil(() => !isPointerDown);

            // Phóng to
            timer = 0;
            startScale = effectScale;
            targetScale = 1;

            while (timer < 1) {
                timer += Time.unscaledDeltaTime * effectSpeed;
                float scale = Mathf.Lerp(startScale, targetScale, timer);
                rectTrans.localScale = new Vector3(scale, scale, scale);
                yield return null;
            }

            rectTrans.localScale = Vector3.one;
        }
    }
}
