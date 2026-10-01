using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Testes de PlayMode da plataforma que anda e cai (MovingFallingPlatform).
/// Cada teste carrega a cena PlatformerDemo, desliga o teclado do jogador e o coloca em cima da plataforma
/// por código. O tempo é medido em passos de física (WaitForFixedUpdate), igual ao script.
/// Para rodar: Window > General > Test Runner > PlayMode > Run All.
/// </summary>
public class MovingFallingPlatformTests
{
    private const string SceneName = "PlatformerDemo";
    private const float MaxWaitSeconds = 3f;

    private PlayerController player;
    private Rigidbody2D playerBody;
    private Collider2D playerCollider;
    private MovingFallingPlatform platform;
    private Rigidbody2D platformBody;
    private Collider2D platformCollider;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        player = Object.FindFirstObjectByType<PlayerController>();
        platform = Object.FindFirstObjectByType<MovingFallingPlatform>();
        Assert.IsNotNull(player, "Nenhum PlayerController encontrado na cena " + SceneName);
        Assert.IsNotNull(platform, "Nenhuma MovingFallingPlatform encontrada na cena " + SceneName);

        playerBody = player.GetComponent<Rigidbody2D>();
        playerCollider = player.GetComponent<Collider2D>();
        platformBody = platform.GetComponent<Rigidbody2D>();
        platformCollider = platform.GetComponent<Collider2D>();

        player.readKeyboard = false;
        player.MoveInput = 0f;
        player.JumpHeld = false;

        // O jogador nasce acima do chão: espera pousar (longe da plataforma, que fica em outro ponto da fase).
        float elapsed = 0f;
        while (!player.IsGrounded && elapsed < MaxWaitSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        Assert.IsTrue(player.IsGrounded, "O jogador deveria estar no chão antes do teste.");
    }

    // Teletransporta o jogador para logo acima da plataforma; ele cai e encosta nela.
    private void PutPlayerAboveThePlatform(float gap)
    {
        Bounds platformBounds = platformCollider.bounds;
        float halfHeight = playerCollider.bounds.extents.y;
        Vector2 position = new Vector2(platformBounds.center.x, platformBounds.max.y + halfHeight + gap);

        playerBody.linearVelocity = Vector2.zero;
        playerBody.position = position;
        player.transform.position = position;
        Physics2D.SyncTransforms();
    }

