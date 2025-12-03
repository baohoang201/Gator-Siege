using TMPro;
using UnityEngine;

namespace UI.Component.View {
    public class WinWaveView : Dialog {
        [SerializeField] TMP_Text coinRewardText;

        public void SetCoin(string coinText) {
            coinRewardText.text = coinText;
        }
    }
}