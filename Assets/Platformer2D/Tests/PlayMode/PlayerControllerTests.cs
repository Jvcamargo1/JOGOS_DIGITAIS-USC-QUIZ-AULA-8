using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Testes de PlayMode da movimentação. Cada teste carrega a cena PlatformerDemo,
/// desliga a leitura do teclado (readKeyboard = false) e controla o personagem por código.
/// Para rodar: Window > General > Test Runner > PlayMode > Run All.
/// </summary>
public class PlayerControllerTests
{
    private const string SceneName = "PlatformerDemo";
    private const float MaxWaitSeconds = 3f;

    private PlayerController player;
    private Rigidbody2D body;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SceneManager.LoadScene(SceneName);
        yield return null;
        yield return null;

        player = Object.FindFirstObjectByType<PlayerController>();
        Assert.IsNotNull(player, "Nenhum PlayerController encontrado na cena " + SceneName);

        body = player.GetComponent<Rigidbody2D>();
        player.readKeyboard = false;
        player.MoveInput = 0f;
        player.JumpHeld = false;

        // O personagem nasce um pouco acima do chão: espera ele cair e pousar.
        yield return WaitUntilGrounded();
    }

    private IEnumerator WaitUntilGrounded()
    {
        float elapsed = 0f;
        while (!player.IsGrounded && elapsed < MaxWaitSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
        }
        Assert.IsTrue(player.IsGrounded, "O personagem deveria estar no chão.");
    }

    [UnityTest]
    public IEnumerator Player_Falls_And_Lands_On_Ground()
    {
        // Já pousou no SetUp: aqui só confirmamos que ficou parado em cima do chão.
        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(player.IsGrounded, "Deveria continuar no chão.");
        Assert.Less(Mathf.Abs(body.linearVelocity.y), 0.5f, "Deveria estar praticamente parado na vertical.");
        Assert.That(body.position.y, Is.InRange(0.2f, 0.8f), "Deveria estar apoiado na superfície do chão (Y = 0).");
    }

    [UnityTest]
    public IEnumerator Jump_Reaches_Configured_Height_And_Lands_Again()
    {
        float startY = body.position.y;
        float maxY = startY;
        bool leftGround = false;

        player.JumpHeld = true;   // botão mantido: altura total
        player.PressJump();

        float elapsed = 0f;
        while (elapsed < MaxWaitSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            maxY = Mathf.Max(maxY, body.position.y);
            if (!player.IsGrounded) leftGround = true;
            if (leftGround && player.IsGrounded) break;
        }

        Assert.IsTrue(leftGround, "O personagem deveria sair do chão ao pular.");
        Assert.AreEqual(player.JumpHeight, maxY - startY, 0.35f, "Altura do pulo diferente da configurada.");
        Assert.IsTrue(player.IsGrounded, "O personagem deveria voltar ao chão.");
    }

    [UnityTest]
    public IEnumerator Cannot_Jump_Again_In_Midair()
    {
        float startY = body.position.y;
        float maxY = startY;
        bool pressedAgain = false;
        bool leftGround = false;

        player.JumpHeld = true;
        player.PressJump();

        float elapsed = 0f;
        while (elapsed < MaxWaitSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            maxY = Mathf.Max(maxY, body.position.y);
            if (!player.IsGrounded) leftGround = true;

            // Já bem no alto da subida, tenta pular de novo (não deve ter efeito).
            if (!pressedAgain && body.position.y > startY + 1f)
            {
                pressedAgain = true;
                player.PressJump();
            }

            if (leftGround && player.IsGrounded) break;
        }

        Assert.IsTrue(pressedAgain, "O teste deveria ter tentado o segundo pulo.");
        Assert.Less(maxY - startY, player.JumpHeight * 1.25f, "Houve pulo duplo: o personagem passou da altura máxima.");
    }

    [UnityTest]
    public IEnumerator Releasing_Jump_Early_Gives_A_Shorter_Jump()
    {
        float startY = body.position.y;
        float maxY = startY;
        bool leftGround = false;

        player.JumpHeld = false;  // soltou o botão logo no início: pulo curto
        player.PressJump();

        float elapsed = 0f;
        while (elapsed < MaxWaitSeconds)
        {
            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            maxY = Mathf.Max(maxY, body.position.y);
            if (!player.IsGrounded) leftGround = true;
            if (leftGround && player.IsGrounded) break;
        }

        Assert.IsTrue(leftGround, "O personagem deveria sair do chão ao pular.");
        Assert.Less(maxY - startY, player.JumpHeight * 0.7f, "O pulo curto deveria ser bem menor que o pulo completo.");
        Assert.Greater(maxY - startY, 0.3f, "Mesmo o pulo curto deveria sair do chão.");
    }

    [UnityTest]
    public IEnumerator Moves_Horizontally_At_Configured_Speed()
    {
        const int steps = 25;   // 25 passos de física = 0,5 s
        float startX = body.position.x;

        player.MoveInput = 1f;
        for (int i = 0; i < steps; i++)
        {
            yield return new WaitForFixedUpdate();
        }
        player.MoveInput = 0f;

        float expected = player.MoveSpeed * steps * Time.fixedDeltaTime;
        Assert.AreEqual(expected, body.position.x - startX, 0.4f, "Deslocamento horizontal diferente do esperado.");
    }

    [UnityTest]
    public IEnumerator Falling_Out_Of_The_World_Respawns_The_Player()
    {
        Vector2 spawn = body.position;

        player.transform.position = new Vector3(0f, -20f, 0f);
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Assert.Greater(body.position.y, -5f, "O personagem deveria ter voltado ao ponto inicial.");
        Assert.Less(Vector2.Distance(body.position, spawn), 3f, "O respawn deveria ser perto do ponto inicial.");
    }
}
