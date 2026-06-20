using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EchoDungeon.UI
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "SampleScene";

        [Header("References")]
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private CanvasGroup titleGroup;
        [SerializeField] private CanvasGroup howToPlayGroup;
        [SerializeField] private RectTransform howToPlayPanel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button closeHowToPlayButton;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI howToPlayBodyText;

        [Header("Copy")]
        [SerializeField] private string gameTitle = "ECHO DUNGEON";
        [SerializeField] private string subtitle = "Clear rooms. Claim rewards. Keep moving.";
        [TextArea(6, 12)]
        [SerializeField] private string howToPlayText =
            "Move room by room through the dungeon.\n" +
            "Clear enemies before pushing deeper.\n" +
            "Use upgrade rooms, chests, and rewards to build momentum.\n\n" +
            "Controls\n" +
            "LMB  Shoot\n" +
            "RMB  Melee\n" +
            "Q    Potion\n" +
            "1    Speed Boost\n" +
            "2    Stop Spell\n" +
            "3    Poison Spell\n" +
            "Shift Dash\n" +
            "E    Interact\n" +
            "Esc  Pause";

        private Coroutine howToPlayRoutine;

        private void Awake()
        {
            if (menuCanvas == null)
            {
                Debug.LogWarning("MainMenuUI: Menu Canvas is not assigned.");
            }

            ApplyCopy();
            BindButtons();
            SetHowToPlayVisible(false, true);
        }

        private void Start()
        {
            if (titleGroup != null)
            {
                StartCoroutine(FadeCanvasGroup(titleGroup, 0f, 1f, 0.35f));
            }
        }

        public void StartGame()
        {
            if (string.IsNullOrWhiteSpace(gameplaySceneName))
            {
                Debug.LogWarning("MainMenuUI: Gameplay scene name is empty.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                Debug.LogError("MainMenuUI: Scene '" + gameplaySceneName + "' is not in Build Settings.");
                return;
            }

            Debug.Log("MainMenuUI: Loading gameplay scene '" + gameplaySceneName + "'.");
            SceneManager.LoadScene(gameplaySceneName);
        }

        public void OpenHowToPlay()
        {
            SetHowToPlayVisible(true, false);
        }

        public void CloseHowToPlay()
        {
            SetHowToPlayVisible(false, false);
        }

        public void QuitGame()
        {
            Debug.Log("MainMenuUI: Quit requested.");

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ApplyCopy()
        {
            if (titleText != null)
            {
                titleText.text = gameTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = subtitle;
            }

            if (howToPlayBodyText != null)
            {
                howToPlayBodyText.text = howToPlayText;
            }
        }

        private void BindButtons()
        {
            BindButton(startButton, StartGame);
            BindButton(howToPlayButton, OpenHowToPlay);
            BindButton(quitButton, QuitGame);
            BindButton(closeHowToPlayButton, CloseHowToPlay);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void SetHowToPlayVisible(bool visible, bool immediate)
        {
            if (howToPlayGroup == null || howToPlayPanel == null)
            {
                return;
            }

            if (howToPlayRoutine != null)
            {
                StopCoroutine(howToPlayRoutine);
            }

            howToPlayGroup.interactable = visible;
            howToPlayGroup.blocksRaycasts = visible;

            if (immediate)
            {
                howToPlayPanel.gameObject.SetActive(visible);
                howToPlayGroup.alpha = visible ? 1f : 0f;
                howToPlayPanel.localScale = visible ? Vector3.one : Vector3.one * 0.96f;
                return;
            }

            howToPlayPanel.gameObject.SetActive(true);
            howToPlayRoutine = StartCoroutine(AnimateHowToPlay(visible));
        }

        private IEnumerator AnimateHowToPlay(bool visible)
        {
            float duration = 0.18f;
            float elapsed = 0f;
            float startAlpha = howToPlayGroup.alpha;
            float endAlpha = visible ? 1f : 0f;
            Vector3 startScale = howToPlayPanel.localScale;
            Vector3 endScale = visible ? Vector3.one : Vector3.one * 0.96f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                howToPlayGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
                howToPlayPanel.localScale = Vector3.Lerp(startScale, endScale, eased);
                yield return null;
            }

            howToPlayGroup.alpha = endAlpha;
            howToPlayPanel.localScale = endScale;
            howToPlayPanel.gameObject.SetActive(visible);
            howToPlayRoutine = null;
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
        {
            group.alpha = from;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            group.alpha = to;
        }
    }
}
