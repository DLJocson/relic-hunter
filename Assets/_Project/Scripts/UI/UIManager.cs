using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RelicHunter.Core;

namespace RelicHunter.UI
{
    /// <summary>
    /// Main menu, HUD, round-start, and game-over UI state and text updates.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        private const string MainMenuBottomBarPlaceholder = "Press Play to begin";
        private const string GameOverBottomBarPlaceholder = "Click Restart to play again";
        private const float GameOverRestartButtonWidth = 200f;
        private const string CanvasObjectName = "GameUICanvas";

        [Header("UI Panels (State Management)")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject roundStartPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Persistent HUD Chrome")]
        [SerializeField] private GameObject topBarPanel;
        [SerializeField] private GameObject bottomBarPanel;
        [SerializeField] private GameObject scorePanel;

        [Header("Round Winner Graphics (HUD)")]
        [SerializeField] private GameObject roundWinnerPanel;
        [SerializeField] private GameObject roundPlayerWinGraphic;
        [SerializeField] private GameObject roundGuardWinGraphic;

        [Header("Match Game Over Graphics")]
        [SerializeField] private GameObject playerWinGraphic;
        [SerializeField] private GameObject guardWinGraphic;
        [SerializeField] private Button restartButton;
        [SerializeField] private Sprite restartButtonSprite;

        [Header("HUD Text References")]
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI playerScoreText;
        [SerializeField] private TextMeshProUGUI guardScoreText;
        [SerializeField] private TextMeshProUGUI barricadeText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private TextMeshProUGUI instructionText;

        private Transform uiCanvasRoot;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            EnsureUiBindings();

            if (playerScoreText != null) playerScoreText.text = "0";
            if (guardScoreText != null) guardScoreText.text = "0";

            ShowMainMenu();
        }

        /// <summary>
        /// Repairs hierarchy and references when bars are still under HUDPanel
        /// or score TMP is covered by legacy child Images.
        /// </summary>
        private void EnsureUiBindings()
        {
            GameObject canvasObject = GameObject.Find(CanvasObjectName);
            if (canvasObject == null)
                return;

            uiCanvasRoot = canvasObject.transform;

            mainMenuPanel ??= FindChildGameObject(uiCanvasRoot, "MainMenuPanel");
            roundStartPanel ??= FindChildGameObject(uiCanvasRoot, "RoundStartPanel");
            hudPanel ??= FindChildGameObject(uiCanvasRoot, "HUDPanel");
            gameOverPanel ??= FindChildGameObject(uiCanvasRoot, "GameOverPanel");
            topBarPanel ??= FindChildGameObject(uiCanvasRoot, "TopBarPanel");
            bottomBarPanel ??= FindChildGameObject(uiCanvasRoot, "BottomBarPanel");
            roundWinnerPanel ??= FindChildGameObject(uiCanvasRoot, "Round Winner");

            if (topBarPanel != null && hudPanel != null && topBarPanel.transform.IsChildOf(hudPanel.transform))
                topBarPanel.transform.SetParent(uiCanvasRoot, false);

            if (bottomBarPanel != null && hudPanel != null && bottomBarPanel.transform.IsChildOf(hudPanel.transform))
                bottomBarPanel.transform.SetParent(uiCanvasRoot, false);

            if (roundWinnerPanel != null && hudPanel != null && !roundWinnerPanel.transform.IsChildOf(hudPanel.transform))
                roundWinnerPanel.transform.SetParent(hudPanel.transform, false);

            if (topBarPanel != null)
                topBarPanel.transform.SetAsFirstSibling();

            if (bottomBarPanel != null)
                bottomBarPanel.transform.SetAsFirstSibling();

            scorePanel ??= topBarPanel != null
                ? FindChildGameObject(topBarPanel.transform, "ScorePanel")
                : null;

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 100;

            ResolveHudTextReferences();
            ConfigureTopBarLayout();
            ConfigureScoreboardLayout();
            EnsureGameOverRestartButton();
            SetScoreboardVisible(false);
        }

