# 🎮 Relic Hunter: The Escaping Thief

A turn-based, grid-based stealth and pursuit game developed in Unity that demonstrates advanced game AI techniques.

**📚 Course**: PUP CS 3-2 2526, COSC 304 - Introduction to Artificial Intelligence  
**👥 Team**: Cabbadu, Jocson, Lambohon, & Salgado

---

## 📁 Project Structure

```
Assets/_Project/
├─ Scenes/          → Game levels and environments
├─ Scripts/         → All gameplay and system logic
│  ├─ AI/           → Pathfinding and decision algorithms
│  ├─ Core/         → Game management and core systems
│  ├─ Player/       → Player controller logic
│  ├─ Enemy/        → Enemy AI behavior
│  └─ UI/           → User interface management
├─ Prefabs/         → Reusable game object templates
├─ Sprites/         → 2D graphics and visual assets
└─ Audio/           → Sound effects and music
```

---

## 👥 Team Ownership & Responsibilities

| Member       | Role                      | Primary Responsibilities                                   | Key Files                                                                                       |
| ------------ | ------------------------- | ---------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| **Jocson**   | 🤖 AI Algorithms & Review | Algorithm implementation, code review, merge coordination  | `AI/AStar.cs`, `AI/Minimax.cs`, `AI/AlphaBeta.cs`, `Core/GameManager.cs`, `Core/TurnManager.cs` |
| **Cabbadu**  | 🎯 Enemy AI & Behavior    | AI behavior tuning, enemy balancing, strategic refinement  | `AI/Heuristics.cs`, `AI/PathNode.cs`, `AI/AIState.cs`, `Enemy/GuardController.cs`               |
| **Lambohon** | 🎨 Visual Assets & UI     | Level layout, visual organization, asset management        | `Prefabs/`, `Sprites/`, `Audio/`, `UI/HUDController.cs`                                         |
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
