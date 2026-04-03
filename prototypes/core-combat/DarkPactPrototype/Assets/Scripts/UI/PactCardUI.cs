using UnityEngine;
using UnityEngine.UI;

namespace DarkPact.Core
{
    public class PactCardUI : MonoBehaviour
    {
        [SerializeField] TMPro.TextMeshProUGUI _nameText;
        [SerializeField] TMPro.TextMeshProUGUI _boonText;
        [SerializeField] TMPro.TextMeshProUGUI _baneText;
        [SerializeField] Button _selectButton;
        [SerializeField] Image _border;

        PactDefinition _pact;
        System.Action<PactDefinition> _onSelected;

        public void Setup(PactDefinition pact, System.Action<PactDefinition> onSelected)
        {
            _pact = pact;
            _onSelected = onSelected;

            if (_nameText) _nameText.text = pact.PactName;
            if (_boonText) _boonText.text = $"<color=#4CAF50>NİMET:</color>\n{pact.BoonDescription}";
            if (_baneText) _baneText.text = $"<color=#F44336>BELA:</color>\n{pact.BaneDescription}";
            if (_border) _border.color = pact.PactColor;
            if (_selectButton) _selectButton.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            _onSelected?.Invoke(_pact);
        }

        void OnDestroy()
        {
            if (_selectButton) _selectButton.onClick.RemoveListener(OnClick);
        }
    }
}
