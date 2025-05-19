# SudokuVS

# Sudoku Game Documentation

## English Version

### Overview
This is a feature-rich Sudoku game implemented in Unity. The game includes both single-player and multiplayer modes with network functionality, special skills, and an intuitive user interface. Players can enjoy traditional Sudoku gameplay with additional interactive elements that enhance the gaming experience.

### Core Features
- Single-player Sudoku with multiple difficulty levels
- Multiplayer mode with real-time networking
- Special skills that affect gameplay (Fire, Grass, Wind)
- Interactive UI with animations and visual effects
- Time tracking and game state management
- Undo feature for move history

### Document Summaries

#### CellManager.cs
A component that manages each cell in the Sudoku grid.
- **Functions**:
  - `Init`: Initializes the cell with row, column, and UI references
  - `OnPointerClick`: Handles cell click events
  - `SetNumber`: Updates the displayed number
  - `ClearNumber`: Removes the displayed number
  - `SetColor`: Changes the text color based on cell state (fixed, conflict, normal)
  - `SetHighlight`: Toggles highlighting for selected cells

#### EventTriggerListener.cs
Extends Unity's EventTrigger to provide easier event handling for UI elements.
- **Functions**:
  - `Get`: Static method to get or add an EventTriggerListener to a GameObject
  - `OnPointerEnter`: Invokes custom action when pointer enters
  - `OnPointerExit`: Invokes custom action when pointer exits

#### GameManager.cs
The core game logic manager that handles Sudoku generation, gameplay, and validation.
- **Functions**:
  - `StartNewGame`: Creates a new Sudoku puzzle with specified difficulty
  - `CreateAndSetupSudokuBoard`: Creates and positions the Sudoku board
  - `GenerateSudokuSolution`: Creates a valid Sudoku solution using backtracking
  - `SolveSudoku`: Recursive algorithm to solve Sudoku
  - `SetCellValue`: Updates a cell's value with validation and network sync
  - `UndoLastMove`: Reverses the last move made
  - `HasConflict`: Checks if a number conflicts with Sudoku rules
  - `UpdateCellUI`: Updates the visual state of a cell
  - `CheckGameCompletion`: Verifies if the puzzle is completed correctly

#### HintController.cs
Provides highlighting and hints for numbers in the Sudoku grid.
- **Functions**:
  - `OnHoverNumber`: Highlights all cells containing the hovered number
  - `OnExitNumber`: Removes highlighting when mouse exits
  - `OnClickNumber`: Handles number button clicks

#### MenuManager.cs
Manages game UI panels and navigation between different game modes.
- **Functions**:
  - `OnSingleModeButtonClicked`: Initiates single-player mode
  - `OnMultiModeButtonClicked`: Initiates multiplayer mode
  - `StartGameWithDifficulty`: Starts a game with selected difficulty
  - `ShowMultiplayerGamePanel`: Switches to multiplayer UI
  - `BackToMainMenu`: Returns to main menu with proper cleanup
  - `RestartGame`: Resets the current game
  - `QuitGame`: Exits the application

#### MultiplayerCellManager.cs
Extends the CellManager for multiplayer functionality, adding opponent visualization.
- **Functions**:
  - `SetBackgroundColor`: Shows opponent moves with colors
  - `DisableInteraction`: Prevents interaction with opponent board
  - `EnableInteraction`: Restores interaction capabilities

#### NetworkGameManager.cs
Handles network synchronization for multiplayer games.
- **Functions**:
  - `InitializeGame`: Sets up game state for all connected players
  - `SetupMultiplayerBoards`: Creates player and opponent boards
  - `NotifyMove`: Sends move updates over the network
  - `CheckGameCompleted`: Checks if the multiplayer game is finished

#### NetworkManagerExtension.cs
Empty class, appears to be a placeholder for future network extensions.

#### NetworkManagerUI.cs
Manages UI components related to networking and room management.
- **Functions**:
  - `CreateRoom`: Creates a new multiplayer room
  - `JoinRoom`: Connects to an existing room
  - `CopyRoomCodeToClipboard`: Copies room code for sharing
  - `DisconnectFromNetwork`: Handles proper disconnection

