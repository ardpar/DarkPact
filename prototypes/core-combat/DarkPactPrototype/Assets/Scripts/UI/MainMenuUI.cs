using UnityEngine;
using UnityEngine.UI;

namespace DarkPact.Core
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] Button _startButton;
        [SerializeField] TMPro.TextMeshProUGUI _titleText;
        [SerializeField] TMPro.TextMeshProUGUI _subtitleText;

        void Start()
        {
            if (_startButton) _startButton.onClick.AddListener(OnStartClicked);
            if (_titleText) _titleText.text = "DARK PACT";
            if (_subtitleText) _subtitleText.text = "Şeytanla anlaş, bedelini öde";
        }

        void OnStartClicked()
        {
            if (ServiceLocator.TryGet<GameManager>(out var gm))
                gm.LoadGameplayScene();
        }
    }
}
