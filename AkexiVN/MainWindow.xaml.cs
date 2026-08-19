using AkexiVN.Models;
using AkexiVN.Services;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Animation;

namespace AkexiVN
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly StoryService _storyService = new();

        private StoryNode? _currentNode;

        // 当前完整台词
        private string _currentText = string.Empty;

        // 当前已经显示到第几个字
        private int _textIndex;

        // 打字机定时器
        private readonly DispatcherTimer _typingTimer;

        // 是否正在打字
        private bool _isTyping;

        // 每个字显示的时间
        private const int TypingInterval = 50;

        public MainWindow()
        {
            InitializeComponent();

            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TypingInterval)
            };

            _typingTimer.Tick += TypingTimer_Tick;

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                await _storyService.LoadAsync();

                ShowStory("start");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "游戏启动失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowStory(string id)
        {
            _currentNode = _storyService.GetNode(id);

            // 停止上一句打字
            StopTyping();

            // =========================
            // 背景
            // =========================

            if (!string.IsNullOrWhiteSpace(
                _currentNode.Background))
            {
                string path =
                    $"pack://application:,,,/Assets/Backgrounds/{_currentNode.Background}";

                BackgroundImage.Source =
                    new BitmapImage(new Uri(path));
            }

            // =========================
            // 立绘
            // =========================

            UpdateCharacters();

            // =========================
            // 角色名字
            // =========================

            CharacterNameText.Text =
                _currentNode.Character;

            // =========================
            // 台词
            // =========================

            StartTyping(
                _currentNode.Text);

            // =========================
            // 选项
            // =========================

            if (_currentNode.Choices.Count > 0)
            {
                // 等台词打完以后才能选择
                ChoicePanel.Visibility =
                    Visibility.Collapsed;

                NextButton.Visibility =
                    Visibility.Collapsed;
            }
            else
            {
                ChoicePanel.Visibility =
                    Visibility.Collapsed;

                NextButton.Visibility =
                    Visibility.Visible;
            }
        }

        // =========================================
        // 开始打字
        // =========================================

        private void StartTyping(string text)
        {
            _currentText = text;

            _textIndex = 0;

            _isTyping = true;

            DialogueText.Text = string.Empty;

            _typingTimer.Start();
        }

        // =========================================
        // 打字机
        // =========================================

        private void TypingTimer_Tick(
            object? sender,
            EventArgs e)
        {
            if (_textIndex >= _currentText.Length)
            {
                StopTyping();

                OnTypingFinished();

                return;
            }

            _textIndex++;

            DialogueText.Text =
                _currentText[.._textIndex];
        }

        // =========================================
        // 停止打字
        // =========================================

        private void StopTyping()
        {
            _typingTimer.Stop();

            _isTyping = false;
        }

        // =========================================
        // 台词打完
        // =========================================

        private void OnTypingFinished()
        {
            if (_currentNode == null)
            {
                return;
            }

            // 有选项
            if (_currentNode.Choices.Count > 0)
            {
                ShowChoices();

                return;
            }

            NextButton.Visibility =
                Visibility.Visible;
        }

        // =========================================
        // 显示选项
        // =========================================

        private void ShowChoices()
        {
            ChoicePanel.Children.Clear();

            ChoicePanel.Visibility =
                Visibility.Visible;

            DialogueBox.Visibility =
                Visibility.Collapsed;

            NextButton.Visibility =
                Visibility.Collapsed;

            foreach (Choice choice
                     in _currentNode!.Choices)
            {
                Button button = new()
                {
                    Content = choice.Text,

                    FontSize = 22,

                    Foreground = Brushes.White,

                    Background =
                        new SolidColorBrush(
                            Color.FromArgb(
                                220,
                                20,
                                20,
                                20)),

                    BorderBrush =
                        new SolidColorBrush(
                            Color.FromArgb(
                                120,
                                255,
                                255,
                                255)),

                    BorderThickness =
                        new Thickness(1),

                    Margin =
                        new Thickness(
                            0,
                            10,
                            0,
                            10),

                    Padding =
                        new Thickness(
                            20)
                };

                string nextId = choice.Next;

                button.Click += (_, _) =>
                {
                    DialogueBox.Visibility =
                        Visibility.Visible;

                    ShowStory(nextId);
                };

                ChoicePanel.Children.Add(button);
            }
        }

        // =========================================
        // 点击下一句
        // =========================================

        private void NextButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_currentNode == null)
            {
                return;
            }

            // 如果正在打字
            // 第一次点击直接显示完整台词
            if (_isTyping)
            {
                StopTyping();

                DialogueText.Text =
                    _currentText;

                OnTypingFinished();

                return;
            }

            // 已经打完
            if (!string.IsNullOrWhiteSpace(
                _currentNode.Next))
            {
                ShowStory(
                    _currentNode.Next);
            }
            else
            {
                MessageBox.Show(
                    "故事结束。",
                    "AkexiVN");
            }
        }

        private void FadeInCharacter()
        {
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 1,
                Duration =
                    TimeSpan.FromMilliseconds(300)
            };

            CharacterLayer.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void FadeOutCharacter()
        {
            DoubleAnimation animation = new()
            {
                From = CharacterLayer.Opacity,
                To = 0,
                Duration =
                    TimeSpan.FromMilliseconds(300)
            };

            animation.Completed += (_, _) =>
            {
                CharacterLayer.Visibility =
                    Visibility.Hidden;
            };

            CharacterLayer.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }

        private void UpdateCharacters()
        {
            CharacterLayer.Children.Clear();

            if (_currentNode == null)
            {
                return;
            }

            foreach (SceneCharacter character
                     in _currentNode.Characters)
            {
                Image image = new()
                {
                    Stretch = Stretch.Uniform,
                    Height = 600,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Opacity = 0
                };

                string path =
                    $"pack://application:,,,/Assets/Characters/{character.Image}";

                image.Source =
                    new BitmapImage(new Uri(path));

                SetCharacterPosition(
                    image,
                    character.Position);

                CharacterLayer.Children.Add(image);

                if (character.Effect.Equals(
                    "fade",
                    StringComparison.OrdinalIgnoreCase))
                {
                    FadeIn(image);
                }
                else
                {
                    image.Opacity = character.Opacity;
                }
            }
        }

        private void SetCharacterPosition(Image image, string position)
        {
            switch (position.ToLower())
            {
                case "left":

                    image.HorizontalAlignment =
                        HorizontalAlignment.Left;

                    image.Margin =
                        new Thickness(
                            100,
                            0,
                            0,
                            80);

                    break;

                case "right":

                    image.HorizontalAlignment =
                        HorizontalAlignment.Right;

                    image.Margin =
                        new Thickness(
                            0,
                            0,
                            100,
                            80);

                    break;

                default:

                    image.HorizontalAlignment =
                        HorizontalAlignment.Center;

                    image.Margin =
                        new Thickness(
                            0,
                            0,
                            0,
                            80);

                    break;
            }
        }

        private void FadeIn(Image image)
        {
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 1,
                Duration =
                    TimeSpan.FromMilliseconds(300)
            };

            image.BeginAnimation(
                UIElement.OpacityProperty,
                animation);
        }
    }
}