#### NetworkPlayerController.cs
Manages player roles and identity in multiplayer games.
- **Functions**:
  - `AssignPlayerRoles`: Sets players as host or guest
  - `UpdatePlayerUI`: Updates UI based on player role
  - `SetPlayerName`: Sets the player's display name

#### PlayerSignalUI.cs
Manages UI for displaying player signals and communication.
- **Functions**:
  - `OnSendSignalButtonClicked`: Sends a signal to other players
  - `DisplayLocalMessage`: Shows local notification messages
  - `OnSignalReceived`: Handles incoming signals from other players

#### PlayerSignalManager.cs
Handles communication between players in multiplayer mode.
- **Functions**:
  - `SendButtonPressSignal`: Sends button press notifications
  - `SendGameStartSignal`: Notifies game start
  - `SendGameEndSignal`: Notifies game end
  - `SendSignal`: Generic method to send custom signals

#### SkillManager.cs
Manages special skills that affect gameplay.
- **Functions**:
  - `ActivateFireSkill`: Highlights a 3x3 subgrid
  - `ActivateGrassSkill`: Creates a fake number in a random cell
  - `ActivateWindSkill`: Rotates the entire Sudoku grid
  - `ShowFireEffectInSubgrid`: Displays fire animation
  - `ShowGrassEffectInCell`: Shows grass animation
  - `RotateGrid`: Animates grid rotation with cell contents

#### SudokuGridSpawner.cs
Creates and manages the Sudoku grid UI elements.
- **Functions**:
  - `CreateSudokuGrid`: Generates the 9x9 grid of cells
  - `OnCellClicked`: Handles cell selection
  - `OnNumberClicked`: Processes number input
  - `ClearSelection`: Deselects the current cell
  - `GetRandomCell`: Returns random cell positions for skills

#### Timer.cs
Tracks and displays game time.
- **Functions**:
  - `StartTimer`: Starts time tracking
  - `PauseTimer`: Pauses the timer
  - `ResetTimer`: Resets time to zero
  - `GetTime`: Returns elapsed time

---

## 中文版本

### 概述
这是一个使用Unity实现的功能丰富的数独游戏。游戏包括单人模式和多人模式，具有网络功能、特殊技能和直观的用户界面。玩家可以享受传统数独游戏玩法，同时还有额外的交互元素来增强游戏体验。

### 核心功能
- 具有多个难度级别的单人数独
- 具有实时网络的多人模式
- 影响游戏玩法的特殊技能（火焰、小草、风吹）
- 具有动画和视觉效果的交互式UI
- 时间跟踪和游戏状态管理
- 移动历史的撤销功能

### 文档摘要

#### CellManager.cs
管理数独网格中每个单元格的组件。
- **功能**:
  - `Init`: 使用行、列和UI引用初始化单元格
  - `OnPointerClick`: 处理单元格点击事件
  - `SetNumber`: 更新显示的数字
  - `ClearNumber`: 移除显示的数字
  - `SetColor`: 根据单元格状态（固定、冲突、正常）更改文本颜色
  - `SetHighlight`: 切换选定单元格的高亮显示

#### EventTriggerListener.cs
扩展Unity的EventTrigger，为UI元素提供更简单的事件处理。
- **功能**:
  - `Get`: 静态方法，获取或添加EventTriggerListener到GameObject
  - `OnPointerEnter`: 当指针进入时调用自定义操作
  - `OnPointerExit`: 当指针退出时调用自定义操作

#### GameManager.cs
核心游戏逻辑管理器，处理数独生成、游戏玩法和验证。
- **功能**:
  - `StartNewGame`: 创建指定难度的新数独谜题
  - `CreateAndSetupSudokuBoard`: 创建并定位数独板
  - `GenerateSudokuSolution`: 使用回溯法创建有效的数独解决方案
  - `SolveSudoku`: 解决数独的递归算法
  - `SetCellValue`: 更新单元格值，进行验证和网络同步
  - `UndoLastMove`: 撤销上一步操作
  - `HasConflict`: 检查数字是否与数独规则冲突
  - `UpdateCellUI`: 更新单元格的视觉状态
  - `CheckGameCompletion`: 验证谜题是否正确完成

