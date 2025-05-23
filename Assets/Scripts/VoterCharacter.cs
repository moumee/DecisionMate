using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class VoterCharacter : MonoBehaviourPunCallbacks
{
    // Camera mainCamera; // 사용하지 않으므로 제거 또는 주석 처리
    public float moveSpeed = 5f;
    private Rigidbody2D rb2D;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;

    [Header("Sound Effects")]
    public AudioClip footstepSound;

    private string currentItemName = "";
    private float localVoteTimer = 0f;
    private bool isLocalTimerRunning = false;

    public static VoterCharacter LocalInstance;

    public bool rightButtonPressed = false;
    public bool leftButtonPressed = false;

    private int lastSelectedItemIndex = -1;

    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (!photonView.IsMine)
        {
            enabled = false;
            if (rb2D != null)
            {
                rb2D.bodyType = RigidbodyType2D.Kinematic;
            }
            return;
        }

        if (LocalInstance == null)
        {
            LocalInstance = this;
        }
        else if (LocalInstance != this)
        {
             // Debug.LogWarning("중복된 VoterCharacter LocalInstance가 감지되었습니다. 현재 인스턴스를 파괴하거나 다른 조치를 취합니다.");
             // Destroy(gameObject); // 또는 다른 로직으로 Instance 관리
        }
    }

    void OnDestroy()
    {
        if (photonView.IsMine && LocalInstance == this)
        {
            LocalInstance = null;
            Debug.Log("[VoterCharacter] LocalInstance 참조가 파괴 시 정리되었습니다.");
        }
    }

    public void InitializeVoting(float duration)
    {
        if (!photonView.IsMine) return;

        float bufferTime = 0.5f;
        localVoteTimer = duration - bufferTime;
        if (localVoteTimer < 0) localVoteTimer = 0;

        isLocalTimerRunning = true;
        enabled = true;
        if (rb2D != null) rb2D.bodyType = RigidbodyType2D.Dynamic;
        lastSelectedItemIndex = -1; // 투표 시작 시 선택 아이템 인덱스 초기화
    }

    void Update()
    {
        if (!photonView.IsMine || !enabled) return;

        if (isLocalTimerRunning)
        {
            localVoteTimer -= Time.deltaTime;
            if (localVoteTimer <= 0)
            {
                isLocalTimerRunning = false;
                FinalizeVote();
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine || !enabled) return;

        HandleMovement();

        int currentSelectedItemIndex = -1;
        if (transform.position.x < 0)
        {
            currentItemName = UIManager.Instance?.votingPollItemAText.text;
            currentSelectedItemIndex = 0;
        }
        else
        {
            currentItemName = UIManager.Instance?.votingPollItemBText.text;
            currentSelectedItemIndex = 1;
        }

        if (currentSelectedItemIndex != lastSelectedItemIndex)
        {
            lastSelectedItemIndex = currentSelectedItemIndex;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BroadcastSignSelection(currentSelectedItemIndex);
            }
        }
    }

    void HandleMovement()
    {
        float horizontalInput = 0;
        if (leftButtonPressed && !rightButtonPressed)
        {
            horizontalInput = -1f;
        }
        else if (!leftButtonPressed && rightButtonPressed)
        {
            horizontalInput = 1f;
        }
        else if (!leftButtonPressed && !rightButtonPressed)
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }

        if (rb2D != null)
        {
            Vector2 moveAmount = new Vector2(horizontalInput * moveSpeed * Time.fixedDeltaTime, 0);
            rb2D.MovePosition(rb2D.position + moveAmount);
        }

        if (animator != null)
        {
            bool isCurrentlyMoving = Mathf.Abs(horizontalInput) > 0.01f;
            animator.SetBool("isMoving", isCurrentlyMoving); // "isWalking" 대신 "isMoving" 사용
        }

        if (spriteRenderer != null)
        {
            // 스프라이트가 기본적으로 왼쪽을 보고, 오른쪽으로 갈 때 X축 반전하는 경우
            if (horizontalInput > 0.01f)
            {
                transform.localScale = new Vector3(-1f, 1f, 1f);
            }
            else if (horizontalInput < -0.01f)
            {
                transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }

    public void PlayFootstepSound() // 애니메이션 이벤트에서 호출
    {
        if (footstepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(footstepSound);
        }
        else
        {
            if (photonView.IsMine)
            {
                // Debug.LogWarning($"VoterCharacter {gameObject.name}: footstepSound 또는 audioSource가 없습니다.");
            }
        }
    }

    public void FinalizeVote()
    {
        if (!photonView.IsMine || !enabled) return;

        if (animator != null)
        {
            animator.SetBool("isMoving", false); // "isWalking" 대신 "isMoving" 사용
        }

        string voteToSend = "";
        if (!string.IsNullOrEmpty(currentItemName))
        {
            voteToSend = currentItemName;
            Debug.Log($"[VoterCharacter] 투표 제출: {voteToSend}");
        }
        else
        {
            voteToSend = "선택 없음";
            Debug.Log("[VoterCharacter] 투표 제출: 선택 없음 (아이템 영역 밖이거나 UIManager 텍스트 비어있음)");
        }

        if (GameManager.Instance != null && GameManager.Instance.photonView != null)
        {
            GameManager.Instance.photonView.RPC("Rpc_SubmitVote", RpcTarget.MasterClient, voteToSend);
        }
        else
        {
            Debug.LogError("[VoterCharacter] GameManager 인스턴스 또는 GameManager의 PhotonView를 찾을 수 없습니다.");
        }

        enabled = false;
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
    }
}