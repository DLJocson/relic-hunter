# 🎮 Relic Hunter: The Escaping Thief

A turn-based, grid-based stealth and pursuit game developed in Unity that demonstrates advanced game AI techniques.

**📚 Course**: PUP CS 3-2 2526, COSC 304 - Introduction to Artificial Intelligence  
**👥 Team**: Cabbadu, Jocson, Lambohon, & Salgado

---

## 📁 Project Structure

```
Assets/_Project/
├─ Scenes/              → Main game scene; Dev/ for optional maze-only tests
├─ Scripts/
│  ├─ AI/               → A*, Minimax; Editor/ for manual AI tests
│  ├─ Core/             → GameManager, GridManager, TurnManager, MazeGridBridge
│  ├─ Maze/             → Procedural maze (GenerateMaze, Room, PlayerStatus)
│  ├─ Player/           → Player controller
│  ├─ Enemy/            → Guard controller
│  └─ UI/               → Round feedback (sprites, audio)
├─ Resources/Prefabs/   → Runtime-loaded barricade & exit tile prefabs
├─ Prefabs/
│  ├─ Entities/         → Guard gameplay prefab
│  ├─ Maze/             → Room, player/guard visual prefabs
│  └─ Legacy/           → Reserved for deprecated flat-grid assets
├─ Sprites/Maze/        → Player/guard sprite sheets
└─ Audio/
   ├─ Music/            → Background, win, lose clips
   └─ SFX/              → Gameplay sound effects
```

---

## 👥 Team Ownership & Responsibilities

| Member       | Role                      | Primary Responsibilities                                   | Key Files                                                                                       |
| ------------ | ------------------------- | ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| **Jocson**   | 🤖 AI Algorithms & Review | Algorithm implementation, code review, merge coordination  | `AI/AStar.cs`, `AI/Minimax.cs`, `AI/AlphaBeta.cs`, `Core/GameManager.cs`, `Core/TurnManager.cs` |
| **Cabbadu**  | 🎯 Enemy AI & Behavior    | AI behavior tuning, enemy balancing, strategic refinement  | `AI/Heuristics.cs`, `AI/PathNode.cs`, `AI/AIState.cs`, `Enemy/GuardController.cs`               |
| **Lambohon** | 🎨 Visual Assets & UI     | Level layout, visual organization, asset management        | `Prefabs/`, `Sprites/`, `Audio/`, `UI/RoundFeedbackController.cs`                              |
| **Salgado**  | 🛠️ Core Systems & QA      | Grid system, player control, testing, bug tracking, builds | `Core/GridManager.cs`, `Player/PlayerController.cs`, `UI/`, `ProjectSettings/`, `README.md`     |

### Detailed Responsibilities

**🤖 Jocson – AI Pathfinding & Algorithm Review**

- Implements A\* pathfinding algorithm
- Develops minimax and alpha-beta pruning logic
- Manages game initialization and turn system
- **Handles**: Code reviews and script merges

**🎯 Cabbadu – Enemy AI & Behavior Tuning**

- Creates pathfinding heuristics and node systems
- Implements AI state management
- Tunes guard behavior and difficulty balancing
- **Handles**: Enemy intelligence and strategic behavior

**🎨 Lambohon – Visual Assets & UI**

- Designs and organizes all prefabs and visual elements
- Creates sprite assets and audio
- Builds the HUD/UI interface
- **Handles**: Level aesthetics and user experience

**🛠️ Salgado – Core Systems, Testing & Build**

- Implements grid system and player mechanics
- Develops UI architecture
- Manages testing, bug tracking, and documentation
- **Handles**: Quality assurance and build verification

---

## 📝 Development Guidelines

- 📂 Keep all AI implementations in the `AI/` folder
- 🏷️ Use descriptive naming for all game objects and scripts
- 💬 Document complex algorithms with clear inline comments
- ✅ Test thoroughly before submitting pull requests
- 🐛 Report bugs with reproduction steps to the team
- 🔄 Communicate with team members before major changes
- 📖 Update documentation when modifying systems