    private IEnumerator WaitForState(MovingFallingPlatform.PlatformState expected, float timeoutSeconds)
    {
        float elapsed = 0f;
        while (platform.CurrentState != expected && elapsed < timeoutSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        Assert.AreEqual(expected, platform.CurrentState,
            "A plataforma deveria estar no estado " + expected + " em até " + timeoutSeconds + " s.");
    }

    [UnityTest]
    public IEnumerator Platform_Stays_Still_Until_Touched()
    {
        Vector2 start = platformBody.position;

        // 1,5 s (75 passos de física) sem ninguém encostar.
        for (int i = 0; i < 75; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.AreEqual(MovingFallingPlatform.PlatformState.Idle, platform.CurrentState, "Sem contato deveria continuar parada.");
        Assert.Less(Vector2.Distance(platformBody.position, start), 0.001f, "A plataforma saiu do lugar sem ser tocada (gravidade ou movimento indevido).");
        Assert.Less(platformBody.linearVelocity.magnitude, 0.001f, "A plataforma deveria estar com velocidade zero.");
    }

    [UnityTest]
    public IEnumerator Moves_Right_At_Constant_Speed_For_Exactly_Five_Seconds_Then_Stops_And_Falls()
    {
        Assert.AreEqual(5f, platform.MoveDuration, 0.0001f, "A duração configurada deveria ser 5 s.");
        Assert.AreEqual(Vector2.right, platform.MoveDirection, "A direção configurada deveria ser a direita.");

        Vector2 start = platformBody.position;
        PutPlayerAboveThePlatform(0.3f);
        yield return WaitForState(MovingFallingPlatform.PlatformState.Moving, 2f);

        Assert.AreEqual(start.x, platformBody.position.x, 0.01f, "Antes do contato a plataforma não deveria ter andado.");
        float startX = platformBody.position.x;

        // Cada iteração = 1 passo de física em que a plataforma estava andando.
        int movingSteps = 0;
        float maxSpeedError = 0f;
        float maxVerticalDrift = 0f;
        int guard = 0;
        while (guard < 600)
        {
            yield return new WaitForFixedUpdate();
            guard++;
            if (platform.CurrentState != MovingFallingPlatform.PlatformState.Moving) break;

            movingSteps++;
            maxSpeedError = Mathf.Max(maxSpeedError, Mathf.Abs(platformBody.linearVelocity.x - platform.MoveSpeed));
            maxVerticalDrift = Mathf.Max(maxVerticalDrift, Mathf.Abs(platformBody.position.y - start.y));
        }

        float movingSeconds = movingSteps * Time.fixedDeltaTime;
        Assert.AreEqual(MovingFallingPlatform.PlatformState.Falling, platform.CurrentState, "Depois do movimento deveria estar caindo.");
        Assert.AreEqual(5f, movingSeconds, 0.03f, "O movimento deveria durar exatamente 5 s.");
        Assert.Less(maxSpeedError, 0.01f, "A velocidade deveria ser constante durante todo o movimento.");
        Assert.Less(maxVerticalDrift, 0.01f, "Durante o movimento a plataforma deveria andar em linha reta, sem cair.");

        float travelled = platformBody.position.x - startX;
        Assert.AreEqual(platform.MoveSpeed * 5f, travelled, 0.05f, "Deslocamento para a direita diferente de velocidade x 5 s.");

        // Fim dos 5 s: sem movimento horizontal e agora sob efeito da gravidade.
        Assert.AreEqual(RigidbodyType2D.Dynamic, platformBody.bodyType, "Deveria passar a sofrer a física normal.");
        Assert.Greater(platformBody.gravityScale, 0f, "A gravidade deveria estar ligada na queda.");
        Assert.AreEqual(0f, platformBody.linearVelocity.x, 0.001f, "O movimento horizontal deveria ter parado por completo.");

        float fallX = platformBody.position.x;
        float fallY = platformBody.position.y;
        for (int i = 0; i < 15; i++)   // 0,3 s de queda
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Less(platformBody.position.y, fallY - 0.5f, "A plataforma deveria estar caindo.");
        Assert.AreEqual(fallX, platformBody.position.x, 0.01f, "A queda deveria ser só vertical.");
    }

    [UnityTest]
    public IEnumerator Touching_Again_During_The_Fall_Does_Not_Restart_The_Movement()
    {
        PutPlayerAboveThePlatform(0.3f);
        yield return WaitForState(MovingFallingPlatform.PlatformState.Moving, 2f);
        yield return WaitForState(MovingFallingPlatform.PlatformState.Falling, 8f);

        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        float fallX = platformBody.position.x;

        // Joga o jogador de novo em cima da plataforma, que já está na fase de queda: novo contato.
        PutPlayerAboveThePlatform(0.3f);
        bool touchedAgain = false;
        for (int i = 0; i < 50; i++)   // 1 s
        {
            yield return new WaitForFixedUpdate();

            if (playerCollider.IsTouching(platformCollider)) touchedAgain = true;
            Assert.AreEqual(MovingFallingPlatform.PlatformState.Falling, platform.CurrentState, "O novo contato não deveria reiniciar o movimento.");
            Assert.AreEqual(0f, platformBody.linearVelocity.x, 0.001f, "A plataforma não deveria voltar a andar na horizontal.");
        }

        Assert.IsTrue(touchedAgain, "O teste deveria ter provocado um novo contato com a plataforma.");
        Assert.AreEqual(fallX, platformBody.position.x, 0.01f, "A plataforma não deveria ter se movido na horizontal depois do novo contato.");
    }

    [UnityTest]
    public IEnumerator Player_Standing_On_The_Platform_Is_Carried_Along_While_It_Moves()
    {
        PutPlayerAboveThePlatform(0.3f);
        yield return WaitForState(MovingFallingPlatform.PlatformState.Moving, 2f);

        // Deixa o jogador assentar na plataforma e anota as posições de partida.
        for (int i = 0; i < 10; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        float playerStartX = playerBody.position.x;
        float platformStartX = platformBody.position.x;

        for (int i = 0; i < 100; i++)   // 2 s de movimento
        {
            yield return new WaitForFixedUpdate();
        }

        float platformDx = platformBody.position.x - platformStartX;
        float playerDx = playerBody.position.x - playerStartX;
        Assert.Greater(platformDx, 3f, "A plataforma deveria ter andado.");
        Assert.AreEqual(platformDx, playerDx, 0.3f, "O jogador parado em cima deveria ser levado junto com a plataforma.");
        Assert.IsTrue(playerCollider.IsTouching(platformCollider), "O jogador deveria continuar em cima da plataforma.");
    }
}
