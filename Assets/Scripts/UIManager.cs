using Photon.Pun; // GameManager.GameState enum 참조를 위해 (직접 사용하지 않더라도 GameManager를 참조하므로)
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject pollCreationPanel;
    public GameObject waitingPanel;
    public GameObject votingPanel;
    public GameObject resultsPanel;
    public GameObject popupMessagePanel;
    public GameObject directionButtons;
    public GameObject optionSigns;

    [Header("UI Buttons")]
    public Button cancelCreatePollButton; // "방 만들기" 시나리오에서 사용될 취소 버튼

    [Header("UI Text Elements")]
    public TMP_Text waitingStatusText; // WaitingPanel에 표시될 메인 상태 텍스트
    public TMP_Text votingPollItemAText;
    public TMP_Text votingPollItemBText;
    public TMP_Text votingTimerText;
    public TMP_Text resultsText;
    public TMP_Text popupMessageText;

    [Header("Sound Effects")]
    public AudioClip uiButtonClickSound;
    private AudioSource uiAudioSource;

    private float currentVoteTimer = 0f;
    private bool isVotingTimerRunning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 필요에 따라
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        uiAudioSource = GetComponent<AudioSource>();
        uiAudioSource.playOnAwake = false;
    }

    void Start()
    {
        if (cancelCreatePollButton != null)
        {
            if (GameManager.Instance != null)
            {
                cancelCreatePollButton.onClick.AddListener(() => GameManager.Instance.HandleCancelWaitingOrCreation());
            }
            else
            {
                Debug.LogError("UIManager: GameManager.Instance is null in Start(). Cannot add listener for cancel button.");
            }
            cancelCreatePollButton.onClick.AddListener(PlayButtonClickSound);
            cancelCreatePollButton.gameObject.SetActive(false);
        }

        // 다른 UI 버튼(메인 메뉴, 투표 생성 확인 버튼 등)의 사운드 리스너를 여기에 추가할 수 있습니다.
    }

    public void PlayButtonClickSound()
    {
        if (uiButtonClickSound != null && uiAudioSource != null)
        {
            uiAudioSource.PlayOneShot(uiButtonClickSound);
        }
        // else Debug.LogWarning("UIManager: uiButtonClickSound 또는 uiAudioSource가 할당되지 않았습니다.");
    }

    void Update()
    {
        if (isVotingTimerRunning && currentVoteTimer > 0)
        {
            currentVoteTimer -= Time.deltaTime;
            if (votingTimerText != null) votingTimerText.text = $"남은 시간: {Mathf.CeilToInt(currentVoteTimer)}초";
            if (currentVoteTimer <= 0)
            {
                isVotingTimerRunning = false;
                if (votingTimerText != null) votingTimerText.text = "시간 종료!";
            }
        }
    }

    public void UpdateUIForState(GameManager.GameState state, string itemA = "", string itemB = "")
    {
        HideAllGamePanels();

        switch (state)
        {
            case GameManager.GameState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case GameManager.GameState.CreatingPoll:
                if (pollCreationPanel != null) pollCreationPanel.SetActive(true);
                break;
            case GameManager.GameState.WaitingForPlayer:
                if (waitingPanel != null) waitingPanel.SetActive(true);
                // GameManager의 UpdateStatus 함수가 직접 uiManager.waitingStatusText.text 를 업데이트합니다.
                // 따라서 여기서 GameManager.Instance.statusText (이제는 존재하지 않음)를 복사할 필요가 없습니다.
                // 만약 waitingStatusText에 초기 메시지를 설정하고 싶다면 여기서 할 수 있지만,
                // 보통 GameManager.UpdateStatus가 상태 변경 직후 호출되므로 괜찮습니다.
                break;
            case GameManager.GameState.Voting:
                if (!PhotonNetwork.IsMasterClient && directionButtons) directionButtons.SetActive(true);
                if (votingPanel != null) votingPanel.SetActive(true);
                if (optionSigns != null) optionSigns.SetActive(true);
                if (votingPollItemAText != null) votingPollItemAText.text = itemA;
                if (votingPollItemBText != null) votingPollItemBText.text = itemB;
                break;
            case GameManager.GameState.Results:
                if (resultsPanel != null) resultsPanel.SetActive(true);
                break;
        }
    }

    public void HideAllGamePanels()
    {
        if (optionSigns != null) optionSigns.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pollCreationPanel != null) pollCreationPanel.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (votingPanel != null) votingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (popupMessagePanel != null) popupMessagePanel.SetActive(false);
        if (directionButtons != null) directionButtons.SetActive(false);

        if (cancelCreatePollButton != null)
        {
            cancelCreatePollButton.gameObject.SetActive(false);
        }
    }

    public void SetCancelCreatePollButtonActive(bool isActive)
    {
        if (cancelCreatePollButton != null)
        {
            cancelCreatePollButton.gameObject.SetActive(isActive);
        }
    }

    public void StartVoteTimerUI(float duration)
    {
        currentVoteTimer = duration;
        isVotingTimerRunning = true;
        if (votingTimerText != null) votingTimerText.text = $"남은 시간: {Mathf.CeilToInt(currentVoteTimer)}초";
        if (currentVoteTimer <= 0)
        {
            isVotingTimerRunning = false;
            if (votingTimerText != null) votingTimerText.text = "시간 종료!";
        }
    }

    public void ShowResults(string resultMessage)
    {
        UpdateUIForState(GameManager.GameState.Results);
        if (resultsText != null) resultsText.text = $"투표 결과:\n{resultMessage}";
    }

    public void ShowPopupMessage(string message)
    {
        if (popupMessagePanel != null && popupMessageText != null)
        {
            popupMessageText.text = message;
            popupMessagePanel.SetActive(true);
            CancelInvoke(nameof(HidePopupMessage));
            Invoke(nameof(HidePopupMessage), 3f);
        }
    }

    public void HidePopupMessage()
    {
        if (popupMessagePanel != null)
        {
            popupMessagePanel.SetActive(false);
        }
    }
}