        private void ResolveHudTextReferences()
        {
            if (topBarPanel == null)
                return;

            Transform scorePanel = topBarPanel.transform.Find("ScorePanel");
            Transform currentRound = topBarPanel.transform.Find("CurrentRound");

            if (roundText == null && currentRound != null)
                roundText = currentRound.GetComponent<TextMeshProUGUI>();

            if (scorePanel != null)
            {
                if (playerScoreText == null)
                {
                    Transform player = scorePanel.Find("PlayerScoreText");
                    if (player != null)
                        playerScoreText = ResolveScoreTextComponent(player);
                }

                if (guardScoreText == null)
                {
                    Transform guard = scorePanel.Find("GuardScoreText");
                    if (guard != null)
                        guardScoreText = ResolveScoreTextComponent(guard);
                }
            }

            if (bottomBarPanel != null && barricadeText == null)
            {
                Transform barricade = bottomBarPanel.transform.Find("BarricadeCounter");
                if (barricade != null)
                    barricadeText = barricade.GetComponent<TextMeshProUGUI>();
            }

            playerScoreText = PrepareScoreText(playerScoreText);
            guardScoreText = PrepareScoreText(guardScoreText);
        }

        private static TextMeshProUGUI ResolveScoreTextComponent(Transform container)
        {
            Transform textChild = container.Find("Text");
            if (textChild != null)
            {
                TextMeshProUGUI childText = textChild.GetComponent<TextMeshProUGUI>();
                if (childText != null)
                    return childText;
            }

            return container.GetComponent<TextMeshProUGUI>();
        }

        private static TextMeshProUGUI PrepareScoreText(TextMeshProUGUI scoreText)
        {
            if (scoreText == null)
                return null;

            Transform container = scoreText.transform;
            if (container.name == "Text" && container.parent != null)
                container = container.parent;

            string currentText = scoreText.text;
            TMP_FontAsset sourceFont = scoreText.font;
            Material sourceMaterial = scoreText.fontSharedMaterial;

            Transform textTransform = container.Find("Text");
            TextMeshProUGUI textComponent;

            if (textTransform == null)
            {
                var textObject = new GameObject("Text", typeof(RectTransform));
                textObject.transform.SetParent(container, false);
                textTransform = textObject.transform;
                textComponent = textObject.AddComponent<TextMeshProUGUI>();
                textComponent.text = currentText;
                if (sourceFont != null)
                {
                    textComponent.font = sourceFont;
                    textComponent.fontSharedMaterial = sourceMaterial;
                }

                TextMeshProUGUI parentText = container.GetComponent<TextMeshProUGUI>();
                if (parentText != null)
                    parentText.enabled = false;
            }
            else
            {
                textComponent = textTransform.GetComponent<TextMeshProUGUI>();
                if (textComponent == null)
                    textComponent = textTransform.gameObject.AddComponent<TextMeshProUGUI>();

                TextMeshProUGUI parentText = container.GetComponent<TextMeshProUGUI>();
                if (parentText != null && parentText != textComponent)
                    parentText.enabled = false;
            }

            Transform imageTransform = container.Find("Image");
            if (imageTransform != null)
            {
                imageTransform.gameObject.SetActive(true);
                imageTransform.SetAsFirstSibling();

                Image backdrop = imageTransform.GetComponent<Image>();
                if (backdrop != null)
                {
                    backdrop.raycastTarget = false;
                    backdrop.sprite = null;
                    backdrop.color = new Color32(92, 68, 33, 255);
                }
            }

            container.gameObject.SetActive(true);

            textComponent.gameObject.SetActive(true);
            textComponent.enabled = true;
            textComponent.text = currentText;
            if (sourceFont != null && textComponent.font == null)
            {
                textComponent.font = sourceFont;
                textComponent.fontSharedMaterial = sourceMaterial;
            }
            textComponent.raycastTarget = false;

            RectTransform textRect = textTransform as RectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textTransform.SetAsLastSibling();
            container.SetAsLastSibling();

            return textComponent;
        }

