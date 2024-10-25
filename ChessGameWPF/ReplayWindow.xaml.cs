using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ChessGameWPF.Exceptions;
using ChessGameWPF.Models;

namespace ChessGameWPF
{
    public partial class ReplayWindow : Window
    {
        private readonly GameRecordService _gameRecordService;
        private List<MoveRecord>? _moveRecords;
        private List<GameSummary>? _gameRecords;
        private Chessboard _chessboard = default!;
        private int _currentMoveIndex = 0;
        private DispatcherTimer? _replayTimer;
        private PieceColor _userColor;
        private PieceColor _computerColor;
        private readonly Brush _redHighlight = new SolidColorBrush(Color.FromRgb(180, 30, 30)); // Dark and vibrant red
        private bool _isPaused = false; // Track if the replay is paused

        private readonly LinearGradientBrush _darkSquareGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
                            {
                                new GradientStop((Color)ColorConverter.ConvertFromString("#8B4513"), 0.0), // SaddleBrown-like
                                new GradientStop((Color)ColorConverter.ConvertFromString("#5C4033"), 1.0)  // Dark wood tone
                             }
        };

        // Predefined gradient brushes for light squares (warmer brown tones)
        private readonly LinearGradientBrush _lightSquareGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
                            {
                                new GradientStop((Color)ColorConverter.ConvertFromString("#D2B48C"), 0.0), // Tan
                                new GradientStop((Color)ColorConverter.ConvertFromString("#A67B5B"), 1.0)  // Warm light brown
                            }
        };
        private readonly MediaPlayer _movePlayer = new MediaPlayer(); // MediaPlayer for move sound
        private Border[,] _squareCache = default!;

        public ReplayWindow()
        {
            _gameRecordService = new GameRecordService();
            InitializeComponent();
            PrepareMoveSound(); // Prepare the move sound
            LoadGameOptions();


        }

        private void PrepareMoveSound()
        {
            try
            {
                var moveUri = new Uri("Sounds/move.mp3", UriKind.Relative);
                _movePlayer.Open(moveUri);

                _movePlayer.MediaFailed += (s, e) =>
                {
                    MessageBox.Show($"Error playing 'move' sound: {e.ErrorException.Message}", "Sound Error", MessageBoxButton.OK, MessageBoxImage.Error);
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing move sound: {ex.Message}", "Sound Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Play the move sound
        private void PlayMove()
        {
            _movePlayer.Stop();  // Stop if already playing
            _movePlayer.Position = TimeSpan.Zero; // Reset position to the start
            _movePlayer.Play();
        }
        // Load available games into the ComboBox
        private async void LoadGameOptions()
        {
            try
            {
                _gameRecords = await _gameRecordService.GetAllGamesAsync();
                GameComboBox.ItemsSource = _gameRecords;
                GameComboBox.DisplayMemberPath = nameof(GameSummary.DisplayText);
                GameComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading games: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        // Load the selected game and start replay
        private async void LoadGame_Click(object sender, RoutedEventArgs e)
        {
            if (GameComboBox.SelectedItem is not GameSummary selectedGame)
            {
                MessageBox.Show("Please select a game.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StopReplay(); // Stop any ongoing replay

            try
            {
                LoadingCanvas.Visibility = Visibility.Visible; // Show loading indicator
                _moveRecords = await _gameRecordService.GetMovesForGameAsync(selectedGame.GameId);

                if (_moveRecords.Count == 0)
                {
                    MessageBox.Show("No moves found for this game.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    LoadingCanvas.Visibility = Visibility.Collapsed;
                    return;
                }
                _chessboard = new Chessboard(selectedGame.UserColor==PieceColor.White);
                if(_squareCache==null)
                _squareCache = new Border[_chessboard.Rows, _chessboard.Cols]; // Cache for quick access to squares
                _userColor=selectedGame.UserColor;
                _computerColor=selectedGame.ComputerColor;
                InitializeBoard(); // Reset the board for new game
                _currentMoveIndex = 0;
                RenderBoard();
                StopResumeButton.Background = Brushes.Red;
                StopResumeButton.Content = "⏹"; // Stop icon
                StopResumeButton.Visibility = Visibility.Visible;
                LoadingCanvas.Visibility = Visibility.Collapsed; // Hide loading indicator
                StartReplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading game: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingCanvas.Visibility = Visibility.Collapsed;
            }
        }

        // Stop the ongoing replay
        private void StopReplay()
        {
            if (_replayTimer != null && _replayTimer.IsEnabled)
            {
                _replayTimer.Stop();
            }
            _chessboard = new Chessboard();
            _currentMoveIndex = 0;
       
        }

        // Initialize the chessboard grid
        private void InitializeBoard()
        {
            ChessBoardGrid.Children.Clear();
            ChessBoardGrid.RowDefinitions.Clear();
            ChessBoardGrid.ColumnDefinitions.Clear();

            for (int row = 0; row < 8; row++)
                ChessBoardGrid.RowDefinitions.Add(new RowDefinition());

            for (int col = 0; col < 4; col++)
                ChessBoardGrid.ColumnDefinitions.Add(new ColumnDefinition());

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var square = new Border
                    {
                        Background = (row + col) % 2 == 0 ? _lightSquareGradient : _darkSquareGradient,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.5)
                    };
                    _squareCache[row, col] = square;
                    Grid.SetRow(square, row);
                    Grid.SetColumn(square, col);
                    ChessBoardGrid.Children.Add(square);
                }
            }
        }

        // Start the replay with timed intervals between moves
        private void StartReplay()
        {
            _replayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // 1-second delay for each move
            };
            _replayTimer.Tick += ReplayNextMove;
            _replayTimer.Start();
        }

        // Handle each move replay step
        private async void ReplayNextMove(object? sender, EventArgs e)
        {
            if (_moveRecords == null || _replayTimer==null) return;
            if (_currentMoveIndex >= _moveRecords.Count)
            {
                _replayTimer.Stop();
                await DisplayEndGame();
                return;
            }

            var move = _moveRecords[_currentMoveIndex++];
            var validMoves = _chessboard.Board[move.FromRow][ move.FromCol]?.Range((move.FromRow, move.FromCol), _chessboard.Board);

            if (validMoves != null)
            {
                HighlightValidMoves(validMoves.ToList());
                await Task.Delay(500); // Delay to show valid moves
                ClearHighlights();
            }
            try
            {
                _chessboard.MovePiece((move.FromRow, move.FromCol), (move.ToRow, move.ToCol));
                PlayMove();
                ClearHighlights();
                UpdateSquare(move.FromRow, move.FromCol);
                UpdateSquare(move.ToRow, move.ToCol);

            }
            catch (PawnException)
            {
                PlayMove();
                ClearHighlights();
                UpdateSquare(move.FromRow, move.FromCol);
                UpdateSquare(move.ToRow, move.ToCol);
                PromotePawn((move.ToRow, move.ToCol), move.Promotion);
                _chessboard.SwitchTurn();
            }
            catch (EnpassantException ex)
            {
                PlayMove();
                ClearHighlights();
                UpdateSquare(move.FromRow, move.FromCol);
                UpdateSquare(move.ToRow, move.ToCol);
                UpdateSquare(ex.Position.Row, ex.Position.Col);
            }
            catch (CastlingException ex)
            {
                PlayMove();
                ClearHighlights();
                foreach (var (r, c) in ex.UpdatedPositions)
                {
                    UpdateSquare(r, c); // Update the affected squares
                }
            }


        }
        private void PromotePawn((int Row, int Col) position, string? pieceName)
        {
            ChessPiece newPiece = pieceName switch
            {
                "Rook" => new Rook(_chessboard.CurrentTurn),
                "Bishop" => new Bishop(_chessboard.CurrentTurn),
                "Knight" => new Knight(_chessboard.CurrentTurn),
                _ => throw new InvalidOperationException("Invalid piece selected.")
            };

            _chessboard.Board[position.Row][position.Col] = newPiece;
            UpdateSquare(position.Row, position.Col); // Refresh the board after promotion
        }
        private void UpdateSquare(int row, int col)
        {
            // Get the target square at the given position
            var square = GetSquareAt(row , col );
            if (square == null) return;

            // Clear the square's content
            square.Child = null;

            // Get the chess piece at this position (if any)
            var piece = _chessboard.Board[row][col];
            if (piece != null)
            {
                // Create a Icon for the chess piece
                string piecePath = $"/ChessPieces/{piece.Color}{piece.Type}.xaml";
                var pieceIcon = Application.LoadComponent(new Uri(piecePath, UriKind.Relative)) as Viewbox;

                if (pieceIcon != null)
                {
                    pieceIcon.Stretch = Stretch.Uniform; // Ensure proper scaling
                    square.Child = pieceIcon; // Set the Viewbox as the child
                }

                // Set the piece text inside the square
                square.Child = pieceIcon;
            }
        }
        // Render the board state
        private void RenderBoard()
        {
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var piece = _chessboard.Board[row][col];
                    var square = _squareCache[row, col];
                    square.Child = null;

                    if (piece != null)
                    {
                        // Create a Icon for the chess piece
                        string piecePath = $"/ChessPieces/{piece.Color}{piece.Type}.xaml";
                        var pieceIcon = Application.LoadComponent(new Uri(piecePath, UriKind.Relative)) as Viewbox;

                        if (pieceIcon != null)
                        {
                            pieceIcon.Stretch = Stretch.Uniform; // Ensure proper scaling
                            square.Child = pieceIcon; // Set the Viewbox as the child
                        }

                        // Set the piece text inside the square
                        square.Child = pieceIcon;
                    }
                }
            }
        }

        // Get the square at a specific position
        private Border GetSquareAt(int row, int col)
        {
            return _squareCache[row, col];
        }
        // Highlight valid moves
        private void HighlightValidMoves(List<(int, int)> validMoves)
        {
            if (_userColor == _chessboard.CurrentTurn)
            {
                foreach (var (row, col) in validMoves)
                {
                    var square = GetSquareAt(row, col);
                    square.Background = Brushes.ForestGreen;
                }
            }
            else
            {
                foreach (var (row, col) in validMoves)
                {
                    var square = GetSquareAt(row, col);
                    square.Background = _redHighlight;
                }
            }
        }

        // Clear highlighted moves
        private void ClearHighlights()
        {
            foreach (var square in _squareCache)
            {
                int row = Grid.GetRow(square);
                int col = Grid.GetColumn(square);
                square.Background = (row + col) % 2 == 0 ? _lightSquareGradient : _darkSquareGradient;
            }
        }

        // Display the end game overlay
        private async Task DisplayEndGame()
        {
            var selectedGame = GameComboBox.SelectedItem as GameSummary;
            if (selectedGame == null) return;
            var gameDetails = await _gameRecordService.GetGameDetailsAsync(selectedGame.GameId);

            var overlayGrid = new Grid
            {
                Background = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var crownTextBlock = new TextBlock
            {
                Text = "\u265A", // Crown icon
                FontSize = 200,
                Foreground = Brushes.Gold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var winnerMessage = new TextBlock
            {
                Text = $"{gameDetails.Winner} Wins!",
                FontSize = 50,
                Foreground = Brushes.Gold,
                Margin = new Thickness(0, 20, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var winMethodMessage = new TextBlock
            {
                Text = gameDetails.WinMethod,
                FontSize = 30,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 80, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            overlayGrid.Children.Add(crownTextBlock);
            overlayGrid.Children.Add(winnerMessage);
            overlayGrid.Children.Add(winMethodMessage);

            ChessBoardGrid.Children.Add(overlayGrid);
            Grid.SetRowSpan(overlayGrid, 8);
            Grid.SetColumnSpan(overlayGrid, 4);
        }

        // Window control handlers
        private void MinimizeWindow_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void MaximizeWindow_Click(object sender, RoutedEventArgs e) =>
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void CloseWindow_Click(object sender, RoutedEventArgs e){
            if(_replayTimer!=null)
            _replayTimer.Stop();
            _movePlayer.Close();
            this.Close();
        } 
        private void Toolbar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        // Handle the Stop/Resume button click
        private void StopResumeReplay_Click(object sender, RoutedEventArgs e)
        {
            if (_replayTimer == null) return;

            if (_isPaused)
            {
                // Resume the replay
                _replayTimer.Start();
                StopResumeButton.Background = Brushes.Red;
                StopResumeButton.Content = "⏹"; // Stop icon
            }
            else
            {
                // Pause the replay
                _replayTimer.Stop();
                StopResumeButton.Background = Brushes.Blue;
                StopResumeButton.Content = "▶"; // Play icon
            }

            _isPaused = !_isPaused; // Toggle the paused state
        }


    }
}
