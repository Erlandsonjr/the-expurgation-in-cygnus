# The Expurgation in Cygnus

> Jogo de ação e sobrevivência com mecânicas *roguelike* e estética *pixel art* retro-futurista, desenvolvido na Unity como projeto de faculdade.

[![Unity](https://img.shields.io/badge/Unity-6-black?logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-darkgreen?logo=csharp&logoColor=white)](https://docs.microsoft.com/dotnet/csharp/)
[![Platform](https://img.shields.io/badge/Platform-Windows-blue?logo=windows&logoColor=white)](https://github.com/Erlandsonjr/the-expurgation-in-cygnus)
[![GDD](https://img.shields.io/badge/Documento-GDD%20PDF-red?logo=adobeacrobatreader&logoColor=white)](GDD%20The%20Expurgation%20in%20Cygnus.pdf)

---

## Sobre o Jogo

O planeta minerador de **Cygnus** foi invadido pela ameaça alienígena **Arkano**. Assuma o controle do **Purger**, o último defensor do planeta, e lute pela sobrevivência contra hordas crescentes de invasores ao longo de **10 ondas de combate intenso**.

Cada onda sobrevivida desbloqueia uma **Matriz de Escolha** — cartas de upgrade aleatórias que moldam seu estilo de combate em tempo real.

---

## Características Principais

- **Combate em ondas** — 10 rounds com dificuldade progressiva
- **Matriz de Escolha (Upgrades)** — escolha entre cartas aleatórias ao fim de cada onda (Dano, Velocidade, Cura)
- **Arsenal diversificado** — *Heavy Rifle*, *Explosive Bow* e *Laser Pistol*
- **Dash tático** — esquiva direcional com cooldown para reposicionamento em combate
- **Trilha sonora original** — *synth-rock* atmosférico e cibernético composto para o jogo
- **Pixel art 32-bit** retro-futurista em cenário sci-fi

---

## Controles

| Tecla / Botão | Ação |
|---|---|
| `A` / `D` | Mover Esquerda / Direita |
| `Espaço` | Pular |
| `LMB` (clique esq.) | Atirar |
| `RMB` (clique dir.) | Dash (Esquiva Tática) |
| `ESC` | Pausar |

---

## Como Jogar (Build Windows)

1. Faça o download do arquivo `build.zip` deste repositório.
2. Extraia **todo o conteúdo** do `.zip` em uma pasta.
3. Execute `The Expurgation in Cygnus.exe`.

> O executável **precisa estar na mesma pasta** que a pasta `_Data/` — não o mova separadamente ou o jogo não abrirá.

---

## Tecnologias e Ferramentas

| Ferramenta | Uso |
|---|---|
| Unity 6 | Engine e editor principal |
| C# | Scripts e toda lógica de jogo |
| Universal Render Pipeline (URP) | Rendering e pós-processamento visual |
| TextMesh Pro | UI e tipografia |
| Unity Input System | Captura de inputs do jogador |

---

## Estrutura do Projeto

```
Assets/
├── _Scripts/            # Scripts C# (player, inimigos, wave system, upgrades)
├── _Animations/         # Animation Controllers e clips
├── _Audio/              # Trilhas sonoras e efeitos
├── _Prefabs/            # Prefabs de inimigos, projéteis e UI
├── _ScriptableObjects/  # Dados de upgrades e configurações de armas
├── _Sprites/            # Pixel art (personagens, tiles, UI)
├── _Settings/           # Configurações URP e Input System
└── Scenes/              # Cenas Unity (menu, gameplay)
```

---

## Documentação

O projeto conta com um **Game Design Document (GDD)** completo disponível neste repositório:

[GDD The Expurgation in Cygnus.pdf](GDD%20The%20Expurgation%20in%20Cygnus.pdf)

---

## Equipe

Desenolvido por **Erlandson Junior** e **Aderson Mesquita** como projeto da disciplina de **Desenvolvimento de Jogos Digitais**.
