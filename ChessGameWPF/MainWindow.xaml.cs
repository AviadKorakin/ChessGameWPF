using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ChessGameWPF.Exceptions;
using ChessGameWPF.Models;
using Microsoft.Data.SqlClient;

namespace ChessGameWPF
{
    public partial class MainWindow : Window
    {
        private Chessboard _chessboard = default!;
        private PieceColor _userColor;
        private PieceColor _computerColor;
        private Border[,] _squareCache = default!; // 8 rows x 4 columns
                                        // Predefined brushes to reuse the wooden look.
                                        // Predefined gradient brushes for squares
                                        // Predefined gradient brushes for squares with more wood-like tones
                                        // Predefined gradient brushes for dark squares (deeper wood tones)
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

        private readonly Brush _redHighlight = new SolidColorBrush(Color.FromRgb(180, 30, 30)); // Dark and vibrant red

        private (int Row, int Col)? _selectedPiecePosition = null; // Store the position of the selected piece
        private bool _isDrawing = false; // To track drawing state
        private readonly Brush _drawColor = Brushes.Red; // Color to use for drawing
        private readonly MediaPlayer _movePlayer = new MediaPlayer();
        private readonly MediaPlayer _tickPlayer = new MediaPlayer();
        private DispatcherTimer? _gameTimer; // Timer for the game
        private int _turnTime = 20;
        private int _timeRemaining ; // Time in seconds (20 seconds limit)
        private bool _isGameStarted = false;  // Track if the game has started
        private bool _isGameEnded=false;
        private WriteableBitmap? _writeableBitmap; // Bitmap for drawing
        private Point _lastPoint; // Store the last point for line drawing 
        private GameRecordService _gameRecordService = new GameRecordService();  // Service to interact with the database
        private int _currentGameId;  // Store the current GameId
        private int _stepOrder = 0;  // Track the step order for saving moves
        private readonly Dictionary<(string PieceType, PieceColor Color), TextBlock> _pieceCache = new();
        private bool _apiTurn = false;
        private List<MoveStepRequest> _moveSteps; // List to hold game steps
        private readonly List<Task> _ongoingTasks = new List<Task>();
        private readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:8000/api/chess/")
        };

        private static readonly DoubleAnimation _sharedBounceAnimation = new DoubleAnimation
        {
            From = 0,
            To = -20,
            Duration = TimeSpan.FromMilliseconds(200),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(1)
        };

        private static readonly ColorAnimation _flashAnimation = new ColorAnimation
        {
            From = Colors.Red,
            To = Colors.Yellow,
            Duration = TimeSpan.FromMilliseconds(300),
            AutoReverse = true,
            RepeatBehavior = new RepeatBehavior(3)
        };
        public MainWindow()
        {
            InitializeComponent();
            DrawWelcomeScreen();
            _moveSteps = new List<MoveStepRequest>();
            // Handle KeyDown to toggle drawing mode
            this.KeyDown += MainWindow_KeyDown;

            // Handle mouse events for drawing
            DrawingImage.MouseLeftButtonDown += DrawingImage_MouseLeftButtonDown;
            DrawingImage.MouseMove += DrawingImage_MouseMove;
            DrawingImage.MouseLeftButtonUp += DrawingImage_MouseLeftButtonUp;
            //disable the top layers clicking event blocking the game moves.
            //canvas is used for background in the end and start of the game.
            //image is used to load the writablebitmap .
            DrawingCanvas.IsHitTestVisible = false;
            DrawingImage.IsHitTestVisible = false;

            PrepareMediaPlayers(); // Prepare sounds
            InitializeWriteableBitmap(); // Initialize drawing surface
        }

