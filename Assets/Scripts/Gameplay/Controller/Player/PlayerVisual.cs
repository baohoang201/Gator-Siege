using UnityEngine;

namespace Gameplay.Controller.Player {
    public class PlayerVisual : MonoBehaviour {
        [SerializeField] private Animator _animator;

        public void SetMoving(bool isMoving) {
            _animator.SetBool("IsMoving", isMoving);
        }
    }
}