using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ChessGameWPF
{
    public partial class TimeSelectionWindow : Window
    {
        public int SelectedTimeInSeconds { get; private set; } = 20; // Default time
        public string UserId { get; private set; } = string.Empty; // Default User ID
        public PieceColor SelectedColor { get; private set; } = PieceColor.White; // Default color

        public TimeSelectionWindow()
        {
            InitializeComponent();
            UserIdTextBox.GotFocus += UserIdTextBox_GotFocus; // Handle focus event
            UserIdTextBox.LostFocus += UserIdTextBox_LostFocus; // Handle lost focus event
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected time from the ComboBox
            var selectedItem = TimeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                string? timeString = selectedItem.Content.ToString();
                if (timeString == null) return;
                if (timeString.Contains("minute"))
                {
                    int minutes = int.Parse(timeString.Split(' ')[0]);
                    SelectedTimeInSeconds = minutes * 60;
                }
                else if (timeString.Contains("seconds"))
                {
                    SelectedTimeInSeconds = 20;
                }
            }

            // Get the User ID from the TextBox and validate it
            UserId = UserIdTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(UserId) ||
                !int.TryParse(UserId, out int userIdValue) ||
                userIdValue < 1 || userIdValue > 1000)
            {
                MessageBox.Show("Please enter a valid User ID (1 to 1000).",
                                "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Get the selected color from the ComboBox
            var selectedColorItem = ColorComboBox.SelectedItem as ComboBoxItem;
            if (selectedColorItem != null)
            {
                string? content = selectedColorItem.Content?.ToString();
                SelectedColor = (PieceColor)Enum.Parse(typeof(PieceColor), content ?? "DefaultColor");
            }

            this.DialogResult = true; // Close the window and return success
            this.Close();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            UserIdTextBox.GotFocus -= UserIdTextBox_GotFocus;
            UserIdTextBox.LostFocus -= UserIdTextBox_LostFocus;
            this.Close();
        }

        private void UserIdTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UserIdTextBox.Text == "Enter User ID") // Check for placeholder text
            {
                UserIdTextBox.Text = ""; // Clear the placeholder
                UserIdTextBox.Foreground = Brushes.White; // Set text color to white
            }
        }

        private void UserIdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UserIdTextBox.Text)) // Check if the TextBox is empty
            {
                UserIdTextBox.Text = "Enter User ID"; // Restore placeholder
                UserIdTextBox.Foreground = Brushes.Gray; // Set placeholder color
            }
        }
    }
}