#### HintController.cs
为数独网格中的数字提供高亮和提示。
- **功能**:
  - `OnHoverNumber`: 高亮显示所有包含悬停数字的单元格
  - `OnExitNumber`: 当鼠标退出时移除高亮
  - `OnClickNumber`: 处理数字按钮点击

#### MenuManager.cs
管理游戏UI面板和不同游戏模式之间的导航。
- **功能**:
  - `OnSingleModeButtonClicked`: 启动单人模式
  - `OnMultiModeButtonClicked`: 启动多人模式
  - `StartGameWithDifficulty`: 以选定难度开始游戏
  - `ShowMultiplayerGamePanel`: 切换到多人UI
  - `BackToMainMenu`: 正确清理后返回主菜单
  - `RestartGame`: 重置当前游戏
  - `QuitGame`: 退出应用程序

#### MultiplayerCellManager.cs
扩展CellManager以实现多人功能，添加对手可视化。
- **功能**:
  - `SetBackgroundColor`: 用颜色显示对手的移动
  - `DisableInteraction`: 防止与对手棋盘交互
  - `EnableInteraction`: 恢复交互能力

#### NetworkGameManager.cs
处理多人游戏的网络同步。
- **功能**:
  - `InitializeGame`: 为所有连接的玩家设置游戏状态
  - `SetupMultiplayerBoards`: 创建玩家和对手棋盘
  - `NotifyMove`: 通过网络发送移动更新
  - `CheckGameCompleted`: 检查多人游戏是否完成

#### NetworkManagerExtension.cs
空类，似乎是未来网络扩展的占位符。

#### NetworkManagerUI.cs
管理与网络和房间管理相关的UI组件。
- **功能**:
  - `CreateRoom`: 创建新的多人房间
  - `JoinRoom`: 连接到现有房间
  - `CopyRoomCodeToClipboard`: 复制房间代码以分享
  - `DisconnectFromNetwork`: 处理正确断开连接

#### NetworkPlayerController.cs
管理多人游戏中的玩家角色和身份。
- **功能**:
  - `AssignPlayerRoles`: 将玩家设置为主机或客人
  - `UpdatePlayerUI`: 根据玩家角色更新UI
  - `SetPlayerName`: 设置玩家的显示名称

#### PlayerSignalUI.cs
管理显示玩家信号和通信的UI。
- **功能**:
  - `OnSendSignalButtonClicked`: 向其他玩家发送信号
  - `DisplayLocalMessage`: 显示本地通知消息
  - `OnSignalReceived`: 处理来自其他玩家的传入信号

#### PlayerSignalManager.cs
处理多人模式下玩家之间的通信。
- **功能**:
  - `SendButtonPressSignal`: 发送按钮按下通知
  - `SendGameStartSignal`: 通知游戏开始
  - `SendGameEndSignal`: 通知游戏结束
  - `SendSignal`: 发送自定义信号的通用方法

#### SkillManager.cs
管理影响游戏玩法的特殊技能。
- **功能**:
  - `ActivateFireSkill`: 高亮显示3x3子网格
  - `ActivateGrassSkill`: 在随机单元格中创建假数字
  - `ActivateWindSkill`: 旋转整个数独网格
  - `ShowFireEffectInSubgrid`: 显示火焰动画
  - `ShowGrassEffectInCell`: 显示草动画
  - `RotateGrid`: 使用单元格内容动画旋转网格

#### SudokuGridSpawner.cs
创建和管理数独网格UI元素。
- **功能**:
  - `CreateSudokuGrid`: 生成9x9的单元格网格
  - `OnCellClicked`: 处理单元格选择
  - `OnNumberClicked`: 处理数字输入
  - `ClearSelection`: 取消选择当前单元格
  - `GetRandomCell`: 返回技能的随机单元格位置

#### Timer.cs
跟踪和显示游戏时间。
- **功能**:
  - `StartTimer`: 开始时间跟踪
  - `PauseTimer`: 暂停计时器
  - `ResetTimer`: 将时间重置为零
  - `GetTime`: 返回经过的时间