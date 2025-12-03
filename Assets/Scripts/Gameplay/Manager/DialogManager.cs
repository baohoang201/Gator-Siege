using System.Collections.Generic;
using UI.Component;
using UnityEngine;

namespace Gameplay.Manager {
    public class DialogManager : MonoBehaviour {
        public static DialogManager Instance;

        private List<Dialog> dialogs = new List<Dialog>();

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
            }
        }

        public void RegisterDialog(Dialog dialog) {
            dialogs.Add(dialog);
        }

        public void ShowDialog(Dialog dialog) {
            foreach (Dialog tmp in dialogs) {
                if (tmp != dialog) {
                    tmp.Hide();
                }
            }

            dialog.Show();
        }

        public List<Dialog> GetDialogs() => dialogs;
    }
}