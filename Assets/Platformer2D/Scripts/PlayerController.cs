using UnityEngine;

/// <summary>
/// Movimentação básica 2D com pulo para um personagem com Rigidbody2D.
///
/// Controles (Input Manager clássico):
///   - Andar: setas ou A / D  (eixo "Horizontal")
///   - Pular: Espaço          (botão "Jump")
///
/// A física (gravidade e colisões) fica por conta da Unity. O script apenas define
/// velocidades e decide quando o pulo é permitido (só com o personagem no chão).
///
/// Extras de "game feel" (todos ajustáveis no Inspector):
///   - Altura de pulo variável: soltar o botão cedo corta o pulo.
///   - Coyote time: ainda dá para pular logo depois de sair da borda de uma plataforma.
///   - Jump buffer: um pulo apertado um instante antes de tocar o chão ainda é aceito.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimento")]
    [Tooltip("Velocidade horizontal, em unidades por segundo.")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Pulo")]
    [Tooltip("Altura do pulo, em unidades da Unity, com o botão mantido pressionado.")]
    [SerializeField] private float jumpHeight = 3.2f;
    [Tooltip("Gravidade extra na queda (>1 deixa a queda mais rápida e o pulo menos flutuante).")]
    [SerializeField] private float fallGravityMultiplier = 1.6f;
    [Tooltip("Gravidade extra na subida quando o botão é solto cedo (pulo de altura variável).")]
    [SerializeField] private float lowJumpGravityMultiplier = 2.5f;
    [Tooltip("Tempo (s) em que ainda dá para pular depois de sair de uma plataforma.")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Tempo (s) em que um pulo apertado antes de tocar o chão ainda é aceito.")]
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground check")]
    [Tooltip("Ponto nos pés do personagem usado para detectar o chão.")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [Tooltip("Layer(s) que contam como chão.")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Respawn")]
    [Tooltip("Se o personagem cair abaixo dessa altura (Y), volta ao ponto inicial.")]
    [SerializeField] private float respawnBelowY = -10f;

    [Header("Entrada")]
    [Tooltip("Desmarque para controlar o personagem por código (usado nos testes automatizados).")]
    public bool readKeyboard = true;

    [Header("Debug (somente leitura)")]
    [SerializeField] private bool isGrounded;

    // Entradas atuais. Normalmente vêm do teclado, mas podem ser escritas por código (testes).
    public float MoveInput { get; set; }
    public bool JumpHeld { get; set; }

    public bool IsGrounded => isGrounded;
    public float JumpHeight => jumpHeight;
    public float MoveSpeed => moveSpeed;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float baseGravityScale;
    private Vector2 startPosition;
    private float coyoteCounter;
    private float jumpBufferCounter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseGravityScale = rb.gravityScale;
        startPosition = transform.position;

        if (groundCheck == null)
        {
            Debug.LogError("PlayerController: defina o objeto 'groundCheck' (ponto nos pés) no Inspector.", this);
            enabled = false;
        }
    }

    // A entrada é lida em Update (todo frame) para não perder nenhum clique.
    private void Update()
    {
        if (readKeyboard)
        {
            MoveInput = Input.GetAxisRaw("Horizontal");
            JumpHeld = Input.GetButton("Jump");
            if (Input.GetButtonDown("Jump"))
            {
                PressJump();
            }
        }

        // Contadores de tolerância: coyote time e jump buffer.
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;

        // Vira o sprite para o lado em que o personagem anda.
        if (spriteRenderer != null && Mathf.Abs(MoveInput) > 0.01f)
        {
            spriteRenderer.flipX = MoveInput < 0f;
        }
    }

    // A física é aplicada em FixedUpdate, no mesmo ritmo do motor de física.
    private void FixedUpdate()
    {
        CheckGround();
        Move();

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Jump();
        }

        ApplyGravityModifiers();

        if (transform.position.y < respawnBelowY)
        {
            Respawn();
        }
    }

    /// <summary>Registra um pedido de pulo (aceito por um curto período, o jump buffer).</summary>
    public void PressJump()
    {
        jumpBufferCounter = jumpBufferTime;
    }

    private void CheckGround()
    {
        bool touchingGround = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;

        // Enquanto está subindo não conta como "no chão": evita um segundo pulo logo após pular.
        isGrounded = touchingGround && rb.linearVelocity.y <= 0.05f;
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(MoveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        // Velocidade inicial para atingir 'jumpHeight' com a gravidade atual:
        // v = sqrt(2 * g * h)
        float gravity = Mathf.Abs(Physics2D.gravity.y) * baseGravityScale;
        float jumpVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpVelocity);

        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        isGrounded = false;
    }

    private void ApplyGravityModifiers()
    {
        float velocityY = rb.linearVelocity.y;

        if (velocityY < -0.01f)
        {
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;      // caindo
        }
        else if (velocityY > 0.01f && !JumpHeld)
        {
            rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;   // soltou o botão: corta o pulo
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    private void Respawn()
    {
        rb.linearVelocity = Vector2.zero;
        rb.position = startPosition;
        transform.position = startPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