        private async Task GetMoveFromApi()
        {
            try
            {
                _apiTurn = true;
                /*HttpResponseMessage printResponse = await _httpClient.PostAsJsonAsync("print-board", _chessboard);
                 if (!printResponse.IsSuccessStatusCode)
                 {
                     ShowErrorMessage($"Print Board API Error: {printResponse.ReasonPhrase}", "API Error");
                 }*/
                // Send the chessboard state to the API
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("move", _chessboard);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response into a MoveResponse object
                    var moveResponse = await response.Content.ReadAsAsync<MoveResponse>();

                    if (moveResponse != null)
                    {
                        if (moveResponse.From == null || moveResponse.To == null ) return;
                        if (moveResponse.Promotion == null)
                        {
                            ApplyApiMove(moveResponse.From.Value, moveResponse.To.Value,null);
                        }
                        else
                        {
                            ApplyApiMove(moveResponse.From.Value, moveResponse.To.Value, moveResponse.Promotion);
                        }

                    }
                }
                else
                {
                    // Read the error response body for details
                    string errorContent = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"API Error: {response.ReasonPhrase}.\nDetails: {errorContent}";

                    // Show the error message and close the game
                    ShowWarningMessage(errorMessage, "API Error");
                    ShowErrorAndClose(
                        "API failed to calculate next move. Check the error details.",
                        "API Error");
                }
            }
            catch (Exception)
            {
                ShowErrorAndClose(
                               "Lost connection to the Server. The game will now close.",
                               "API Error"
                           );
            }

        }

        private async void ApplyApiMove((int Row, int Col) from, (int Row, int Col) to,string? promotion)
        {
            try
            {

                await Task.Delay(200);
                // Get the piece at the selected position
                var selectedPiece = _chessboard.Board[from.Row][from.Col];

                if (selectedPiece == null)
                    throw new InvalidOperationException("No piece to move.");

                // Get valid moves for the piece
                var validMoves = selectedPiece.Range(from, _chessboard.Board);

                // Apply bounce animation to the selected piece
                UIElement? pieceIcon = GetPieceElementAt(from.Row + 1, from.Col + 1);
                if (pieceIcon == null) return;

                BounceAnimation(pieceIcon);
                HighlightValidMoves(validMoves);

                // Delay to allow the bounce animation to complete before highlighting
                await Task.Delay(500);

                // Highlight valid moves

                // Attempt to move the piece
                if (_chessboard.MovePiece(from, to))
                {

                    ResetGameTimer();
                    // Play move sound
                    PlayMove();

                    // Save the move to the database
                    _stepOrder++;
                    string? name = _chessboard.Board?[to.Row][to.Col]?.Type;
                    if (name != null)
                    {
                        var moveStep = new MoveStepRequest
                        {
                            GameId = _currentGameId,
                            From = from,
                            To = to,
                            StepOrder = _stepOrder,
                            PieceType = name,
                            Promotion = promotion,
                            Castling = false,
                            EnPassant = false


                        };
                        //_moveSteps.Add(moveStep); // Add to the list
                        //_gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, name, null);
                        var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                        var taskClient = Task.Run(() =>
                        {
                            try
                            {
                                // Save the step in the background
                                _gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, name, null,false,false);
                            }
                            catch (SqlException)
                            {
                                // Handle SQL-specific connection errors
                                ShowErrorAndClose(
                                    "Lost connection to the local database. The game will now close.",
                                    "Database Connection Error"
                                );
                            }
                            catch (Exception ex)
                            {
                                // Handle all other exceptions
                                ShowErrorAndClose(
                                    $"Error saving step: {ex.Message}. The game will now close.",
                                    "Save Error"
                                );
                            }
                        });
                        _ongoingTasks.Add(taskServer); // Add to task list
                        _ongoingTasks.Add(taskClient); // Add to task list

                    }

                    // Perform the fade-out and move-up animation after the move
                    FadeOutAndMoveUpAnimation(pieceIcon, () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ClearHighlights(); // Clear highlights after the animation completes
                            UpdateSquare(from.Row, from.Col); // Update original square
                            UpdateSquare(to.Row, to.Col); // Update destination square
                            _selectedPiecePosition = null; // Reset selected position
                            _apiTurn = false;
                        });
                    });
                }
            }
            catch (TieException ex)
            {
                EndGame(3, ex.Message);
            }
            catch (CastlingException ex)
            {
                ResetGameTimer();
                UIElement? pieceIcon = GetPieceElementAt(from.Row + 1, from.Col + 1);
                if (pieceIcon == null) return;
                // Play move sound
                PlayMove();

                // Save the move to the database
                _stepOrder++;
                var moveStep = new MoveStepRequest
                {
                    GameId = _currentGameId,
                    From = from,
                    To = to,
                    StepOrder = _stepOrder,
                    PieceType = "King",
                    Promotion = null,
                    Castling =true,
                    EnPassant = false
                };
                //_moveSteps.Add(moveStep); // Add to the list
                //_gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, name, null);
                var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                var taskClient = Task.Run(() =>
                {
                    try
                    {
                        // Save the step in the background
                        _gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, "King", null, false, true);
                    }
                    catch (SqlException)
                    {
                        // Handle SQL-specific connection errors
                        ShowErrorAndClose(
                            "Lost connection to the local database. The game will now close.",
                            "Database Connection Error"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Handle all other exceptions
                        ShowErrorAndClose(
                            $"Error saving step: {ex.Message}. The game will now close.",
                            "Save Error"
                        );
                    }
                });
                _ongoingTasks.Add(taskServer); // Add to task list
                _ongoingTasks.Add(taskClient); // Add to task list




                // Perform the fade-out and move-up animation after the move
                FadeOutAndMoveUpAnimation(pieceIcon, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ClearHighlights(); // Clear highlights after the animation completes
                        foreach (var (r, c) in ex.UpdatedPositions)
                        {
                            UpdateSquare(r, c); // Update the affected squares
                        }
                        _selectedPiecePosition = null; // Reset selected position
                        _apiTurn = false;

                    });
                });
            }

            catch (EnpassantException ex)
            {
                ResetGameTimer();
                UIElement? pieceIcon = GetPieceElementAt(from.Row + 1, from.Col + 1);
                if (pieceIcon == null) return;
                // Play move sound
                PlayMove();

                // Save the move to the database
                _stepOrder++;
                string? name = _chessboard.Board?[to.Row][to.Col]?.Type;
                if (name != null)
                {
                    var moveStep = new MoveStepRequest
                    {
                        GameId = _currentGameId,
                        From = from,
                        To = to,
                        StepOrder = _stepOrder,
                        PieceType = name,
                        Promotion = null,
                        Castling = false,
                        EnPassant=true
   
                    };
                    //_moveSteps.Add(moveStep); // Add to the list
                    //_gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, name, null);
                    var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                    var taskClient = Task.Run(() =>
                    {
                        try
                        {
                            // Save the step in the background
                            _gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, name, null, true, false);
                        }
                        catch (SqlException)
                        {
                            // Handle SQL-specific connection errors
                            ShowErrorAndClose(
                                "Lost connection to the local database. The game will now close.",
                                "Database Connection Error"
                            );
                        }
                        catch (Exception ex)
                        {
                            // Handle all other exceptions
                            ShowErrorAndClose(
                                $"Error saving step: {ex.Message}. The game will now close.",
                                "Save Error"
                            );
                        }
                    });
                    _ongoingTasks.Add(taskServer); // Add to task list
                    _ongoingTasks.Add(taskClient); // Add to task list
                }

               

                // Perform the fade-out and move-up animation after the move
                FadeOutAndMoveUpAnimation(pieceIcon, () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ClearHighlights(); // Clear highlights after the animation completes
                        UpdateSquare(from.Row, from.Col); // Update original square
                        UpdateSquare(to.Row, to.Col); // Update destination square
                        UpdateSquare(ex.Position.Row, ex.Position.Col);
                        _selectedPiecePosition = null; // Reset selected position
                        _apiTurn = false;
                        
                    });
                });
            }
            catch (PawnException)
            {
                ResetGameTimer();
                _stepOrder++;
               // _gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, "Pawn", promotion);
                var moveStep = new MoveStepRequest
                {
                    GameId = _currentGameId,
                    From = from,
                    To = to,
                    StepOrder = _stepOrder,
                    PieceType = "Pawn",
                    Promotion = promotion,
                    Castling = false,
                    EnPassant = false
                };
                var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                var taskClient = Task.Run(() =>
                {
                    try
                    {
                        // Save the step in the background
                        _gameRecordService.SaveStep(_currentGameId, from, to, _stepOrder, "Pawn", promotion, false, false);
                    }
                    catch (SqlException)
                    {
                        // Handle SQL-specific connection errors
                        ShowErrorAndClose(
                            "Lost connection to the local database. The game will now close.",
                            "Database Connection Error"
                        );
                    }
                    catch (Exception ex)
                    {
                        // Handle all other exceptions
                        ShowErrorAndClose(
                            $"Error saving step: {ex.Message}. The game will now close.",
                            "Save Error"
                        );
                    }
                });
                _ongoingTasks.Add(taskServer); // Add to task list
                _ongoingTasks.Add(taskClient); // Add to task list
                PromotePawn(to, promotion);
            }
            catch (CheckmateException ex)
            {
                EndGame(1, ex.Message);
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error applying move: {ex.Message}", "Game Error");
            }
        }

        private void ShowErrorAndClose(string message, string title)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBoxResult result = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                if (result == MessageBoxResult.OK)
                {
                    CloseWindow(this,new RoutedEventArgs()); // Gracefully close the window
                }
            });
        }
        private void InitializeWriteableBitmap()
        {
            // Get dimensions from the ChessBoardGrid or DrawingCanvas
            int width = (int)DrawingCanvas.ActualWidth;
            int height = (int)DrawingCanvas.ActualHeight;

            if (width <= 0 || height <= 0) return;

            // Create the WriteableBitmap
            _writeableBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            // Set the bitmap as the source of the Image control
            DrawingImage.Source = _writeableBitmap;
        }

        // Toggle drawing mode when 'D' is pressed
        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.D)
            {
                _isDrawing = !_isDrawing;
                DrawingImage.IsHitTestVisible = _isDrawing;

                if (_isDrawing)
                {
                    Cursor = Cursors.Cross; // Change to drawing cursor
                }
                else
                {
                    ClearDrawing(); // Clear the drawing when exiting draw mode
                    Cursor = Cursors.Arrow; // Reset cursor
                }
            }
        }
       
        // Handle mouse down to start drawing
        private void DrawingImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                _lastPoint = e.GetPosition(DrawingImage);
                DrawingImage.CaptureMouse();
            }
        }

        // Handle mouse move to draw continuously
        private void DrawingImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDrawing && e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPoint = e.GetPosition(DrawingImage);
                DrawLine(_lastPoint, currentPoint, Colors.Red,10); // Thicker and rounder line
                _lastPoint = currentPoint;
            }
        }

        // Handle mouse release to stop drawing
        private void DrawingImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDrawing)
            {
                DrawingImage.ReleaseMouseCapture();
            }
        }

        // Draw a line with a round brush
        private void DrawLine(Point start, Point end, Color color, int thickness)
        {
            _writeableBitmap?.Lock();

            try
            {
                int x1 = (int)start.X;
                int y1 = (int)start.Y;
                int x2 = (int)end.X;
                int y2 = (int)end.Y;

                int dx = Math.Abs(x2 - x1), sx = x1 < x2 ? 1 : -1;
                int dy = Math.Abs(y2 - y1), sy = y1 < y2 ? 1 : -1;
                int err = dx - dy, e2;

                // Ensure no gaps by using filled rectangles for thickness
                while (true)
                {
                    DrawFilledCircle(x1, y1, thickness / 2, color); // Smoother brush

                    if (x1 == x2 && y1 == y2) break;

                    e2 = 2 * err;
                    if (e2 > -dy) { err -= dy; x1 += sx; }
                    if (e2 < dx) { err += dx; y1 += sy; }
                }

                _writeableBitmap?.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight));
            }
            finally
            {
                _writeableBitmap?.Unlock();
            }
        }

        // Draw a filled circle to ensure thick lines without gaps
        private void DrawFilledCircle(int centerX, int centerY, int radius, Color color)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int x = -radius; x <= radius; x++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        SetPixel(centerX + x, centerY + y, color);
                    }
                }
            }
        }

        // Set pixel on the WriteableBitmap safely
        private void SetPixel(int x, int y, Color color)
        {
            if (_writeableBitmap == null || x < 0 || x >= _writeableBitmap.PixelWidth || y < 0 || y >= _writeableBitmap.PixelHeight)
                return;

            int pixelOffset = (y * _writeableBitmap.BackBufferStride) + (x * 4);

            unsafe
            {
                byte* pBackBuffer = (byte*)_writeableBitmap.BackBuffer + pixelOffset;
                pBackBuffer[0] = color.B;
                pBackBuffer[1] = color.G;
                pBackBuffer[2] = color.R;
                pBackBuffer[3] = 255; // Alpha channel
            }
        }


        // Clear the drawing by resetting the WriteableBitmap
        private void ClearDrawing()
        {
            _writeableBitmap?.Lock();
            try
            {
                unsafe
                {
                    if (_writeableBitmap == null) return;
                    // Clear the bitmap by setting all pixels to transparent
                    IntPtr pBackBuffer = _writeableBitmap.BackBuffer;
                    for (int i = 0; i < _writeableBitmap.PixelHeight; i++)
                    {
                        int rowOffset = i * _writeableBitmap.BackBufferStride;
                        for (int j = 0; j < _writeableBitmap.PixelWidth * 4; j += 4)
                        {
                            byte* pixel = (byte*)(pBackBuffer + rowOffset + j);
                            pixel[0] = 0; // Blue
                            pixel[1] = 0; // Green
                            pixel[2] = 0; // Red
                            pixel[3] = 0; // Alpha (fully transparent)
                        }
                    }
                }
                _writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, _writeableBitmap.PixelWidth, _writeableBitmap.PixelHeight));
            }
            finally
            {
                _writeableBitmap?.Unlock();
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            if (!_isGameStarted)
            {
                DrawWelcomeScreen(); // Redraw on window resize
            }
            InitializeWriteableBitmap();

        }
        private void DrawWelcomeScreen()
        {
            // Clear previous drawings
            DrawingCanvas.Children.Clear();

            // Load the chess icon image
            Image chessIcon = new Image
            {
                Source = new BitmapImage(new Uri("/Images/chess.png", UriKind.Relative)),
                Width = 150,  // Adjust size as needed
                Height = 150,
                Stretch = Stretch.Uniform
            };

            // Calculate the center position for the icon
            double iconX = (DrawingCanvas.ActualWidth / 2) - (chessIcon.Width + 10); // Adjust for spacing
            double iconY = (DrawingCanvas.ActualHeight / 2) - (chessIcon.Height / 2);

            // Set icon position
            Canvas.SetLeft(chessIcon, iconX);
            Canvas.SetTop(chessIcon, iconY);
            DrawingCanvas.Children.Add(chessIcon);
            // Create a TextBlock for the LiteChess text
            TextBlock liteChessText = new TextBlock
            {
                Text = "LiteChess",
                FontSize = 50,
                Foreground = Brushes.Gold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Measure the text size
            liteChessText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size textSize = liteChessText.DesiredSize;

            // Calculate the center position for the text (aligned to the right of the icon)
            double textX = iconX + chessIcon.Width + 20; // Spacing between icon and text
            double textY = (DrawingCanvas.ActualHeight / 2) - (textSize.Height / 2);

            // Set text position
            Canvas.SetLeft(liteChessText, textX);
            Canvas.SetTop(liteChessText, textY);
            DrawingCanvas.Children.Add(liteChessText);
        }

        private void InitializeGameTimer()
        {
            _timeRemaining = _turnTime;
           _gameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1) // Tick every second
            };

            // Timer event handler for each tick
            _gameTimer.Tick += (s, e) =>
            {
                _timeRemaining--;

                // Calculate minutes and seconds from the remaining time
                int minutes = _timeRemaining / 60;
                int seconds = _timeRemaining % 60;

                // Update the timer display with mm:ss format
                GameTimer.Text = $"Time Left: {minutes:D2}:{seconds:D2}";

                // Play tick sound on every tick
                PlayTick();

                // Check if time is up
                if (_timeRemaining <= 0)
                {
                    _gameTimer.Stop(); // Stop the time
                    EndGame(0,null);
                }
            };

            _gameTimer.Start(); // Start the timer
        }

        private async void EndGame(int winType, string? message)
        {
            ClearHighlights();
            _selectedPiecePosition = null;
            _gameTimer?.Stop();
            _isGameEnded = true;

            PieceColor? winner = null;
            string? winMethod;

            switch (winType)
            {
                case 0: // Out of time

                    winner = _chessboard.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    winMethod = $"Out of time! {winner} wins.";
                    // Update the chessboard with winner and win method
                    _chessboard.Winner = winner;
                    _chessboard.WinMethod = winMethod;

                    break;
                case 1: // Checkmate
                    winner = _chessboard.Winner ?? (_chessboard.CurrentTurn == PieceColor.White ? PieceColor.Black : PieceColor.White);
                    winMethod = message ?? "Victory!";
                    break;
                case 2: // Tie/Stalemate
                    winMethod = message;
                    break;
                default:
                    winMethod = message;
                    break;
            }
            // Update the chessboard with winner and win method
            _chessboard.Winner = winner;
            _chessboard.WinMethod = winMethod;


            if (winMethod != null)
            {
                // Update the winner and win method in the database
                _gameRecordService.UpdateGameWinner(
                    _currentGameId,
                    winner?.ToString() ?? "Tie",  // Fallback to "Unknown"
                    winMethod
                );
                if (_ongoingTasks.Count > 0)
                {
                    try
                    {
                        // Await completion of all ongoing tasks
                        await Task.WhenAll(_ongoingTasks);
                    }
                    catch (AggregateException aggregateEx)
                    {
                        bool connectionLost = false;

                        // Handle each exception individually
                        foreach (var ex in aggregateEx.InnerExceptions)
                        {
                            if (ex is HttpRequestException)
                            {
                                connectionLost = true;
                            }
                            else
                            {
                                // Handle other exceptions gracefully
                                ShowErrorMessage($"Error while closing: {ex.Message}", "Error");
                            }
                        }

                        // If any task threw HttpRequestException, show the connection lost message
                        if (connectionLost)
                        {
                            ShowConnectionLostMessage();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch any other unexpected exceptions
                        ShowErrorMessage($"Unexpected error while closing: {ex.Message}", "Error");
                    }
                    finally
                    {
                  
                        _ongoingTasks.Clear();
               
                    }
                }
                await UpdateWinnerOnServer(winner, winMethod);
            }

            DisplayCrown(winner, winMethod);
        }

        private async Task UpdateWinnerOnServer(PieceColor? winner, string winMethod)
        {
            try
            {
                // Prepare the request to send winner information
                var winnerRequest = new UpdateWinnerRequest
                {
                    GameId = _currentGameId,
                    Winner = winner?.ToString() ?? "Tie", // Assign "Tie" if winner is null
                    WinMethod = winMethod
                };

                // Send the request to update the winner on the server
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("update-winner", winnerRequest);

                if (!response.IsSuccessStatusCode)
                {
                    ShowErrorMessage($"Failed to update winner on server: {response.ReasonPhrase}", "Server Error");
                }
            }
            catch (HttpRequestException)
            {
                return;
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"An error occurred while updating the winner: {ex.Message}", "Error");
            }
        }

        private async Task SaveGameStepsToServer()
        {
            try
            {
                // Send the move records to the server
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("store-steps", _moveSteps);

                if (!response.IsSuccessStatusCode)
                {
                    ShowErrorMessage($"Failed to save game steps: {response.ReasonPhrase}", "Server Error");
                }
            }
            catch (HttpRequestException httpEx)
            {
                ShowErrorMessage($"Network error while saving steps: {httpEx.Message}.", "Network Error");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"An error occurred while saving steps: {ex.Message}", "Error");
            }
        }
        private void PrepareMediaPlayers()
        {
            try
            {
                // Prepare move.mp3
                var moveUri = new Uri("Sounds/move.mp3", UriKind.Relative);
                _movePlayer.Open(moveUri);

                _movePlayer.MediaFailed += (s, e) =>
                {
                    ShowErrorMessage($"Error playing 'move' sound: {e.ErrorException.Message}", "Sound Playback Error");
                };

                // Prepare tick.mp3
                var tickUri = new Uri("Sounds/tick.mp3", UriKind.Relative);
                _tickPlayer.Open(tickUri);

                _tickPlayer.MediaFailed += (s, e) =>
                {
                    ShowErrorMessage($"Error playing 'tick' sound: {e.ErrorException.Message}", "Sound Playback Error");
                };
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error preparing sounds: {ex.Message}", "Sound Preparation Error");
            }
        }
        private void InitializeLabels()
        {
            // Top Column Labels
            AddLabel(0, 1, "a");
            AddLabel(0, 2, "b");
            AddLabel(0, 3, "c");
            AddLabel(0, 4, "d");

            // Bottom Column Labels
            AddLabel(9, 1, "a");
            AddLabel(9, 2, "b");
            AddLabel(9, 3, "c");
            AddLabel(9, 4, "d");

            // Left Row Labels
            for (int i = 1; i <= 8; i++)
            {
                AddLabel(i, 0, (9 - i).ToString());
            }

            // Right Row Labels
            for (int i = 1; i <= 8; i++)
            {
                AddLabel(i, 5, (9 - i).ToString());
            }
        }

        private void AddLabel(int row, int col, string text)
        {
            TextBlock label = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 16,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(label, row);
            Grid.SetColumn(label, col);
            ChessBoardGrid.Children.Add(label);
        }

        private void RenderBoard()
        {
            // Clear previous squares in the grid (leaving labels intact).
            for (int i = ChessBoardGrid.Children.Count - 1; i >= 0; i--)
            {
                if (Grid.GetRow(ChessBoardGrid.Children[i]) > 0 && Grid.GetRow(ChessBoardGrid.Children[i]) < 9 &&
                    Grid.GetColumn(ChessBoardGrid.Children[i]) > 0 && Grid.GetColumn(ChessBoardGrid.Children[i]) < 5)
                {
                    ChessBoardGrid.Children.RemoveAt(i);
                }
            }

            // Populate the board with squares and cache them.
            for (int row = 1; row <= 8; row++)
            {
                for (int col = 1; col <= 4; col++)
                {
                    // Create the border (square) for each cell.
                    Border square = new Border
                    {
                        Background = (row + col) % 2 == 0 ? _lightSquareGradient : _darkSquareGradient,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(0.3)
                    };
                    // Get the chess piece at this position (if any).
                    ChessPiece? piece = _chessboard.Board[row - 1][ col - 1];

                    if (piece != null)
                    {

                        // Load the XAML resource for the piece
                        string piecePath = $"/ChessPieces/{piece.Color}{piece.Type}.xaml";
                        var pieceIcon = Application.LoadComponent(new Uri(piecePath, UriKind.Relative)) as Viewbox;

                        if (pieceIcon != null)
                        {
                            pieceIcon.Stretch = Stretch.Uniform; // Ensure proper scaling
                            square.Child = pieceIcon; // Set the Viewbox as the child
                        }
                    }
                    // Attach the click event handler.
                    square.MouseLeftButtonDown += Square_Clicked;

                    // Cache the square for fast access.
                    _squareCache[row - 1, col - 1] = square;

                    // Add the square to the grid.
                    Grid.SetRow(square, row);
                    Grid.SetColumn(square, col);
                    ChessBoardGrid.Children.Add(square);
                }
            }
        }


        private async void Square_Clicked(object sender, MouseButtonEventArgs e)
        {
            if (_apiTurn == true) return;
            // Get the clicked square
            Border? clickedSquare = sender as Border;
            int row = Grid.GetRow(clickedSquare);
            int col = Grid.GetColumn(clickedSquare);
            if (clickedSquare == null) return;

            ChessPiece? piece = _chessboard.Board[row - 1][col - 1]; // Adjust for zero-based indexing

            if (piece != null && piece.Color == _userColor)
            {
                // If a piece is clicked
                if (_selectedPiecePosition == null)
                {
                    // First click - select the piece
                    _selectedPiecePosition = (row - 1, col - 1); // Store selected position

                    // Get valid moves
                    var validMoves = piece.Range((row - 1, col - 1), _chessboard.Board);
                    HighlightValidMoves(validMoves);

                    // Bounce effect for the clicked piece
                    UIElement? pieceIcon = GetPieceElementAt(_selectedPiecePosition.Value.Row + 1, _selectedPiecePosition.Value.Col + 1);
                    if (pieceIcon == null) return;
                    BounceAnimation(pieceIcon);
                }
                else
                {
                    ChessPiece? selectedPiece = _chessboard.Board[_selectedPiecePosition.Value.Row][_selectedPiecePosition.Value.Col];
                    if (selectedPiece is King && piece is Rook && piece.Color == selectedPiece.Color ||
                        selectedPiece is Rook && piece is King && piece.Color == selectedPiece.Color)
                    {
                        // **Castling attempt**
                        try
                        {
                            _chessboard.MovePiece(_selectedPiecePosition.Value, (row - 1, col - 1)); // Attempt the castling move
                        }
                        catch (CastlingException ex)
                        {
                            if (_selectedPiecePosition == null) return;
                            // Reset the game timer after a valid move
                            ResetGameTimer();
                            // Play move sound
                            PlayMove();

                            // Save the move to the database
                            _stepOrder++;

                            var moveStep = new MoveStepRequest
                            {
                                GameId = _currentGameId,
                                From = _selectedPiecePosition.Value,
                                To = (row - 1, col - 1),
                                StepOrder = _stepOrder,
                                PieceType = "King",
                                Promotion = null,
                                Castling = true,
                                EnPassant = false
                            };
                            //_moveSteps.Add(moveStep); // Add to the list
                            //_gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, name, null);
                            var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                            var taskClient = Task.Run(() =>
                            {
                                try
                                {
                                    // Save the step in the background
                                    _gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, "King", null,false,true);
                                }
                                catch (SqlException)
                                {
                                    // Handle SQL-specific connection errors
                                    ShowErrorAndClose(
                                        "Lost connection to the local database. The game will now close.",
                                        "Database Connection Error"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    // Handle all other exceptions
                                    ShowErrorAndClose(
                                        $"Error saving step: {ex.Message}. The game will now close.",
                                        "Save Error"
                                    );
                                }
                            });
                            _ongoingTasks.Add(taskServer); // Add to task list
                            _ongoingTasks.Add(taskClient); // Add to task list

                            // Perform the fade-out and move-up animation
                            UIElement? pieceIcon = GetPieceElementAt(_selectedPiecePosition.Value.Row + 1, _selectedPiecePosition.Value.Col + 1);
                            if (pieceIcon == null) return;

                            // Perform the fade-out and move-up animation after the move
                            FadeOutAndMoveUpAnimation(pieceIcon, () =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ClearHighlights(); // Clear highlights after the animation completes
                                    foreach (var (r, c) in ex.UpdatedPositions)
                                    {
                                        UpdateSquare(r, c); // Update the affected squares
                                    }
                                    _selectedPiecePosition = null; // Reset selected position
                                    _apiTurn = false;
                                    ResetGameTimer();

                                });
                            });
                            await GetMoveFromApi();
                        }
                    }
                    // Second click - deselect the piece
                    ClearHighlights();
                    _selectedPiecePosition = null; // Reset selected position
                }
            }
            else
            {
                // Clicked an empty square
                if (_selectedPiecePosition != null)
                {
                    // If a piece was selected, move it to the empty square
                    var selectedPiece = _chessboard.Board[_selectedPiecePosition.Value.Row][ _selectedPiecePosition.Value.Col];

                    try
                    {
                        // Attempt to move the piece in the chessboard logic
                        if (_chessboard.MovePiece(_selectedPiecePosition.Value, (row - 1, col - 1)))
                        {
                            // Reset the game timer after a valid move
                            ResetGameTimer();
                            // Play move sound
                            PlayMove();

                            // Save the move to the database
                            _stepOrder++;
                            string? name = _chessboard.Board?[row - 1][col - 1]?.Type;
                            if (name != null)
                            {
                                var moveStep = new MoveStepRequest
                                {
                                    GameId = _currentGameId,
                                    From = _selectedPiecePosition.Value,
                                    To = (row - 1, col - 1),
                                    StepOrder = _stepOrder,
                                    PieceType = name,
                                    Promotion = null,
                                    Castling = false,
                                    EnPassant = false
                                };
                                //_moveSteps.Add(moveStep); // Add to the list
                                //_gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, name, null);
                                var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                                var taskClient = Task.Run(() =>
                                {
                                    try
                                    {
                                        // Save the step in the background
                                        _gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, name, null, false, false);
                                    }
                                    catch (SqlException)
                                    {
                                        // Handle SQL-specific connection errors
                                        ShowErrorAndClose(
                                            "Lost connection to the local database. The game will now close.",
                                            "Database Connection Error"
                                        );
                                    }
                                    catch (Exception ex)
                                    {
                                        // Handle all other exceptions
                                        ShowErrorAndClose(
                                            $"Error saving step: {ex.Message}. The game will now close.",
                                            "Save Error"
                                        );
                                    }
                                });
                                _ongoingTasks.Add(taskServer); // Add to task list
                                _ongoingTasks.Add(taskClient); // Add to task list

                            }

                            // Perform the fade-out and move-up animation
                            UIElement? pieceIcon = GetPieceElementAt(_selectedPiecePosition.Value.Row + 1, _selectedPiecePosition.Value.Col + 1);
                            if (pieceIcon == null) return;
                            FadeOutAndMoveUpAnimation(pieceIcon, () =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    ClearHighlights(); // Clear highlights if the move is successful
                                    UpdateSquare(_selectedPiecePosition.Value.Row, _selectedPiecePosition.Value.Col); // Original square
                                    UpdateSquare(row - 1, col - 1); // Destination square
                                    _selectedPiecePosition = null; // Reset selected position
                                });
                            });

                            // **Call API for White's Move (After Player's Move Completes)**
                           if (_chessboard.CurrentTurn == _computerColor)
                            {
                                await GetMoveFromApi();
                                
                            }
                        }
                    }
                    catch (GameOverException ex)
                    {
                        ShowWarningMessage(ex.Message, "Invalid Move");
                        ClearHighlights(); // Clear highlights on game over
                        _selectedPiecePosition = null; // Reset selected position
                    }
                    catch (CheckmateException ex)
                    {
                        EndGame(1, ex.Message);
                    }
                    catch (EnpassantException ex)
                    {
                        if (_selectedPiecePosition == null) return;
                        // Reset the game timer after a valid move
                        ResetGameTimer();
                        // Play move sound
                        PlayMove();

                        // Save the move to the database
                        _stepOrder++;
                        string? name = _chessboard.Board?[row - 1][col - 1]?.Type;
                        if (name != null)
                        {
                            var moveStep = new MoveStepRequest
                            {
                                GameId = _currentGameId,
                                From = _selectedPiecePosition.Value,
                                To = (row - 1, col - 1),
                                StepOrder = _stepOrder,
                                PieceType = name,
                                Promotion = null,
                                Castling = false,
                                EnPassant = true
                            };
                            //_moveSteps.Add(moveStep); // Add to the list
                            //_gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, name, null);
                            var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                            var taskClient = Task.Run(() =>
                            {
                                try
                                {
                                    // Save the step in the background
                                    _gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, name, null, true, false);
                                }
                                catch (SqlException)
                                {
                                    // Handle SQL-specific connection errors
                                    ShowErrorAndClose(
                                        "Lost connection to the local database. The game will now close.",
                                        "Database Connection Error"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    // Handle all other exceptions
                                    ShowErrorAndClose(
                                        $"Error saving step: {ex.Message}. The game will now close.",
                                        "Save Error"
                                    );
                                }
                            });
                            _ongoingTasks.Add(taskServer); // Add to task list
                            _ongoingTasks.Add(taskClient); // Add to task list

                            }

                        // Perform the fade-out and move-up animation
                        UIElement? pieceIcon = GetPieceElementAt(_selectedPiecePosition.Value.Row + 1, _selectedPiecePosition.Value.Col + 1);
                        if (pieceIcon == null) return;
                        FadeOutAndMoveUpAnimation(pieceIcon, () =>
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ClearHighlights(); // Clear highlights if the move is successful
                                UpdateSquare(_selectedPiecePosition.Value.Row, _selectedPiecePosition.Value.Col); // Original square
                                UpdateSquare(row - 1, col - 1); // Destination square
                                UpdateSquare(ex.Position.Row,ex.Position.Col);
                                _selectedPiecePosition = null; // Reset selected position
                                // Reset the game timer after a valid move
                                ResetGameTimer();
                               
                            });

                        });
                        await GetMoveFromApi();
                    }
                    catch (TieException ex)
                    {
                        EndGame(2, ex.Message);
                    }
                    catch (CheckException ex)
                    {
                        ShowErrorMessage(ex.Message, "Invalid Move");
                        ClearHighlights(); // Clear highlights on invalid move
                        _selectedPiecePosition = null; // Reset selected position
                    }
                    catch (IlegalMoveException)
                    {
                        ClearHighlights(); // Clear highlights on invalid move
                        AnimateKingAndFlashCells();  // Animate the king and flash the cells
                        _selectedPiecePosition = null; // Reset selected position
                    }
                    catch (PawnException)
                    {
                        // Open the promotion dialog
                        var promotionDialog = new PromotionDialog();

                        if (promotionDialog.ShowDialog() == true)
                        {
                            if (!_isGameEnded)
                            {
                                if (_selectedPiecePosition == null) return;
                                ResetGameTimer();

                                UpdateSquare(_selectedPiecePosition.Value.Row, _selectedPiecePosition.Value.Col);
                                PromotePawn((row - 1, col - 1), promotionDialog.SelectedPiece);
                                _stepOrder++;
                              
                                var moveStep = new MoveStepRequest
                                {
                                    GameId = _currentGameId,
                                    From = _selectedPiecePosition.Value,
                                    To = (row - 1, col - 1),
                                    StepOrder = _stepOrder,
                                    PieceType = "Pawn",
                                    Promotion = promotionDialog.SelectedPiece,
                                    Castling = false,
                                    EnPassant = false

                                };
                                //_moveSteps.Add(moveStep); // Add to the list
                               // _gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, "Pawn", promotionDialog.SelectedPiece);
                                var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                                var taskClient = Task.Run(() =>
                                {
                                    try
                                    {

                                        // Save the step in the background
                                        _gameRecordService.SaveStep(
                                            moveStep.GameId,
                                            moveStep.From,
                                            moveStep.To,
                                            moveStep.StepOrder,
                                            moveStep.PieceType,
                                            moveStep.Promotion,
                                            false,
                                            false
                                            );
                                    }
                                    catch (SqlException)
                                    {
                                        // Handle SQL-specific connection errors
                                        ShowErrorAndClose(
                                            "Lost connection to the local database. The game will now close.",
                                            "Database Connection Error"
                                        );
                                    }
                                    catch (Exception ex)
                                    {
                                        // Handle all other exceptions
                                        ShowErrorAndClose(
                                            $"Error saving step: {ex.Message}. The game will now close.",
                                            "Save Error"
                                        );
                                    }
                                });
                                _ongoingTasks.Add(taskServer); // Add to task list
                                _ongoingTasks.Add(taskClient); // Add to task list




                                ClearHighlights();
                                _selectedPiecePosition = null; // Reset selected position
                                _chessboard.SwitchTurn();
                                await GetMoveFromApi();
                                return;
                            }
                        }

                        if (!_isGameEnded)
                        {
                            if (_selectedPiecePosition == null) return;
                            ResetGameTimer();
                            PromotePawn((row - 1, col - 1), promotionDialog.SelectedPiece);
                            _stepOrder++;
                    
                            var moveStep = new MoveStepRequest
                            {
                                GameId = _currentGameId,
                                From = _selectedPiecePosition.Value,
                                To = (row - 1, col - 1),
                                StepOrder = _stepOrder,
                                PieceType = "Pawn",
                                Promotion = promotionDialog.SelectedPiece,
                                Castling = false,
                                EnPassant = false
                            };
                            //_moveSteps.Add(moveStep); // Add to the list
                           // _gameRecordService.SaveStep(_currentGameId, _selectedPiecePosition.Value, (row - 1, col - 1), _stepOrder, "Pawn", promotionDialog.SelectedPiece);
                            var taskServer = Task.Run(async () => await SaveStepToServerAsync(moveStep));
                            var taskClient = Task.Run(() =>
                            {
                                try
                                {
                                    // Save the step in the background
                                    _gameRecordService.SaveStep(
                                    moveStep.GameId,
                                    moveStep.From,
                                    moveStep.To,
                                    moveStep.StepOrder,
                                    moveStep.PieceType,
                                    moveStep.Promotion
                                    ,false
                                    ,false
                                        );
                                }
                                catch (SqlException)
                                {
                                    // Handle SQL-specific connection errors
                                    ShowErrorAndClose(
                                        "Lost connection to the local database. The game will now close.",
                                        "Database Connection Error"
                                    );
                                }
                                catch (Exception ex)
                                {
                                    // Handle all other exceptions
                                    ShowErrorAndClose(
                                        $"Error saving step: {ex.Message}. The game will now close.",
                                        "Save Error"
                                    );
                                }
                            });
                            _ongoingTasks.Add(taskServer); // Add to task list
                            _ongoingTasks.Add(taskClient); // Add to task list

                            _selectedPiecePosition = null; // Reset selected position
                            _chessboard.SwitchTurn();
                            ClearHighlights();
                            await GetMoveFromApi();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        ShowErrorMessage(ex.Message, "Game Error");
                        ClearHighlights(); // Clear highlights on error
                        _selectedPiecePosition = null; // Reset selected position
                    }
                }
            }
        }

        private void UpdateSquare(int row, int col)
        {
            var square = GetSquareAt(row + 1, col + 1);
            if (square == null) return;

            square.Child = null; // Clear existing content

            var piece = _chessboard.Board[row][col];
            if (piece != null)
            {
                // Load the XAML resource for the piece
                string piecePath = $"/ChessPieces/{piece.Color}{piece.Type}.xaml";
                var pieceIcon = Application.LoadComponent(new Uri(piecePath, UriKind.Relative)) as Viewbox;

                if (pieceIcon != null)
                {
                    pieceIcon.Stretch = Stretch.Uniform; // Ensure proper scaling
                    square.Child = pieceIcon; // Set the Viewbox as the child
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
           
            _chessboard.Board[position.Row][ position.Col] = newPiece;
            UpdateSquare(position.Row, position.Col); // Refresh the board after promotion
        }
        private void AnimateKingAndFlashCells()
        {
            // Get the king's position based on the current turn's color
            var kingPosition = _chessboard.CurrentTurn == PieceColor.White
                ? _chessboard.WhiteKingPosition
                : _chessboard.BlackKingPosition;

            // Rattle the king with a shake animation
            UIElement? kingImage = GetPieceElementAt(kingPosition.Row + 1, kingPosition.Col + 1);
            if (kingImage != null)
            {
                ShakeAnimation(kingImage);
            }
            FlashGridCell(kingPosition.Row + 1, kingPosition.Col + 1);

        }

        private void ShakeAnimation(UIElement element)
        {
            // Create the shake animation
            DoubleAnimation shakeAnimation = new DoubleAnimation
            {
                From = -5,
                To = 5,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(6) // Shake back and forth 3 times
            };

            TranslateTransform transform = new TranslateTransform();
            element.RenderTransform = transform;

            // Add a handler to reset the transform after the animation completes
            shakeAnimation.Completed += (s, e) =>
            {
                // Stop the animation by setting it to null
                transform.BeginAnimation(TranslateTransform.XProperty, null);

                // Reset the element's position to the origin
                transform.X = 0;
                transform.Y = 0;
            };

            // Start the animation
            transform.BeginAnimation(TranslateTransform.XProperty, shakeAnimation);
        }


        private async void FlashGridCell(int row, int col)
        {
            Border? square = GetSquareAt(row, col);
            if (square == null) return;

            // Store the original background color
            Brush originalBackground = square.Background;

            SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
            square.Background = redBrush;
            redBrush.BeginAnimation(SolidColorBrush.ColorProperty, _flashAnimation);


            // Wait for the animation to complete before resetting the background
            await Task.Delay(1800); // 300ms * 3 * 2 (for AutoReverse)

            // Reset to the original background
            square.Background = originalBackground;
        }



        private void DisplayCrown(PieceColor? winnerColor, string? exceptionMessage)
        {
            // Create an overlay grid to cover the entire window
            Grid overlayGrid = new Grid
            {
                Background = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            string crownUnicode = "\u265A"; // Crown symbol
            TextBlock crownTextBlock = new TextBlock
            {
                Text = crownUnicode,
                FontSize = 200,
                Foreground = Brushes.Gold,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            overlayGrid.Children.Add(crownTextBlock);

            TextBlock winnerMessage = new TextBlock
            {
                Text = winnerColor.HasValue ? (winnerColor == PieceColor.White ? "White Wins!" : "Black Wins!") : "It's a Tie!",
                FontSize = 50,
                Foreground = Brushes.Gold,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 0)
            };

            overlayGrid.Children.Add(winnerMessage);

            // Add exception message block if applicable
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                TextBlock exceptionMessageBlock = new TextBlock
                {
                    Text = exceptionMessage,
                    FontSize = 40,
                    Foreground = Brushes.Gold,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 80, 0, 0)
                };
                overlayGrid.Children.Add(exceptionMessageBlock);
            }

            ChessBoardGrid.Children.Add(overlayGrid);
            Grid.SetRowSpan(overlayGrid, 10);
            Grid.SetColumnSpan(overlayGrid, 6);
        }




        private void ShowErrorMessage(string message, string title)
        {
            // Ensure message box is shown on the UI thread
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        private void ShowWarningMessage(string message, string title)
        {
            // Ensure message box is shown on the UI thread
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }


        private void RenderDrawingOnBoard()
        {
            DrawingCanvas.Children.Clear();
        }

        private void BounceAnimation(UIElement pieceText)
        {
            pieceText.RenderTransform = new TranslateTransform();
            pieceText.RenderTransform.BeginAnimation(TranslateTransform.YProperty, _sharedBounceAnimation);
        }


        private void FadeOutAndMoveUpAnimation(UIElement pieceText, Action onCompleted)
        {
            // Create a TranslateTransform if not already present
            if (pieceText.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                pieceText.RenderTransform = transform;
            }

            // Fade out animation
            DoubleAnimation fadeOutAnimation = new DoubleAnimation
            {
                From = 1.0, // Fully visible
                To = 0.0, // Fade out
                Duration = TimeSpan.FromMilliseconds(200),
                FillBehavior = FillBehavior.HoldEnd
            };

            // Upward movement animation
            DoubleAnimation moveUpAnimation = new DoubleAnimation
            {
                From = 0, // Start from current position
                To = -20, // Move up by 20 pixels
                Duration = TimeSpan.FromMilliseconds(200),
                FillBehavior = FillBehavior.HoldEnd
            };

            // Set the fade-out animation completion action
            fadeOutAnimation.Completed += (s, e) => onCompleted(); // Call onCompleted when fade out is done

            // Start both animations
            pieceText.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            transform.BeginAnimation(TranslateTransform.YProperty, moveUpAnimation);
        }

        private UIElement? GetPieceElementAt(int row, int col)
        {
            foreach (var child in ChessBoardGrid.Children)
            {
                if (child is Border square &&
                    Grid.GetRow(square) == row &&
                    Grid.GetColumn(square) == col)
                {
                    // Return the child of the Border (e.g., Image or Viewbox)
                    return square.Child;
                }
            }
            return null;
        }

        private void HighlightValidMoves(List<(int, int)> validMoves)
        {
            if (_userColor == _chessboard.CurrentTurn)
            {
                foreach (var move in validMoves)
                {
                    Border? squareToHighlight = GetSquareAt(move.Item1 + 1, move.Item2 + 1);
                    if (squareToHighlight != null)
                    {
                        // Fill the background with green color
                        squareToHighlight.Background = Brushes.ForestGreen; // Fill color
                    }
                }
            }
            else
            {
                foreach (var move in validMoves)
                {
                    Border? squareToHighlight = GetSquareAt(move.Item1 + 1, move.Item2 + 1);
                    if (squareToHighlight != null)
                    {
                        // Fill the background with green color
                        squareToHighlight.Background = _redHighlight; // Tomato red
                    }
                }
            }
        }

        private void ClearHighlights()
        {
            for (int row = 1; row <= 8; row++)
            {
                for (int col = 1; col <= 4; col++)
                {
                    Border? square = GetSquareAt(row, col);
                    if (square != null)
                    {
                        square.Background = (row + col) % 2 == 0 ? _lightSquareGradient : _darkSquareGradient;
                    }
                }
            }
        }

        private Border? GetSquareAt(int row, int col)
        {
            if (row < 1 || row > 8 || col < 1 || col > 4) return null;
            return _squareCache[row - 1, col - 1]; // Use the cached square
        }
        public void PlayMove()
        {
            _movePlayer.Stop();  // Stop if already playing
            _movePlayer.Position = TimeSpan.Zero; // Reset position to the start
            _movePlayer.Play();
        }
        public void PlayTick()
        {
            _tickPlayer.Stop();  // Stop if already playing
            _tickPlayer.Position = TimeSpan.Zero; // Reset position to the start
            _tickPlayer.Play();
        }

        

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private async void CloseWindow(object sender, RoutedEventArgs e)
        {
            // Stop the timer and release resources
            _gameTimer?.Stop();

            if (_isGameStarted && !_isGameEnded)
            {
         
                // Set the game as an auto-win for White
                _chessboard.Winner = _computerColor;
                _chessboard.WinMethod = $"Opponent quit the game. {_computerColor} by default.";

                // Update the winner and win method in the database
                _gameRecordService.UpdateGameWinner(
                    _currentGameId,
                    _chessboard.Winner.ToString() ??"Unknown",
                    _chessboard.WinMethod
                );
                await UpdateWinnerOnServer(_chessboard.Winner, _chessboard.WinMethod);
               // await SaveGameStepsToServer();

                // Display the crown for White
                DisplayCrown(_chessboard.Winner, _chessboard.WinMethod);
                if (_ongoingTasks.Count > 0)
                {
                    try
                    {
                        // Await completion of all ongoing tasks
                        await Task.WhenAll(_ongoingTasks);
                    }
                    catch (AggregateException aggregateEx)
                    {
                        bool connectionLost = false;

                        // Handle each exception individually
                        foreach (var ex in aggregateEx.InnerExceptions)
                        {
                            if (ex is HttpRequestException)
                            {
                                connectionLost = true;
                            }
                            else
                            {
                                // Handle other exceptions gracefully
                                ShowErrorMessage($"Error while closing: {ex.Message}", "Error");
                            }
                        }

                        // If any task threw HttpRequestException, show the connection lost message
                        if (connectionLost)
                        {
                            ShowConnectionLostMessage();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Catch any other unexpected exceptions
                        ShowErrorMessage($"Unexpected error while closing: {ex.Message}", "Error");
                    }
                    finally
                    {

                        _ongoingTasks.Clear();

                    }
                }
            }
            ReleaseMediaPlayers();
            UnregisterEventHandlers();
            DisposeBitmap();  // Dispose of the bitmap when exiting drawing mode
            _httpClient.Dispose();
            this.Close();
        }
        private void ReleaseMediaPlayers()
        {
            _movePlayer.Close();
            _tickPlayer.Close();
        }


        // Enable dragging of the window
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private async void PlayGame(object sender, RoutedEventArgs e)
        {
            if (_isGameStarted && !_isGameEnded)
            {
                ShowErrorMessage("Finish the game in order to start a new one.", "Error");
                return;
            }

            // Open the time selection dialog
            var timeSelectionWindow = new TimeSelectionWindow();
            bool? result = timeSelectionWindow.ShowDialog();

            if (result == true) // If the player selects a time and presses OK
            {
                if (int.TryParse(timeSelectionWindow.UserId, out int userId) && userId >= 1 && userId <= 1000)
                {
                    // Assign colors based on the user's choice
                    PieceColor userColor = timeSelectionWindow.SelectedColor;
                    PieceColor computerColor = userColor == PieceColor.White ? PieceColor.Black : PieceColor.White;
                    // Store the user and computer colors for reference during the game
                    _userColor = userColor;
                    _computerColor = computerColor;

                    bool isStored = await StoreGameAsync(userId);
                    if (!isStored) return; // Exit if the game could not be saved

                    // Set the game status and time
                    _apiTurn = false;
                    _isGameStarted = true;
                    _isGameEnded = false;
                    _turnTime = timeSelectionWindow.SelectedTimeInSeconds;
                    _stepOrder = 0;

                    // Hide the welcome canvas and show the chessboard grid
                    DrawingCanvas.Children.Clear();
                    ChessBoardGrid.Visibility = Visibility.Visible;


                    if(userColor == PieceColor.White)
                    {
                        _chessboard= new Chessboard(isWhiteAtBottom:true);

                    }
                    else _chessboard = new Chessboard(isWhiteAtBottom: false);
                    if(_squareCache == null)
                    _squareCache =new Border[_chessboard.Rows, _chessboard.Cols];
                    _chessboard.CurrentTurn = PieceColor.White;
                    InitializeLabels();
                    RenderBoard();
                    InitializeGameTimer();
                    // If the user chose black, get the first move from the API (white starts)
                    if (userColor == PieceColor.Black)
                    {
                        await GetMoveFromApi();
                    }
                }
                else
                {
                    ShowErrorMessage("Invalid User ID. Please enter a number between 1 and 1000.", "Input Error");
                    return;
                }
            }
        }


        private async Task<bool> StoreGameAsync(int userId)
        {
            try
            {
                var storeGameRequest = new StoreGameRequest
                {
                    UserId = userId,
                    UserColor = _userColor,
                    ComputerColor = _computerColor
                };

                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("store-game", storeGameRequest);

                if (response.IsSuccessStatusCode)
                {
                    _currentGameId = await response.Content.ReadAsAsync<int>(); // Get the generated GameId

                    // Now save the game to the local database
                    _gameRecordService.SaveGame(userId, _currentGameId,_userColor,_computerColor); // Save to local DB
                    return true; // Indicate success
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ShowRegistrationPrompt(); // Show registration prompt if user is not found
                    return false; // Exit if the game could not be saved
                }
                else
                {
                    ShowErrorMessage($"Failed to save game to the server: {response.ReasonPhrase}", "Server Error");
                    return false; // Exit if the game could not be saved
                }
            }
            catch (HttpRequestException)
            {
                ShowErrorMessage("Lost connection to the server. Please try again later.", "Network Error");
                return false; // Exit if there was a network issue
            }
            catch (TaskCanceledException)
            {
                ShowErrorMessage("The request timed out. Please try again later.", "Request Timeout");
                return false; // Exit if the request timed out
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"An unexpected error occurred: {ex.Message}", "Error");
                return false; // Exit on unexpected error
            }
        }


        private void ShowRegistrationPrompt()
        {
            MessageBoxResult result = MessageBox.Show(
                "User not found. Please register to our system before playing here.",
                "Registration Required",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.OK)
            {
                // Open registration link in the default web browser
                string url = "https://localhost:8000/Users/Create";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Use the OS shell to open the URL
                });
            }
        }
        private void OpenRecords(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create and show the ReplayWindow
                var replayWindow = new ReplayWindow();
                replayWindow.ShowDialog(); // Use ShowDialog() to open as a modal window
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Error opening records: {ex.Message}", "Error Opening Records");
            }
        }
        private void OpenRegistrationWebsite(object sender, RoutedEventArgs e)
        {
            string url = "https://localhost:8000/Users/Create";
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message, "Error Opening Website");
            }
        }
        private void DisposeBitmap()
        {
            if (_writeableBitmap != null)
            {
                _writeableBitmap.Lock();
                _writeableBitmap = null;
            }
        }

        private void LoginUser(object sender, RoutedEventArgs e)
        {
            // To be implemented later.
            MessageBox.Show("Login feature is not implemented yet.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RestartGame(object sender, RoutedEventArgs e)
        {
            try
            {
                _chessboard = new Chessboard(); // Reset the chessboard logic
                InitializeLabels(); // Set up labels once
                RenderBoard(); // Re-render the chessboard
                ClearHighlights(); // Clear any highlights
                _selectedPiecePosition = null; // Reset selected piece state
                ResetGameTimer();
                MessageBox.Show("Game restarted!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message, "Error Restarting Game");
            }
        }
        private void UnregisterEventHandlers()
        {
            this.KeyDown -= MainWindow_KeyDown;
            DrawingImage.MouseLeftButtonDown -= DrawingImage_MouseLeftButtonDown;
            DrawingImage.MouseMove -= DrawingImage_MouseMove;
            DrawingImage.MouseLeftButtonUp -= DrawingImage_MouseLeftButtonUp;
        }
        private void StopGameTimer()
        {
            if (_gameTimer != null)
            {
                _gameTimer.Stop(); // Stop the timer if it's running
            }
        }
        private void ResetGameTimer()
        {
            _timeRemaining = _turnTime; // Reset time to 20 seconds

            if (_gameTimer != null && _gameTimer.IsEnabled)
            {
                _gameTimer.Stop(); // Stop the timer if it's running
            }

            _gameTimer?.Start(); // Start the timer again
            int minutes = _timeRemaining / 60;
            int seconds = _timeRemaining % 60;

            // Update the timer display with mm:ss format
            GameTimer.Text = $"Time Left: {minutes:D2}:{seconds:D2}";
        }
        private async Task SaveStepToServerAsync(MoveStepRequest moveStep)
        {
            try
            {
                // Send the move step to the server
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync("store-step", moveStep);

                if (!response.IsSuccessStatusCode)
                {
                    // If saving fails, display an error and close the game
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Network issue, display the connection lost message and close the game
                return;
            }
            catch (Exception)
            {
                return;
            }
        }
        private void ShowConnectionLostMessage()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBoxResult result = MessageBox.Show(
                    "Connection lost. The game will now close.",
                    "Connection Lost",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                if (result == MessageBoxResult.OK)
                {
                    CloseWindow(this, new RoutedEventArgs()); // Close the application
                }
            });
        }











    }

}
