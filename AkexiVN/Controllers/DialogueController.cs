using AkexiVN.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AkexiVN.Controllers
{
    public class DialogueController
    {
        private readonly TextBlock _nameText;
        private readonly TextBlock _dialogueText;
        private readonly StackPanel _choicePanel;
        private readonly Button _nextButton;
        private readonly DispatcherTimer _typingTimer;
        private string _currentText = string.Empty;
        private int _textIndex;

        public StoryNode? CurrentNode { get; private set; }
        public bool IsTyping { get; private set; }
        public string CurrentText => _currentText;
        public event Action<string>? NodeRequested;
        public event Action? ChapterEndRequested;

        public DialogueController(TextBlock nameText, TextBlock dialogueText, StackPanel choicePanel, Button nextButton, Dispatcher dispatcher)
        {
            _nameText = nameText;
            _dialogueText = dialogueText;
            _choicePanel = choicePanel;
            _nextButton = nextButton;
            _typingTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) { Interval = TimeSpan.FromMilliseconds(50) };
            _typingTimer.Tick += TypingTimer_Tick;
        }

        public void ShowNode(StoryNode node)
        {
            CurrentNode = node;
            StopTyping();
            _nameText.Text = node.Character;
            _choicePanel.Visibility = Visibility.Collapsed;
            _nextButton.Visibility = node.Choices.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            StartTyping(node.Text);
        }

        public void RestoreText(StoryNode node, string text, string characterName)
        {
            CurrentNode = node;
            StopTyping();
            _currentText = string.IsNullOrWhiteSpace(text) ? node.Text : text;
            _textIndex = _currentText.Length;
            _nameText.Text = string.IsNullOrWhiteSpace(characterName) ? node.Character : characterName;
            _dialogueText.Text = _currentText;
        }

        public void StopTyping()
        {
            _typingTimer.Stop();
            IsTyping = false;
        }

        public void Next()
        {
            if (CurrentNode == null) return;
            if (IsTyping)
            {
                StopTyping();
                _dialogueText.Text = _currentText;
                OnTypingFinished();
                return;
            }

            if (!string.IsNullOrWhiteSpace(CurrentNode.Next))
            {
                NodeRequested?.Invoke(CurrentNode.Next);
            }
            else
            {
                ChapterEndRequested?.Invoke();
            }
        }

        private void StartTyping(string text)
        {
            _currentText = text;
            _textIndex = 0;
            IsTyping = true;
            _dialogueText.Text = string.Empty;
            _typingTimer.Start();
        }

        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            if (_textIndex >= _currentText.Length)
            {
                StopTyping();
                OnTypingFinished();
                return;
            }

            _textIndex++;
            _dialogueText.Text = _currentText[.._textIndex];
        }

        private void OnTypingFinished()
        {
            if (CurrentNode == null) return;
            if (CurrentNode.Choices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                _nextButton.Visibility = Visibility.Visible;
            }
        }

        public void ShowChoices()
        {
            _choicePanel.Children.Clear();
            _choicePanel.Visibility = Visibility.Visible;
            Panel.SetZIndex(_choicePanel, 100);
            _choicePanel.VerticalAlignment = VerticalAlignment.Bottom;
            _choicePanel.Margin = new Thickness(30, 0, 30, 220);
            _nextButton.Visibility = Visibility.Collapsed;

            foreach (Choice choice in CurrentNode!.Choices)
            {
                Button button = new()
                {
                    Content = choice.Text,
                    FontSize = 22,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 20)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 10, 0, 10),
                    Padding = new Thickness(20)
                };
                string nextId = choice.Next;
                button.Click += (_, _) =>
                {
                    _choicePanel.Visibility = Visibility.Collapsed;
                    NodeRequested?.Invoke(nextId);
                };
                _choicePanel.Children.Add(button);
            }
        }
    }
}
