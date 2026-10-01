# Platformer 2D - movimentação básica com pulo (Unity 6)

Exercício do curso de Jogos Digitais: movimentação de personagem 2D com inputs e física (sem animação).

- **Unity:** 6000.0.84f1 (URP 2D)
- **Cena:** `Assets/Scenes/PlatformerDemo.unity`
- **Código:** `Assets/Platformer2D/Scripts/`
- **Testes:** `Assets/Platformer2D/Tests/PlayMode/` (Window > General > Test Runner > PlayMode)

## Controles

| Ação  | Teclas            |
|-------|-------------------|
| Andar | Setas ou A / D    |
| Pular | Espaço            |

## Como funciona

- `PlayerController` usa `Rigidbody2D`: o script só define velocidades; gravidade e colisões ficam com a física da Unity.
- O pulo só é permitido no chão (`OverlapCircle` no `groundCheck`, na layer `Ground`).
- A altura do pulo é configurável em unidades (`v = sqrt(2·g·h)`).
- Extras de game feel, ajustáveis no Inspector: altura de pulo variável (soltar o botão corta o pulo), *coyote time* e *jump buffer*.
- `CameraFollow` segue o jogador com suavização; cair da fase faz o personagem renascer.

## Fase

O chão, as paredes e as plataformas são um `Tilemap` (layer `Ground`) com `TilemapCollider2D` + `CompositeCollider2D`.
Os tiles ficam em `Assets/Platformer2D/Tiles/`.

## Plataforma que anda e cai

`MovingFallingPlatform` (objeto `MovingPlatform`, no alto à esquerda da fase; dá para pular nela a partir do degrau mais alto da esquerda).

1. Começa parada e sem gravidade: `Rigidbody2D` Kinematic, com collider.
2. Fica imóvel até o jogador encostar nela.
3. Ao ser tocada, anda em linha reta (direita, 2 u/s) a velocidade constante.
4. O movimento dura exatamente 5 s, contados em passos de física (250 passos de 0,02 s), não em frames.
5. Ao fim dos 5 s zera a velocidade horizontal, trava o eixo X e vira `Dynamic` com gravidade: cai na vertical.
6. A ativação acontece uma vez só. Novos contatos durante o movimento ou a queda são ignorados.

Direção, velocidade, duração e gravidade da queda ficam no Inspector. Extra: com `Carry Riders` ligado o jogador em cima é levado junto enquanto ela anda.

## Testes automatizados

10 testes de PlayMode. Movimentação (6): cair e pousar, altura do pulo, sem pulo duplo, pulo curto ao soltar o botão, velocidade horizontal e respawn. Plataforma (4): parada até ser tocada, 5 s a velocidade constante e depois queda, sem reiniciar ao ser tocada na queda, e jogador levado junto.

## Assets

Arte do pack **Pixel Adventure 1** (Pixel Frog): personagem Ninja Frog, tileset de terreno, fundo e sprite da plataforma.
O pack fica em `Assets/Pixel Adventure 1/`. Foi usado 16 pixels por unidade nas texturas do personagem, do terreno, do fundo e da plataforma.
