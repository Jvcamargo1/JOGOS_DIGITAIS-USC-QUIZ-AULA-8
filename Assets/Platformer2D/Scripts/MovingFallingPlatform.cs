using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plataforma que fica parada até o jogador encostar nela; então anda em linha reta, em velocidade
/// constante, durante um tempo fixo (5 s) e, ao final, para na horizontal e cai pela gravidade.
///
/// Ciclo de vida (uma única vez):
///   Idle    - parada, sem gravidade (Rigidbody2D Kinematic). Espera o primeiro contato com o jogador.
///   Moving  - anda na direção configurada, a velocidade constante, por 'moveDuration' segundos.
///   Falling - sem movimento horizontal, Rigidbody2D Dynamic com gravidade. Estado final: contatos
///             novos com o jogador são ignorados, então o movimento nunca recomeça.
///
/// O tempo é contado em passos de física (FixedUpdate), não em frames: com Time.fixedDeltaTime = 0,02
/// o movimento dura exatamente 250 passos = 5,00 s, independente do FPS.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingFallingPlatform : MonoBehaviour
{
    public enum PlatformState { Idle, Moving, Falling }

    [Header("Movimento")]
    [Tooltip("Direção do movimento (não precisa ser unitária). Ex.: (1,0) = direita.")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [Tooltip("Velocidade constante, em unidades por segundo.")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("Quanto tempo (s) a plataforma anda depois de tocada.")]
    [SerializeField] private float moveDuration = 5f;

    [Header("Queda")]
    [Tooltip("Escala de gravidade aplicada quando o movimento termina.")]
    [SerializeField] private float fallGravityScale = 3f;

    [Header("Gatilho")]
    [Tooltip("Tag do objeto que ativa a plataforma ao encostar nela.")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Passageiros")]
    [Tooltip("Se ligado, quem está em cima da plataforma é levado junto enquanto ela anda " +
             "(o PlayerController zera a velocidade horizontal a cada passo, então precisa desse empurrão).")]
    [SerializeField] private bool carryRiders = true;

    [Header("Debug (somente leitura)")]
    [SerializeField] private PlatformState state = PlatformState.Idle;
    [SerializeField] private float movingElapsed;

    public PlatformState CurrentState => state;
    public Vector2 MoveDirection => moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.right;
    public float MoveSpeed => moveSpeed;
    public float MoveDuration => moveDuration;

    // Tolerância para erro de ponto flutuante ao somar 250 vezes o fixedDeltaTime.
    private const float TimeEpsilon = 1e-4f;
    // Folga para considerar que alguém está "em cima" da plataforma (e não encostado na lateral).
    private const float TopTolerance = 0.1f;

    private Rigidbody2D rb;
    private Collider2D platformCollider;
    private readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<Collider2D>();

        if (platformCollider == null)
        {
            Debug.LogError("MovingFallingPlatform: falta um Collider2D neste objeto.", this);
            enabled = false;
            return;
        }

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            Debug.LogWarning("MovingFallingPlatform: direção zerada, usando a direita (1,0).", this);
        }

        // Estado inicial: imóvel e sem efeito da gravidade.
        // Kinematic não sofre gravidade nem é empurrado, mas ainda gera colisões com o jogador.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        state = PlatformState.Idle;
        movingElapsed = 0f;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        moveDuration = Mathf.Max(0f, moveDuration);
        fallGravityScale = Mathf.Max(0f, fallGravityScale);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(triggerTag)) return;

        // Só o primeiro contato (estado Idle) ativa. Durante Moving e Falling o contato é ignorado.
        if (state == PlatformState.Idle)
        {
            StartMoving();
        }

        TryRegisterRider(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryRegisterRider(collision);
    }

    private void FixedUpdate()
    {
        if (state != PlatformState.Moving) return;

        if (movingElapsed < moveDuration - TimeEpsilon)
        {
            // Velocidade reaplicada a cada passo: constante durante todo o movimento.
            Vector2 velocity = MoveDirection * moveSpeed;
            rb.linearVelocity = velocity;
            movingElapsed += Time.fixedDeltaTime;
            CarryRiders(velocity * Time.fixedDeltaTime);
        }
        else
        {
            StartFalling();
        }
    }

    private void StartMoving()
    {
        state = PlatformState.Moving;
        movingElapsed = 0f;
        riders.Clear();
        // A velocidade é aplicada no próximo FixedUpdate: assim o movimento dura exatamente moveDuration.
    }

    private void StartFalling()
    {
        riders.Clear();

        // Passa a ser afetada pela física normal: Dynamic + gravidade.
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;

        // Para completamente na horizontal e trava o eixo X: a queda é sempre vertical.
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        rb.WakeUp();

        state = PlatformState.Falling;
    }

    // Guarda quem está em cima da plataforma neste passo de física.
    private void TryRegisterRider(Collision2D collision)
    {
        if (!carryRiders || state != PlatformState.Moving) return;

        Rigidbody2D other = collision.rigidbody;
        if (other == null || other.bodyType != RigidbodyType2D.Dynamic) return;

        bool onTop = collision.collider.bounds.min.y >= platformCollider.bounds.max.y - TopTolerance;
        if (onTop)
        {
            riders.Add(other);
        }
    }

    // Desloca os passageiros o mesmo tanto que a plataforma andou neste passo.
    private void CarryRiders(Vector2 delta)
    {
        foreach (Rigidbody2D rider in riders)
        {
            if (rider != null)
            {
                rider.position += delta;
            }
        }
        riders.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        // Mostra o trajeto previsto: da posição atual até onde a plataforma chega em moveDuration segundos.
        Vector3 from = transform.position;
        Vector3 to = from + (Vector3)(MoveDirection * moveSpeed * moveDuration);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawWireSphere(to, 0.15f);
    }
}
