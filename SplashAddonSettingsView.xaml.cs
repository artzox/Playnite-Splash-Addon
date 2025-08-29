// SplashAddonSettingsView.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SplashAddon
{
    public partial class SplashAddonSettingsView : UserControl
    {
        public SplashAddonSettingsView()
        {
            InitializeComponent();
        }

        private void ExcludedIdsTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    // Get current cursor position
                    int caretIndex = textBox.CaretIndex;

                    // Insert a new line at cursor position
                    string text = textBox.Text ?? "";
                    string newText = text.Insert(caretIndex, Environment.NewLine);

                    // Update the text
                    textBox.Text = newText;

                    // Move cursor to after the new line
                    textBox.CaretIndex = caretIndex + Environment.NewLine.Length;

                    // Mark the event as handled to prevent default behavior
                    e.Handled = true;

                    // Force focus to stay on the textbox
                    textBox.Focus();
                }
            }
        }

        private void GameSpecificTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            // Parse game-specific durations when the text box loses focus
            if (DataContext is SplashAddonSettings settings)
            {
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    settings.ParseGameSpecificDurations(textBox.Text);
                }
            }
        }
    }
}