        private void ConfigureTopBarLayout()
        {
            if (topBarPanel == null)
                return;

            RectTransform topRect = topBarPanel.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0f, 80f);

            Transform currentRound = topBarPanel.transform.Find("CurrentRound");
            if (currentRound is RectTransform roundRect)
            {
                roundRect.anchorMin = Vector2.zero;
                roundRect.anchorMax = Vector2.one;
                roundRect.offsetMin = Vector2.zero;
                roundRect.offsetMax = Vector2.zero;
            }
        }

        private void ConfigureScoreboardLayout()
        {
            if (topBarPanel == null)
                return;

            Transform scorePanel = topBarPanel.transform.Find("ScorePanel");
            RectTransform scoreRect = scorePanel as RectTransform;
            if (scoreRect == null)
                return;

            if (this.scorePanel != null)
            {
                Image rootImage = this.scorePanel.GetComponent<Image>();
                if (rootImage != null)
                    rootImage.enabled = true;
            }
        }

        private void SetScoreboardVisible(bool visible)
        {
            if (scorePanel == null && topBarPanel != null)
                scorePanel = FindChildGameObject(topBarPanel.transform, "ScorePanel");

            if (scorePanel != null)
                scorePanel.SetActive(visible);
        }

        private static GameObject FindChildGameObject(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            foreach (Transform transform in parent.GetComponentsInChildren<Transform>(true))
            {
                if (transform != parent && transform.name == childName)
                    return transform.gameObject;
            }

            return null;
        }

        private void SetHudChromeVisible(bool visible)
        {
            if (topBarPanel != null) topBarPanel.SetActive(visible);
            if (bottomBarPanel != null) bottomBarPanel.SetActive(visible);
        }

        private void SetBottomBarPlaceholder(string message)
        {
            if (barricadeText != null)
                barricadeText.text = message;
        }

        private void SetRestartButtonVisible(bool visible)
        {
            if (restartButton != null)
                restartButton.gameObject.SetActive(visible);
        }

        private void EnsureGameOverRestartButton()
        {
            if (gameOverPanel == null)
                return;

            Transform existing = gameOverPanel.transform.Find("Restart");
            if (existing != null)
            {
                restartButton ??= existing.GetComponent<Button>();
                ApplyRestartButtonLayout(existing.GetComponent<RectTransform>(), existing.GetComponent<Image>());
                if (restartButton != null)
                    WireRestartButton(restartButton);
                return;
            }

            var restartObject = new GameObject("Restart", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            restartObject.transform.SetParent(gameOverPanel.transform, false);
            restartObject.layer = gameOverPanel.layer;

            RectTransform restartRect = restartObject.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.5f, 0.5f);
            restartRect.anchorMax = new Vector2(0.5f, 0.5f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.anchoredPosition = new Vector2(0f, -170f);

            Image restartImage = restartObject.GetComponent<Image>();
            restartImage.raycastTarget = true;
            ApplyRestartButtonLayout(restartRect, restartImage);

            restartButton = restartObject.GetComponent<Button>();
            restartButton.targetGraphic = restartImage;
            WireRestartButton(restartButton);

            restartObject.SetActive(false);
            restartObject.transform.SetAsLastSibling();
        }

        private void ApplyRestartButtonLayout(RectTransform restartRect, Image restartImage)
        {
            if (restartRect == null)
                return;

            restartRect.anchoredPosition = new Vector2(0f, -170f);

            if (restartImage == null)
                return;

            Sprite sprite = restartButtonSprite != null ? restartButtonSprite : restartImage.sprite;
            if (sprite != null)
            {
                restartImage.sprite = sprite;
                restartImage.color = Color.white;
                restartImage.preserveAspect = true;
                float scale = GameOverRestartButtonWidth / sprite.rect.width;
                restartRect.sizeDelta = sprite.rect.size * scale;
                return;
            }

            restartImage.sprite = null;
            restartImage.color = new Color(0.2f, 0.55f, 0.25f, 1f);
            restartRect.sizeDelta = new Vector2(GameOverRestartButtonWidth, 48f);
        }

        private void WireRestartButton(Button button)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(HandleRestartClicked);
            button.onClick.AddListener(HandleRestartClicked);
        }

        private static void HandleRestartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.RestartMatch();
        }

        private void BringHudChromeToFront()
        {
            if (topBarPanel != null)
                topBarPanel.transform.SetAsLastSibling();

            if (bottomBarPanel != null)
                bottomBarPanel.transform.SetAsLastSibling();
        }

        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (roundStartPanel != null) roundStartPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (roundWinnerPanel != null) roundWinnerPanel.SetActive(false);

            SetHudChromeVisible(true);
            SetScoreboardVisible(false);
            SetRestartButtonVisible(false);

            if (roundText != null)
                roundText.text = "RELIC HUNTER";

            SetBottomBarPlaceholder(MainMenuBottomBarPlaceholder);
        }

        public void ShowRoundStart()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (roundStartPanel != null) roundStartPanel.SetActive(true);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (roundWinnerPanel != null) roundWinnerPanel.SetActive(false);

            SetHudChromeVisible(true);
            SetScoreboardVisible(false);
            SetRestartButtonVisible(false);
            UpdateScore(0, 0);
        }

        public void ShowHUD()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (roundStartPanel != null) roundStartPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(true);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            SetHudChromeVisible(true);
            SetScoreboardVisible(true);
            SetRestartButtonVisible(false);

            if (roundWinnerPanel != null) roundWinnerPanel.SetActive(false);
        }

        public void ShowRoundWinner(bool playerWon)
        {
            if (roundWinnerPanel != null) roundWinnerPanel.SetActive(true);

            if (roundPlayerWinGraphic != null && roundGuardWinGraphic != null)
            {
                roundPlayerWinGraphic.SetActive(playerWon);
                roundGuardWinGraphic.SetActive(!playerWon);
            }
        }

        public void ShowGameOver(bool playerWon)
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (roundStartPanel != null) roundStartPanel.SetActive(false);
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            if (roundWinnerPanel != null) roundWinnerPanel.SetActive(false);

            SetHudChromeVisible(true);
            SetScoreboardVisible(true);
            SetRestartButtonVisible(true);

            if (roundText != null)
                roundText.text = "MATCH COMPLETE";

            SetBottomBarPlaceholder(GameOverBottomBarPlaceholder);
            BringHudChromeToFront();

            if (playerWinGraphic != null && guardWinGraphic != null)
            {
                playerWinGraphic.SetActive(playerWon);
                guardWinGraphic.SetActive(!playerWon);
            }
        }

        public void UpdateScore(int playerWins, int guardWins)
        {
            if (playerScoreText != null) playerScoreText.text = playerWins.ToString();
            if (guardScoreText != null) guardScoreText.text = guardWins.ToString();
        }

        public void UpdateRound(int roundNumber, string difficulty, int width, int height, float guardSpeed, int barricadeDuration)
        {
            if (roundText != null)
            {
                roundText.text = $"ROUND {roundNumber} ({difficulty.ToUpper()})\n<size=60%>Map: {width}x{height} | Guard Speed: {guardSpeed}s | Barricade Life: {barricadeDuration} Turns</size>";
            }
        }

        public void UpdateBarricades(int currentPlaced, int maxAllowed)
        {
            if (barricadeText != null)
            {
                int remaining = maxAllowed - currentPlaced;
                barricadeText.text = $"Barriers Placeable: {remaining}/{maxAllowed}";
            }
        }

        public void UpdateTurnNotice(TurnManager.TurnState activeTurn)
        {
            if (turnText == null) return;

            switch (activeTurn)
            {
                case TurnManager.TurnState.PlayerTurn:
                    turnText.text = "<color=#00FF99>YOUR TURN</color>";
                    break;
                case TurnManager.TurnState.GuardTurn:
                    turnText.text = "<color=#FFCC00>GUARD IS MOVING...</color>";
                    break;
                case TurnManager.TurnState.Processing:
                    turnText.text = "<color=#999999>PROCESSING...</color>";
                    break;
            }
        }

    }
}
