using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] Transform _cardContainer;
        [SerializeField] GameObject _pactCardPrefab;

        [Header("Timing")]
        [SerializeField] float _overlayFadeDuration = 0.5f;
        [SerializeField] float _cardAppearDelay = 0.3f;

        enum UIState { Hidden, Appearing, Selecting, Selected }
        UIState _state = UIState.Hidden;

        readonly List<GameObject> _spawnedCards = new();

        bool _subscribed;

        void OnEnable()
        {
            if (!_subscribed)
            {
                RunManager.OnRunStateChanged += OnRunStateChanged;
                _subscribed = true;
            }
            if (_overlay) _overlay.SetActive(false);
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
            gameObject.SetActive(true);

            Time.timeScale = 0f;

            ClearCards();

            // Get 3 pact options
            PactDefinition[] options = null;
            if (ServiceLocator.TryGet<PactManager>(out var pact))
                options = pact.GetRandomPactOptions(3);

            if (options == null || options.Length == 0)
            {
                // No pacts left, skip
                _state = UIState.Hidden;
                Time.timeScale = 1f;
                if (ServiceLocator.TryGet<RunManager>(out var run))
                    run.OnPactSelected();
                return;
            }

            // Spawn cards
            foreach (var p in options)
            {
                if (_pactCardPrefab == null || _cardContainer == null) break;
                var cardObj = Instantiate(_pactCardPrefab, _cardContainer);
                var card = cardObj.GetComponent<PactCardUI>();
                if (card != null)
                    card.Setup(p, OnPactChosen);
                _spawnedCards.Add(cardObj);
            }

            StartCoroutine(AppearSequence());
        }

        void OnPactChosen(PactDefinition chosen)
        {
            if (_state != UIState.Selecting) return;
            _state = UIState.Selected;

            if (ServiceLocator.TryGet<PactManager>(out var pact))
                pact.ActivatePact(chosen.Id);

            StartCoroutine(SelectionSequence());
        }

        IEnumerator AppearSequence()
        {
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

            if (_flavorText)
            {
                _flavorText.gameObject.SetActive(true);
                _flavorText.text = "Karanlık bir güç seni çağırıyor...";
            }

            yield return new WaitForSecondsRealtime(_cardAppearDelay);

            // Show cards
            foreach (var card in _spawnedCards)
                card.SetActive(true);

            _state = UIState.Selecting;
        }

        IEnumerator SelectionSequence()
        {
            yield return new WaitForSecondsRealtime(0.3f);

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

            ClearCards();
            if (_overlay) _overlay.SetActive(false);
            if (_flavorText) _flavorText.gameObject.SetActive(false);

            _state = UIState.Hidden;
            Time.timeScale = 1f;

            if (ServiceLocator.TryGet<RunManager>(out var run))
                run.OnPactSelected();
        }

        void ClearCards()
        {
            foreach (var card in _spawnedCards)
                if (card != null) Destroy(card);
            _spawnedCards.Clear();
        }
    }
}
