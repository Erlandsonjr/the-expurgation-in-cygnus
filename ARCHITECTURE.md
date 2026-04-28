# ARCHITECTURE.md - The Expurgation in Cygnus

## 1. Visão Geral e Core Loop
- **Gênero:** Action Roguelike / Platformer Arena Shooter 2D.
- **Core Loop:** O jogador (Purger) deve sobreviver a 10 ondas de inimigos limitados em uma arena. Ao eliminar o último inimigo de uma onda, o jogo entra em estado de interrupção (`Time.timeScale = 0`) e um painel de Upgrades (3 opções aleatórias) é exibido. A progressão culmina na Onda 10 contra o Boss (Atrokcidus).

## 2. Entidades e Comportamentos (IA)
### 2.1. Player (Purger)
- **Movimentação:** Física 2D responsiva com Rigidbody2D (movimento horizontal e pulo).
- **Combate:** Mira tridimensional em 360 graus independente da movimentação, ditada pela posição do cursor/mouse.
- **Sobrevivência:** 5 corações vitais. Ao receber dano, ativa invulnerabilidade temporária (iFrames) e recebe knockback omnidirecional (`velocity = Vector2.zero` seguido de `AddForce` em modo `Impulse`).

### 2.2. Inimigos (Horda)
- **Arkano Scout (Físico/Melee):**
  - **IA:** Rastreador de solo (Máquina de Estados Básica). Move-se no eixo X em direção ao player.
  - **Visual:** Cor Vermelha (`#FF0000`). Material `Sprites-Default` (Unlit).
- **Arkano Sniper (Voador/Ranged):**
  - **Restrição de Spawn:** Nunca é instanciado no Ponto Sul (South).
  - **IA:** Mantém uma distância fixa acima do jogador (ex: `Y + 6` unidades). Segue o eixo X do jogador com suavização (Lerp).
  - **Ataque:** Dispara o prefab `EnemyProjectile` na direção vetorial `(player.position - firePoint.position).normalized` usando Object Pooling ou Timer Simples.
  - **Visual:** Cor Ciano (`#00F3FF`). Material `Sprites-Default` (Unlit).

## 3. Sistemas Arquiteturais
### 3.1. Gerenciador de Ondas (Wave System V2)
- **Estrutura:** Array de `WaveData` contendo `enemyCount`, `spawnRate`, e `allowedEnemyPrefabs`.
- **Progressão Curva:**
  - Onda 1: 7 Inimigos (Apenas Scouts).
  - Onda 2+: Introdução de Snipers; redução gradual do `spawnRate`.

### 3.2. Sistema de Upgrades (Semana 5)
- **Data Architecture:** Orientado a dados via `ScriptableObject` para facilitar balanceamento sem recompilação.
- **UI Flow:** Modais exibidas na transição de onda. O jogador escolhe 1 entre 3 opções sorteadas.
- **Modificadores:** Adição de projéteis extras, penetração, aumento de cadência de fogo, cura ou velocidade de movimento.