using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DarkPact.Core
{
    public class PactSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] GameObject _overlay;
        [SerializeField] CanvasGroup _overlayCanvasGroup;
        [SerializeField] TMPro.TextMeshProUGUI _flavorText;
        [SerializeField] GameObject _cardContainer;
        [SerializeField] Button _selectButton;

        [Header("Card Display")]
        [SerializeField] TMPro.TextMeshProUGUI _pactNameText;
        [SerializeField] TMPro.TextMeshProUGUI _boonText;
        [SerializeField] TMPro.TextMeshProUGUI _baneText;

        [Header("Timing")]
        [SerializeField] float _overlayFadeDuration = 0.5f;
        [SerializeField] float _cardAppearDelay = 0.3f;

        enum UIState { Hidden, Appearing, Selecting, Selected }
        UIState _state = UIState.Hidden;

        void Start()
        {
            if (_overlay) _overlay.SetActive(false);
            if (_cardContainer) _cardContainer.SetActive(false);
            if (_selectButton) _selectButton.onClick.AddListener(OnSelectClicked);

            RunManager.OnRunStateChanged += OnRunStateChanged;
        }

        void OnDestroy()
        {
            RunManager.OnRunStateChanged -= OnRunStateChanged;
        }

        void OnRunStateChanged(RunState oldState, RunState newState)
        {
            if (newState == RunState.PactSelection)
                Show();
        }

        void Show()
        {
            if (_state != UIState.Hidden) return;
            _state = UIState.Appearing;

            // Pause gameplay
            Time.timeScale = 0f;

            // Setup card content — MVP: just Katliam Paktı
            if (_pactNameText) _pactNameText.text = "Katliam Paktı";
            if (_boonText) _boonText.text = "<color=#4CAF50>BOON:</color> +%60 hasar";
            if (_baneText) _baneText.text = "<color=#F44336>BANE:</color> Düşmanlar bir kez dirilir";

            StartCoroutine(AppearSequence());
        }

        IEnumerator AppearSequence()
        {
            // Show overlay with fade
            if (_overlay) _overlay.SetActive(true);

            if (_overlayCanvasGroup)
            {
                _overlayCanvasGroup.alpha = 0f;
                float t = 0f;
                while (t < _overlayFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    _overlayCanvasGroup.alpha = Mathf.Clamp01(t / _overlayFadeDuration);
                    yield return null;
                }
            }

            // Show flavor text
            if (_flavorText)
            {
                _flavorText.gameObject.SetActive(true);
                _flavorText.text = "Karanlık bir güç seni çağırıyor...";
            }

            yield return new WaitForSecondsRealtime(_cardAppearDelay);

            // Show card
            if (_cardContainer) _cardContainer.SetActive(true);

            _state = UIState.Selecting;
        }

        void OnSelectClicked()
        {
            if (_state != UIState.Selecting) return;
            _state = UIState.Selected;

            // Activate pact
            if (ServiceLocator.TryGet<PactManager>(out var pact))
                pact.ActivateKatliamPact();

            StartCoroutine(SelectionSequence());
        }

        IEnumerator SelectionSequence()
        {
            // Flash effect
            if (_pactNameText) _pactNameText.color = Color.yellow;

            yield return new WaitForSecondsRealtime(0.5f);

            // Fade out overlay
            if (_overlayCanvasGroup)
            {
                float t = 0f;
                while (t < _overlayFadeDuration)
                {
                    t += Time.unscaledDeltaTime;
                    _overlayCanvasGroup.alpha = 1f - Mathf.Clamp01(t / _overlayFadeDuration);
                    yield return null;
                }
            }

            // Hide everything
            if (_overlay) _overlay.SetActive(false);
            if (_cardContainer) _cardContainer.SetActive(false);
            if (_flavorText) _flavorText.gameObject.SetActive(false);
            if (_pactNameText) _pactNameText.color = Color.white;

            _state = UIState.Hidden;

            // Resume gameplay
            Time.timeScale = 1f;

            // Notify RunManager
            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.OnPactSelected();
        }

        // Keyboard shortcut for prototype
        void Update()
        {
            if (_state == UIState.Selecting && Input.GetKeyDown(KeyCode.Return))
                OnSelectClicked();
        }
    }
}
