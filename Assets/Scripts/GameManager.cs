using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro; // UIManager가 사용하는 TMP_Text 필드를 위해 유지될 수 있음

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    public UIManager uiManager; // Inspector에서 UIManager 할당 필요
    // public TMP_Text statusText; // 글로벌 상태 텍스트 필드 제거

    public enum GameState { MainMenu, CreatingPoll, WaitingForPlayer, Voting, Results }
    public GameState currentState;

    // 투표 관련 데이터
    private string pollItem1_A;
    private string pollItem2_B;
    public float voteDuration = 30f; // 기본 투표 시간 (초)
    private string lastSubmittedVote; // 마지막으로 제출된 투표 (마스터 클라이언트용)

    // 취소 관련 플래그
    private bool isAttemptingToCancelCreateDuringRpc = false; // 방 생성 RPC 도중 취소 시도 플래그

    // 표지판 동기화용
    public Signs signsManagerInstance; // Inspector에서 Signs 오브젝트 할당 필요

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    void Start()
    {
        ConnectToPhoton();
        // 초기 UI 상태는 OnConnectedToMaster 또는 OnJoinedLobby에서 설정합니다.
    }

    void ConnectToPhoton()
    {
        if (!PhotonNetwork.IsConnected)
        {
            UpdateStatus("서버에 연결 중..."); // 이 메시지는 이제 주로 UIManager의 waitingStatusText로 전달됨
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        // pollItem 정보는 Voting 상태일 때 UIManager에게 전달됨
        uiManager?.UpdateUIForState(newState, pollItem1_A, pollItem2_B);
    }

    public void UpdateStatus(string message)
    {
        // 글로벌 statusText UI 요소 관련 코드 제거
        Debug.Log($"[GameManager] Status Update: {message}"); // 개발자 확인용 로그는 유지

        // 현재 상태가 WaitingForPlayer이고, UIManager와 waitingStatusText가 존재하면
        // UIManager의 대기 화면 전용 텍스트를 이 메시지로 업데이트합니다.
        if (currentState == GameState.WaitingForPlayer && uiManager != null && uiManager.waitingStatusText != null)
        {
            uiManager.waitingStatusText.text = message;
        }
    }

    #region Public Methods Called by UI or Other Scripts

    public void CreatePollRoom(string itemA, string itemB)
    {
        if (string.IsNullOrEmpty(itemA) || string.IsNullOrEmpty(itemB))
        {
            uiManager?.ShowPopupMessage("두 가지 항목을 모두 입력해주세요!");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            uiManager?.ShowPopupMessage("서버에 아직 연결되지 않았습니다. 잠시 후 시도해주세요.");
            return;
        }

        pollItem1_A = itemA;
        pollItem2_B = itemB;
        isAttemptingToCancelCreateDuringRpc = false; // 플래그 초기화

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 2;
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "itemA", itemA },
            { "itemB", itemB },
            { "status", "waiting" }
        };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "itemA", "itemB", "status" };

        PhotonNetwork.CreateRoom(null, roomOptions);
        UpdateGameState(GameState.WaitingForPlayer);
        uiManager?.SetCancelCreatePollButtonActive(true); // "방 만들기" 시에는 취소 버튼 활성화
        UpdateStatus("투표 방 생성 중...");
    }

    public void JoinPollRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            uiManager?.ShowPopupMessage("서버에 아직 연결되지 않았습니다. 잠시 후 시도해주세요.");
            return;
        }
        isAttemptingToCancelCreateDuringRpc = false; // "방 찾기"는 다른 종류의 취소이므로 이 플래그는 false

        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "status", "waiting" }
        };
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, 2);
        UpdateGameState(GameState.WaitingForPlayer);
        uiManager?.SetCancelCreatePollButtonActive(false); // "방 찾기" 시에는 취소 버튼 비활성화
        UpdateStatus("투표 방 검색 중...");
    }

    // UIManager의 취소 버튼에 연결될 핸들러 (이제 "방 만들기" 관련 취소만 주로 처리)
    public void HandleCancelWaitingOrCreation()
    {
        uiManager?.SetCancelCreatePollButtonActive(false);

        // 이 함수는 이제 "방 만들기" 시나리오의 취소에만 해당됨
        if (currentState == GameState.WaitingForPlayer)
        {
            if (!PhotonNetwork.InRoom) // 생성 RPC 진행 중 취소
            {
                UpdateStatus("투표 방 생성을 취소합니다...");
                isAttemptingToCancelCreateDuringRpc = true;
                if (PhotonNetwork.InLobby) { PhotonNetwork.LeaveLobby(); }
                UpdateGameState(GameState.MainMenu);
            }
            else if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom) // 마스터가 상대방 기다리던 중 취소
            {
                UpdateStatus("상대방 기다리기를 취소하고 방을 나갑니다...");
                PhotonNetwork.LeaveRoom();
            }
            else // 기타 WaitingForPlayer 상황
            {
                UpdateStatus("메인 메뉴로 돌아갑니다...");
                if (PhotonNetwork.InRoom) PhotonNetwork.LeaveRoom();
                else UpdateGameState(GameState.MainMenu);
            }
        }
        else // 그 외 다른 상태
        {
            UpdateGameState(GameState.MainMenu);
        }
    }

    public void LeaveRoomAndGoToMainMenu()
    {
        isAttemptingToCancelCreateDuringRpc = false;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            UpdateGameState(GameState.MainMenu);
        }
    }

    // Signs 동기화를 위한 메소드 (VoterCharacter가 호출)
    public void BroadcastSignSelection(int selectedItemIndex)
    {
        if (photonView != null)
        {
            photonView.RPC("Rpc_UpdateSignsVisuals", RpcTarget.All, selectedItemIndex);
        }
        else
        {
            Debug.LogError("[GameManager] PhotonView가 없어 표지판 동기화 RPC를 호출할 수 없습니다.");
        }
    }

    [PunRPC]
    void Rpc_UpdateSignsVisuals(int selectedItemIndex, PhotonMessageInfo info)
    {
        if (signsManagerInstance != null)
        {
            signsManagerInstance.UpdateSignDisplay(selectedItemIndex);
        }
        else
        {
            Debug.LogWarning("[GameManager] signsManagerInstance 참조가 없어 표지판 UI를 업데이트할 수 없습니다.");
        }
    }

    #endregion

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateStatus("서버에 연결되었습니다!");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        UpdateStatus("로비에 접속했습니다. 투표를 만들거나 참여할 수 있습니다.");
        if (!PhotonNetwork.InRoom && !isAttemptingToCancelCreateDuringRpc && currentState != GameState.CreatingPoll)
        {
             UpdateGameState(GameState.MainMenu);
        }
        isAttemptingToCancelCreateDuringRpc = false;
        uiManager?.SetCancelCreatePollButtonActive(false);
    }

    public override void OnCreatedRoom()
    {
        if (isAttemptingToCancelCreateDuringRpc)
        {
            isAttemptingToCancelCreateDuringRpc = false;
            uiManager?.SetCancelCreatePollButtonActive(false);
            PhotonNetwork.LeaveRoom();
            return;
        }
        UpdateStatus($"투표 방 '{PhotonNetwork.CurrentRoom.Name}' 생성 완료. 상대방을 기다립니다...");
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            uiManager?.SetCancelCreatePollButtonActive(true);
        }
        else
        {
            uiManager?.SetCancelCreatePollButtonActive(false);
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isAttemptingToCancelCreateDuringRpc = false;
        uiManager?.SetCancelCreatePollButtonActive(false);
        UpdateStatus($"투표 방 생성 실패: {message}");
        UpdateGameState(GameState.MainMenu);
    }

    public override void OnJoinedRoom()
    {
        uiManager?.SetCancelCreatePollButtonActive(false); // 방에 들어가면 취소 버튼은 일단 숨김 (방 만들기 후 대기 시에는 OnCreatedRoom에서 다시 켤 수 있음)
        UpdateStatus($"방 '{PhotonNetwork.CurrentRoom.Name}'에 참가했습니다. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");

        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                StartVotingPhaseIfReady();
            }
            else
            {
                // 마스터이고 아직 방이 안 찼다면 (예: 생성 직후 또는 재접속) 취소 버튼 표시
                uiManager?.SetCancelCreatePollButtonActive(true);
            }
        }
        else // 참가자
        {
            pollItem1_A = (string)PhotonNetwork.CurrentRoom.CustomProperties["itemA"];
            pollItem2_B = (string)PhotonNetwork.CurrentRoom.CustomProperties["itemB"];
            UpdateStatus("상대방을 기다리거나, 마스터가 게임을 시작할 것입니다.");
            // 참가자는 WaitingForPlayer 상태에서 UI를 보지만, 방 생성 취소 버튼은 마스터용이므로 여기서는 false
            uiManager?.SetCancelCreatePollButtonActive(false);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        uiManager?.SetCancelCreatePollButtonActive(false);
        UpdateStatus($"방 참가 실패: {message}");
        UpdateGameState(GameState.MainMenu);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        uiManager?.SetCancelCreatePollButtonActive(false);
        UpdateStatus($"참여 가능한 투표 방을 찾지 못했습니다: {message}. 직접 만들어보세요!");
        UpdateGameState(GameState.MainMenu);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus($"상대방 {newPlayer.NickName}님이 입장했습니다. 현재 인원: {PhotonNetwork.CurrentRoom.PlayerCount}");
        if (PhotonNetwork.IsMasterClient)
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
            {
                uiManager?.SetCancelCreatePollButtonActive(false);
                StartVotingPhaseIfReady();
            }
            // else -> 방이 아직 덜 찼으면 마스터의 취소 버튼은 계속 활성화 상태여야 함 (OnCreatedRoom 또는 OnJoinedRoom에서 처리)
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateStatus($"상대방 {otherPlayer.NickName}님이 나갔습니다.");
        if (currentState == GameState.Voting || currentState == GameState.Results)
        {
            photonView.RPC("Rpc_HandleOpponentLeft", RpcTarget.All);
        }
        else if (currentState == GameState.WaitingForPlayer && PhotonNetwork.IsMasterClient)
        {
            UpdateStatus("상대방이 나갔습니다. 다시 기다립니다...");
            uiManager?.SetCancelCreatePollButtonActive(true); // 마스터는 다시 취소 버튼 활성화
        }
    }

    public override void OnLeftRoom()
    {
        UpdateStatus("방에서 나왔습니다.");
        pollItem1_A = null;
        pollItem2_B = null;
        uiManager?.SetCancelCreatePollButtonActive(false);
        isAttemptingToCancelCreateDuringRpc = false;
        UpdateGameState(GameState.MainMenu);
        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"서버 연결 끊김: {cause}.");
        uiManager?.SetCancelCreatePollButtonActive(false);
        isAttemptingToCancelCreateDuringRpc = false;
        UpdateGameState(GameState.MainMenu);
    }

    #endregion

    void StartVotingPhaseIfReady()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom.PlayerCount != PhotonNetwork.CurrentRoom.MaxPlayers) return;
        UpdateStatus("모든 플레이어가 모였습니다. 곧 투표를 시작합니다...");
        ExitGames.Client.Photon.Hashtable roomProps = PhotonNetwork.CurrentRoom.CustomProperties;
        roomProps["status"] = "playing";
        PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        photonView.RPC("Rpc_StartVoting", RpcTarget.All, voteDuration, pollItem1_A, pollItem2_B);
    }

    [PunRPC]
    void Rpc_StartVoting(float duration, string itemA, string itemB, PhotonMessageInfo info)
    {
        pollItem1_A = itemA;
        pollItem2_B = itemB;
        voteDuration = duration;
        lastSubmittedVote = null;
        UpdateGameState(GameState.Voting);

        if (!PhotonNetwork.IsMasterClient)
        {
            Vector3 spawnPosition = new Vector3(0, 3, 0);
            GameObject playerCharacterGO = PhotonNetwork.Instantiate("Prefabs/VoterCharacter", spawnPosition, Quaternion.identity);
            VoterCharacter characterScript = playerCharacterGO.GetComponent<VoterCharacter>();
            if (characterScript != null && characterScript.photonView.IsMine)
            {
                characterScript.InitializeVoting(duration);
            }
        }
        else
        {
            CancelInvoke("EndVotingPhaseByTime");
            Invoke("EndVotingPhaseByTime", duration);
        }
        uiManager?.StartVoteTimerUI(duration);
    }

    [PunRPC]
    public void Rpc_SubmitVote(string votedItem, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (currentState != GameState.Voting) return;
        string voterNickName = info.Sender != null ? info.Sender.NickName : "상대방";
        UpdateStatus($"{voterNickName}님이 '{votedItem}'에 투표했습니다.");
        lastSubmittedVote = votedItem;
        CancelInvoke("EndVotingPhaseByTime");
        AnnounceResult(lastSubmittedVote);
    }

    void EndVotingPhaseByTime()
    {
        if (!PhotonNetwork.IsMasterClient || currentState != GameState.Voting) return;
        UpdateStatus("시간 초과!");
        AnnounceResult(string.IsNullOrEmpty(lastSubmittedVote) ? "시간 초과 (투표 없음)" : lastSubmittedVote);
    }

    void AnnounceResult(string result)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC("Rpc_AnnounceResults", RpcTarget.All, result);
    }

    [PunRPC]
    void Rpc_AnnounceResults(string finalResult)
    {
        UpdateGameState(GameState.Results);
        uiManager?.ShowResults(finalResult);

        // 캐릭터 파괴 로직
        if (!PhotonNetwork.IsMasterClient)
        {
            if (VoterCharacter.LocalInstance != null && VoterCharacter.LocalInstance.photonView.IsMine)
            {
                Debug.Log("[GameManager] Rpc_AnnounceResults: 로컬 플레이어 VoterCharacter 파괴.");
                PhotonNetwork.Destroy(VoterCharacter.LocalInstance.gameObject);
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Invoke("DelayedLeaveRoomAndGoToMainMenu", 5f);
        }
    }

    [PunRPC]
    void Rpc_HandleOpponentLeft()
    {
        UpdateStatus("상대방이 나갔습니다. 게임이 종료됩니다.");
        if (currentState == GameState.Voting || currentState == GameState.Results || currentState == GameState.WaitingForPlayer)
        {
            uiManager?.HideAllGamePanels();
            uiManager?.ShowPopupMessage("상대방이 나갔습니다. 메인 메뉴로 돌아갑니다.");
        }
        // 상대방 나갔을 때도 로컬 캐릭터 파괴
        if (!PhotonNetwork.IsMasterClient && VoterCharacter.LocalInstance != null && VoterCharacter.LocalInstance.photonView.IsMine)
        {
            Debug.Log("[GameManager] Rpc_HandleOpponentLeft: 상대방 퇴장으로 로컬 VoterCharacter 파괴.");
            PhotonNetwork.Destroy(VoterCharacter.LocalInstance.gameObject);
        }
        
        if (PhotonNetwork.InRoom)
        {
            CancelInvoke("DelayedLeaveRoomAndGoToMainMenu");
            Invoke("DelayedLeaveRoomAndGoToMainMenu", 3f);
        }
    }

    void DelayedLeaveRoomAndGoToMainMenu()
    {
        LeaveRoomAndGoToMainMenu();
    }
}