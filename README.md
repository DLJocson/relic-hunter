# 🏺 Relic Hunter: The Escaping Thief

A bite-sized, turn-based stealth game where a nimble thief must navigate procedurally generated mazes, steal a sacred relic, and escape while outsmarting an AI-controlled guard. The project uses a hybrid AI approach built around A\* Search and Minimax with Alpha-Beta Pruning.

---

## ✨ Overview

**Relic Hunter: The Escaping Thief** is a 2D, grid-based chase game set in a maze-like environment. The player takes the role of the thief, carries the stolen relic, and attempts to reach the exit tile. The guard pursues the player using a hybrid AI pipeline that combines **Minimax** for strategic decision-making and **A\*** for pathfinding.

The game is designed to create strategic depth through:

- turn-based movement,
- temporary barricades,
- procedural maze variation,
- and adaptive guard pursuit.

---

## ✨ Features

| Feature                 | Description                                                                                                   |
| :---------------------- | :------------------------------------------------------------------------------------------------------------ |
| **Turn-Based Tension**  | The player and guard alternate turns on a grid, making every move important.                                  |
| **Dynamic Stealth**     | Use barricades and maze geometry to delay pursuit and create safer routes.                                    |
| **Hybrid Guard AI**     | The guard uses **Minimax with Alpha-Beta Pruning** for tactical decisions and **A\*** for efficient movement. |
| **Procedural Rounds**   | Each round can change wall placement, path layout, and exit tile location.                                    |
| **Difficulty Scaling**  | Guard speed, barricade duration, barricade limit, and minimax depth increase across rounds.                   |
| **Grid-Based Strategy** | Movement is limited to legal tiles in a maze-like 2D grid.                                                    |

---

## 🎮 How to Play

### Controls

|     Input      | Action          | Details                                                  |
| :------------: | :-------------- | :------------------------------------------------------- |
|    **WASD**    | Move            | Move the thief one tile in the four cardinal directions. |
| **Arrow Keys** | Place Barricade | Select an adjacent tile for barricade placement.         |

### Core Gameplay Rules

- The game is **turn-based** and **grid-based**.
- On the player’s turn, the player may either:
  - move one tile in a cardinal direction, or
  - place a temporary barricade on an adjacent accessible tile.
- Barricades cannot overlap with existing walls or the AI guard, and they disappear after a fixed number of turns.
- The guard cannot pass through permanent walls or active barricades.

---

## 🧱 Win & Loss Conditions

| Outcome        | Criteria                                                                                        |
| :------------- | :---------------------------------------------------------------------------------------------- |
| 🏆 **Victory** | Reach the exit tile while carrying the relic, or survive until the mode-specific timer expires. |
| 💀 **Defeat**  | The guard moves onto the player’s tile, or the player is trapped with no legal moves.           |

---

## 🧠 Guard AI

The guard uses a hybrid search system:

- **Minimax with Alpha-Beta Pruning** predicts the player’s likely future moves and evaluates tactical choices.
- **A\*** computes the shortest legal path through the maze toward the selected target.

### Guard AI behavior

- The guard is the maximizing player in Minimax, while the thief is the minimizing player.
- The thief may either move or place a barricade during simulated turns.
- The AI recomputes paths when barricades change, allowing it to react to dynamic obstacles.
- The guard AI subsystem is organized as an input → process → output pipeline with perception, decision-making, path request, action selection, and output stages.

---

## 🗺️ Round Structure & Difficulty Scaling

The game consists of **three rounds**. A player or the AI wins the match by taking **2 out of 3 rounds** or winning all 3. These values match the default round settings defined in `Assets/_Project/Scripts/Core/GameManager.cs` when the scene does not override them.

| Parameter                    |   Round 1 (Easy)    |    Round 2 (Medium)    |   Round 3 (Difficult)   |
| :--------------------------- | :-----------------: | :--------------------: | :---------------------: |
| Guard speed (tiles per turn) |          1          |          1.25          |          1.34           |
| Barricade duration (turns)   |          6          |           5            |            4            |
| Maximum active barricades    |          4          |           3            |            2            |
| Minimax search depth         |          1          |           2            |            3            |
| Grid size / complexity       | 9×9, wide corridors | 12×12, mixed corridors | 15×15, narrow corridors |

---

## 🧩 Project Architecture

The project is modularly organized under `Assets/_Project/`:

```text
Assets/_Project/
├── Scenes/        → Main game scene; Dev/ folder for maze-only tests
├── Scripts/
│   ├── AI/        → A* Pathfinding, Minimax, guard decision logic
│   ├── Core/      → GameManager, GridManager, TurnManager
│   ├── Maze/      → Procedural maze generation and room prefabs
│   ├── Player/    → Player controller, inventory, interactions
│   ├── Enemy/     → Guard controller and detection behaviors
│   └── UI/        → HUD, round feedback, menus
├── Prefabs/       → Player, guard, maze pieces, visual helpers
└── Audio/         → Music and sound effects
```

---

## 🚀 Getting Started

1. Clone the repository to your local machine.
2. Open the project in Unity.
3. Open the Main scene located under Assets/\_Project/Scenes/.
4. Run the game to test the full turn-based loop.
5. Use the Dev/ scenes to test isolated systems like maze generation or AI pathfinding.

---

## 🛠️ Development Notes

The implementation was planned in phases: Design, Prototyping, Core Build & AI Coding, Testing, and Polishing.

The AI implementation targets:

- A\* Search for navigation.
- Minimax with Alpha-Beta Pruning for tactical prediction.
- Unity as the engine and C# as the programming language.

---

## 🎓 Academic Context

This project was developed as a case study for:

| Institution Details |                                                                               |
| :------------------ | :---------------------------------------------------------------------------- |
| **University**      | Polytechnic University of the Philippines, Sta. Mesa, Manila                  |
| **College**         | College of Computer and Information Sciences                                  |
| **Course**          | COSC 304 – Introduction to Artificial Intelligence (BSCS 3-2, A.Y. 2025–2026) |

### Development Team

| Name                           | Role(s)                         |
| :----------------------------- | :------------------------------ |
| **Dan Louie M. Jocson**        | Lead Programmer & AI Programmer |
| **Elija C. Cabaddu**           | AI Programmer                   |
| **Ashley H. Lambohon**         | Game Designer & Art Lead        |
| **Samantha Marion C. Salgado** | Engine Developer & QA Tester    |

---

## 🧪 Technologies Used

| Component            | Tools / Technologies                        |
| :------------------- | :------------------------------------------ |
| Algorithms           | A\* Search, Minimax with Alpha-Beta Pruning |
| Game Engine          | Unity                                       |
| Programming Language | C#                                          |
| IDE                  | Visual Studio Code                          |
| Version Control      | GitHub                                      |
| Art / Audio Assets   | Open source websites and resources          |
