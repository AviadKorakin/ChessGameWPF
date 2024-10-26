# ♟️ **WPF Chess Game with ASP.NET Server Integration**  

Welcome to the **WPF Chess Game Project**, an immersive chess experience blending modern aesthetics with traditional design. This project leverages **WPF** for an elegant client interface and **ASP.NET Core** for seamless backend integration. Below is a breakdown of the key features, technologies, and design elements that make this project stand out.

---

## 🎮 **Game Overview: חצי שח (Half Chess)**  

This custom chess variant delivers a refreshing gameplay experience with **advanced animations**, **3D chess pieces**, and **real-time interactions**.  

- **Board Setup**:  
  - **8x4 chessboard**, providing a half-sized challenge compared to a standard board.  
  - **Pieces**: King, Bishop, Knight, Rook, and pawns designed to align with the new board dimensions.  

- **Unique Rules**:  
  - Pawns move **horizontally**, though they cannot capture in this direction.  
  - **En passant**, **pawn promotions**, and **castling** are supported and validated.  

- **Turn Timer**:  
  - Players must **complete their moves** within a time limit or forfeit the game.  

---

🎥 **Watch the Demo Videos**:
[![ChessGameWPF-Gameplay](https://img.youtube.com/vi/51A4xsYjNX4/0.jpg)](https://www.youtube.com/watch?v=51A4xsYjNX4)  
[![ChessGameWPF-Records](https://img.youtube.com/vi/WM0rT5jcTt0/0.jpg)](https://www.youtube.com/watch?v=WM0rT5jcTt0)  
(*Click the thumbnail to watch the video.*)

## 🛠️ **WPF Client Features**  

### 🖼️ **Custom Chessboard Design**  
- **Wooden Chessboard Theme**:  
  - The board mimics the **original wooden look** to provide a nostalgic and familiar feel.  

- **3D XAML-Based Chess Pieces**:  
  - Each piece is crafted in **modern 3D designs** for a smooth and immersive user experience.  
  - **Custom animations** give the pieces lifelike movements during captures, moves, and promotions.

- **Custom Board Printing**:  
  - Supports **black and green surface designs** for those seeking a more modern theme.  
  - Players can toggle between classic **wooden board** and **modern themes** for variety.  

### 🎵 **Sound Effects and Interaction**  
- **Distinct Sounds for Each Action**:  
  - Custom sound effects for **piece movements**, **captures**, and **check** scenarios.  
  - Feedback sounds for **UI actions**, such as clicking comboboxes or selecting inputs.  

- **Animations on Move and Capture**:  
  - **Smooth transitions** for player turns and movements.  
  - **Visual indicators** highlight legal moves when a piece is selected.

---

## 🎛️ **Modern WPF UI Components**  

### 🌟 **Custom Inputs and Comboboxes**  
- **Themed Dropdowns**: Comboboxes styled to match the board aesthetics.  
- **Player Input Fields**: Enhanced with **animations** and **placeholder hints** for improved usability.  

### 🎨 **XAML for Design and Styling**  
- **Data Binding**: Used to connect game logic with UI elements dynamically.  
- **Resource Dictionaries**: Centralizes styles, ensuring **modularity** and **reusability**.  
- **Templated Controls**: Each piece uses **3D XAML templates** for a modern touch while maintaining high performance.  

---

## 🧠 **Game Logic and Board Interaction**  

### 🎯 **Move Suggestions and Real-Time Validation**  
- **Highlighted Move Indicators**: Visual hints for valid moves upon selecting a piece.  
- **Real-Time Validation**: Prevents illegal moves, ensuring smooth gameplay and correct rule enforcement.  

### 📝 **Move Recording and Undo/Redo Functionality**  
- **Move Tracking**: Every game step is saved locally and sent to the server.  
- **Undo and Redo Options**: Allows players to revert or replay previous moves during gameplay.  

---

## 🌐 **ASP.NET Core Server Integration**  

### 📬 **Client-Server Communication**  
- **HTTP Requests**: The WPF client sends **game state updates and move requests** via RESTful APIs.  
- **Server Response Handling**: Client processes server responses to **update the board** and display the next player’s moves.

### 📦 **Server-Side Game Management**  
- **Game State Management**:  
  - Handles **pawn promotion**, **checkmate detection**, and **special moves** like castling.  
  - Stores game logs, player statistics, and move history.  
- **Database Integration**:  
  - Uses **SQL Server** to manage **players, games, and game steps** efficiently.  

---

## 🚀 **Key Features for Recruiters**  

### ⚙️ **Advanced Game Logic with Parallel Processing**  
- **Custom Rules Support**: Includes **en passant**, **castling**, and **pawn promotions**.  
- **Parallel Loops**: Uses **Parallel.ForEach** to optimize board evaluations.  

### 🛠️ **Backend Architecture**  
- **ASP.NET Core** with **ADO.NET** and **Entity Framework**.  
- **RESTful APIs** for seamless client-server interaction.  

### 🎨 **UI Excellence with 3D Animations**  
- **3D Chess Pieces**: Crafted in **XAML** for a modern look and smooth animations.  
- **Color Themes**: Offers a choice between **classic wood** and **modern black-green themes**.  

---

## 🎯 **Recording and Game Management**  

### 📝 **Move Logging to Server**  
- **Game Logs**: Moves are logged locally and sent to the server upon completion.  
- **Winner Updates**: When a winner is determined, the client updates the server immediately.

### 🗂️ **Sample API Routes**  

1. **Make a Move**  
   **POST** `/api/chess/move`  
   - Validates and processes the move. Responds with appropriate messages such as "Checkmate" or "Invalid move."  

2. **Fetch Player Games**  
   **GET** `/api/players/{playerId}/games`  
   - Retrieves all games associated with a player.  

3. **Save Game Steps**  
   **POST** `/api/chess/save-steps`  
   - Logs game moves to the server asynchronously.  

---

## 🧑‍💻 **Technologies Used**  
- **WPF (Windows Presentation Foundation)**: For building a responsive and modern chess client.  
- **ASP.NET Core**: Manages game logic and player interactions on the backend.  
- **SQL Server**: Stores player data, games, and move sequences.  
- **XAML**: Powers the UI with custom templates and 3D chess piece designs.  
- **Parallel Computing**: Enhances performance by evaluating moves concurrently.  

---

## 🎨 **UI Highlights**  

- **Modern 3D Pieces with Animations**: Smoothly animated pieces provide a dynamic user experience.  
- **Interactive Inputs and Comboboxes**: Custom controls enhance the UI with animations and sound feedback.  
- **Multiple Themes**: Toggle between **classic wooden board** and **black-green modern surfaces**.  

---

## 📧 **Contact Information**  
Built with ❤️ by **Aviad Korakin**  
Feel free to reach out: [aviad825@gmail.com](mailto:aviad825@gmail.com)  

---

This project is a showcase of **creativity, technical skill, and design mastery**. It seamlessly integrates **chess game logic, backend APIs, and modern UI elements**, making it an impressive example for recruiters. With its **3D visuals**, **sound effects**, and **real-time gameplay mechanics**, this chess game demonstrates your ability to build engaging, high-performance applications. 🎉
