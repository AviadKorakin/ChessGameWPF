using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChessGameWPF
{
    public partial class PromotionDialog : Window
    {
        public string SelectedPiece { get; private set; } = "Rook"; // Default to Rook

        public PromotionDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if the user selected an item from the ComboBox
            if (PieceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                SelectedPiece = selectedItem.Content.ToString() ?? "Rook"; // Use Rook as fallback
            }
            else
            {
                SelectedPiece = "Rook"; // Default to Rook if no valid selection
            }

            this.DialogResult = true; // Close the dialog with OK result
            this.Close();
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            // Ensure the default is set to "Rook" if the dialog is closed without selecting
            SelectedPiece = "Rook";
            this.DialogResult = false; // Indicate the dialog was closed without confirmation
            this.Close();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
