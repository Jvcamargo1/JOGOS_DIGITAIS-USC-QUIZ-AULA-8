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

## Testes automatizados

6 testes de PlayMode: cair e pousar, altura do pulo, sem pulo duplo, pulo curto ao soltar o botão, velocidade horizontal e respawn.

## Assets

Arte do pack **Pixel Adventure 1** (Pixel Frog): personagem Ninja Frog, tileset de terreno e fundo.
O pack fica em `Assets/Pixel Adventure 1/`. Foi usado 16 pixels por unidade nas texturas do personagem, do terreno e do fundo.
