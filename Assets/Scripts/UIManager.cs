using UnityEngine;
using TMPro; // TextMeshPro 사용 시

public class UIManager : MonoBehaviour
{
    public static UIManager Instance; // 싱글톤으로 사용하려면

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject pollCreationPanel;
    public GameObject waitingPanel;
    public GameObject votingPanel;
    public GameObject resultsPanel;
    public GameObject popupMessagePanel;

    [Header("UI Text Elements")]
    public TMP_Text waitingStatusText; // waitingPanel 내부의 텍스트 (선택 사항)
    public TMP_Text votingPollItemAText;
    public TMP_Text votingPollItemBText;
    public TMP_Text votingTimerText;
    public TMP_Text resultsText;
    public TMP_Text popupMessageText;

    private float currentVoteTimer = 0f;
    private bool isVotingTimerRunning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
                // 실제 투표 종료 로직은 GameManager와 VoterCharacter가 처리
            }
        }
    }

    public void UpdateUIForState(GameManager.GameState state, string itemA = "", string itemB = "")
    {
        // 모든 패널 우선 비활성화
        HideAllGamePanels();

        switch (state)
        {
            case GameManager.GameState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case GameManager.GameState.CreatingPoll:
                if (pollCreationPanel != null)  pollCreationPanel.SetActive(true);
                break;
            case GameManager.GameState.WaitingForPlayer:
                if (waitingPanel != null)  waitingPanel.SetActive(true);
                // GameManager의 statusText가 주 상태를 표시하므로, waitingStatusText는 보조적으로 사용하거나 생략 가능
                if (waitingStatusText != null) waitingStatusText.text = GameManager.Instance.statusText.text; // GameManager의 메시지를 복사
                break;
            case GameManager.GameState.Voting:
                if (votingPanel != null) votingPanel.SetActive(true);
                if (votingPollItemAText != null) votingPollItemAText.text = itemA;
                if (votingPollItemBText != null) votingPollItemBText.text = itemB;
                // 타이머 시작은 Rpc_StartVoting에서 GameManager가 UIManager의 함수를 호출해줄 것임
                break;
            case GameManager.GameState.Results:
                if (resultsPanel != null) resultsPanel.SetActive(true);
                // 결과 표시는 ShowResults 메소드에서 처리
                break;
        }
    }

    public void HideAllGamePanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (pollCreationPanel != null) pollCreationPanel.SetActive(false);
        if (waitingPanel != null) waitingPanel.SetActive(false);
        if (votingPanel != null) votingPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (popupMessagePanel != null) popupMessagePanel.SetActive(false);
    }

    public void StartVoteTimerUI(float duration)
    {
        currentVoteTimer = duration;
        isVotingTimerRunning = true;
        if (votingTimerText != null) votingTimerText.text = $"남은 시간: {Mathf.CeilToInt(currentVoteTimer)}초";
    }

    public void ShowResults(string resultMessage) // GameManager의 Rpc_AnnounceResults에서 호출
    {
        UpdateUIForState(GameManager.GameState.Results); // 결과 패널 활성화
        if (resultsText != null) resultsText.text = $"투표 결과:\n{resultMessage}";
    }

    public void ShowPopupMessage(string message) // GameManager에서 호출
    {
        if (popupMessagePanel != null && popupMessageText != null)
        {
            popupMessageText.text = message;
            popupMessagePanel.SetActive(true);
            // 메시지 종류에 따라 자동으로 닫히게 할 수 있음
            // Invoke(nameof(HidePopupMessage), 3f); // 예: 3초 후 팝업 숨기기
        }
    }

    public void HidePopupMessage()
    {
        if (popupMessagePanel != null) popupMessagePanel.SetActive(false);
    }
}