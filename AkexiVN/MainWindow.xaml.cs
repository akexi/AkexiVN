using AkexiVN.Models;
using AkexiVN.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AkexiVN
{
    public partial class MainWindow : Window
    {
        private readonly StoryService _storyService = new();
        private readonly SceneState _sceneState = new();
        private readonly SaveService _saveService = new();

        private StoryNode? _currentNode;
        private string _currentText = string.Empty;
        private int _textIndex;
        private readonly DispatcherTimer _typingTimer;
        private bool _isTyping;
        private const int TypingInterval = 50;

        public MainWindow()
        {
            InitializeComponent();

            _typingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(TypingInterval)
            };

            _typingTimer.Tick += TypingTimer_Tick;
            BgmPlayer.MediaEnded += BgmPlayer_MediaEnded;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
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

        private void ShowStory(string id, bool updateScene = true)
        {
            _currentNode = _storyService.GetNode(id);
            StopTyping();

            if (updateScene)
            {
                UpdateScene();
            }

            CharacterNameText.Text = _currentNode.Character;
            StartTyping(_currentNode.Text);

            if (_currentNode.Choices.Count > 0)
            {
                ChoicePanel.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                ChoicePanel.Visibility = Visibility.Collapsed;
                NextButton.Visibility = Visibility.Visible;
            }
        }

        private void StartTyping(string text)
        {
            _currentText = text;
            _textIndex = 0;
            _isTyping = true;
            DialogueText.Text = string.Empty;
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
            DialogueText.Text = _currentText[.._textIndex];
        }

        private void StopTyping()
        {
            _typingTimer.Stop();
            _isTyping = false;
        }

        private void OnTypingFinished()
        {
            if (_currentNode == null)
            {
                return;
            }

            if (_currentNode.Choices.Count > 0)
            {
                ShowChoices();
                return;
            }

            NextButton.Visibility = Visibility.Visible;
        }

        private void ShowChoices()
        {
            ChoicePanel.Children.Clear();
            ChoicePanel.Visibility = Visibility.Visible;
            Panel.SetZIndex(ChoicePanel, 100);
            ChoicePanel.VerticalAlignment = VerticalAlignment.Bottom;
            ChoicePanel.Margin = new Thickness(30, 0, 30, 220);
            NextButton.Visibility = Visibility.Collapsed;

            foreach (Choice choice in _currentNode!.Choices)
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
                    ChoicePanel.Visibility = Visibility.Collapsed;
                    DialogueBox.Visibility = Visibility.Visible;
                    ShowStory(nextId);
                };

                ChoicePanel.Children.Add(button);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentNode == null)
            {
                return;
            }

            if (_isTyping)
            {
                StopTyping();
                DialogueText.Text = _currentText;
                OnTypingFinished();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_currentNode.Next))
            {
                ShowStory(_currentNode.Next);
            }
            else
            {
                MessageBox.Show("故事结束。", "AkexiVN");
            }
        }

        private const double DesignWidth = 1280;
        private const double DesignHeight = 720;
        private const double CharacterBaseWidth = 500;
        private const double CharacterBaseHeight = 820;
        private const double CharacterViewportHeight = 480;

        private void UpdateCharacters()
        {
            CharacterLayer.Children.Clear();

            if (_currentNode == null)
            {
                return;
            }

            foreach (SceneCharacter character in _currentNode.Characters)
            {
                Image image = CreateCharacterImage(character);
                CharacterLayer.Children.Add(image);
            }
        }

        private Image CreateCharacterImage(SceneCharacter character)
        {
            string imageFile = !string.IsNullOrWhiteSpace(character.Image)
                ? character.Image
                : $"{character.Name}/{character.Expression}.png";

            string path = $"pack://application:,,,/Assets/Characters/{imageFile}";
            BitmapImage bitmap = new(new Uri(path));

            Image image = new()
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 1),
                Clip = new RectangleGeometry(new Rect(0, 0, CharacterBaseWidth, CharacterViewportHeight))
            };

            ApplyCharacterLayout(image, character);

            if (character.Effect.Equals("fade", StringComparison.OrdinalIgnoreCase))
            {
                FadeIn(image);
            }
            else
            {
                image.Opacity = character.Opacity;
            }

            return image;
        }

        private void ApplyCharacterLayout(Image image, SceneCharacter character)
        {
            double scale = character.Scale <= 0 ? 1 : character.Scale;
            double width = CharacterBaseWidth * scale;
            double height = CharacterBaseHeight * scale;
            double viewportHeight = CharacterViewportHeight * scale;
            double anchorX = GetCharacterAnchorX(character.Position);

            image.Width = width;
            image.Height = height;
            image.Clip = new RectangleGeometry(new Rect(0, 0, width, viewportHeight));
            Canvas.SetLeft(image, anchorX - (width / 2) + character.OffsetX);
            Canvas.SetBottom(image, character.OffsetY);
        }

        private static double GetCharacterAnchorX(string position)
        {
            return position?.Trim().ToLowerInvariant() switch
            {
                "left" => DesignWidth * 0.25,
                "right" => DesignWidth * 0.75,
                _ => DesignWidth * 0.5
            };
        }

        private void FadeIn(Image image)
        {
            DoubleAnimation animation = new()
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            image.BeginAnimation(UIElement.OpacityProperty, animation);
        }

        private void UpdateScene()
        {
            if (_currentNode == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(_currentNode.Background))
            {
                _sceneState.Background = _currentNode.Background;
                string path = $"pack://application:,,,/Assets/Backgrounds/{_sceneState.Background}";
                BackgroundImage.Source = new BitmapImage(new Uri(path));
            }

            UpdateBgm();

            if (!string.IsNullOrWhiteSpace(_currentNode.Se))
            {
                PlaySoundEffect(_currentNode.Se);
            }

            foreach (SceneCharacter character in _currentNode.Characters)
            {
                if (character.Effect.Equals("hide", StringComparison.OrdinalIgnoreCase))
                {
                    var keysToRemove = _sceneState.Characters
                        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.Id) && kvp.Value.Id == character.Id)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        _sceneState.Characters.Remove(key);
                    }
                }
                else
                {
                    var prevKeys = _sceneState.Characters
                        .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value.Id) && kvp.Value.Id == character.Id && kvp.Key != character.Position)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in prevKeys)
                    {
                        _sceneState.Characters.Remove(key);
                    }

                    _sceneState.Characters[character.Position] = character;
                }
            }

            RenderCharacters();
        }

        private void RenderCharacters()
        {
            CharacterLayer.Children.Clear();

            foreach (SceneCharacter character in _sceneState.Characters.Values)
            {
                Image image = CreateCharacterImage(character);
                CharacterLayer.Children.Add(image);
            }
        }

        private void UpdateBgm()
        {
            if (_currentNode == null || string.IsNullOrWhiteSpace(_currentNode.Bgm))
            {
                return;
            }

            if (string.Equals(_sceneState.Bgm, _currentNode.Bgm, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _sceneState.Bgm = _currentNode.Bgm;
            PlayBgm(_sceneState.Bgm);
        }

        private void PlayBgm(string bgmFileName)
        {
            if (string.IsNullOrWhiteSpace(bgmFileName))
            {
                return;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "BGM", bgmFileName);
            if (!File.Exists(path))
            {
                return;
            }

            BgmPlayer.Stop();
            BgmPlayer.Source = new Uri(path);
            BgmPlayer.Position = TimeSpan.Zero;
            BgmPlayer.Play();
        }

        private void BgmPlayer_MediaEnded(object? sender, RoutedEventArgs e)
        {
            BgmPlayer.Position = TimeSpan.Zero;
            BgmPlayer.Play();
        }

        private void PlaySoundEffect(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "SE", fileName);
            if (!File.Exists(path))
            {
                return;
            }

            SePlayer.Stop();
            SePlayer.Source = new Uri(path);
            SePlayer.Position = TimeSpan.Zero;
            SePlayer.Play();
        }

        private async Task SaveGameAsync(int slot)
        {
            if (_currentNode == null)
            {
                return;
            }

            SaveData data = new()
            {
                CurrentNodeId = _currentNode.Id,
                Background = _sceneState.Background,
                Bgm = _sceneState.Bgm,
                CurrentCharacterName = _currentNode.Character,
                CurrentText = string.IsNullOrWhiteSpace(_currentText) ? _currentNode.Text : _currentText,
                Characters = new Dictionary<string, SceneCharacter>(_sceneState.Characters),
                SaveTime = DateTime.Now
            };

            await _saveService.SaveAsync(slot, data);
        }

        private async Task LoadGameAsync(int slot)
        {
            SaveData? data = await _saveService.LoadAsync(slot);
            if (data == null)
            {
                SetSaveLoadStatus("这个存档不存在。", "请先在当前槽位上保存游戏。", false);
                return;
            }

            try
            {
                _currentNode = _storyService.GetNode(data.CurrentNodeId);
            }
            catch
            {
                SetSaveLoadStatus("这个存档对应的剧情节点已不存在。", "请重新开始游戏。", false);
                return;
            }

            _sceneState.Background = data.Background;
            _sceneState.Bgm = data.Bgm;
            _sceneState.Characters = new Dictionary<string, SceneCharacter>(data.Characters ?? new Dictionary<string, SceneCharacter>());

            UpdateLoadedSceneVisuals();

            StopTyping();
            _currentText = string.IsNullOrWhiteSpace(data.CurrentText) ? _currentNode.Text : data.CurrentText;
            _textIndex = _currentText.Length;
            CharacterNameText.Text = string.IsNullOrWhiteSpace(data.CurrentCharacterName) ? _currentNode.Character : data.CurrentCharacterName;
            DialogueText.Text = _currentText;
            NextButton.Visibility = Visibility.Collapsed;
            ChoicePanel.Visibility = Visibility.Collapsed;
            DialogueBox.Visibility = Visibility.Visible;

            if (_currentNode.Choices.Count > 0)
            {
                ShowChoices();
            }
            else
            {
                NextButton.Visibility = Visibility.Visible;
            }

            CloseMenuOverlay();
        }

        private void UpdateLoadedSceneVisuals()
        {
            if (!string.IsNullOrWhiteSpace(_sceneState.Background))
            {
                string path = $"pack://application:,,,/Assets/Backgrounds/{_sceneState.Background}";
                BackgroundImage.Source = new BitmapImage(new Uri(path));
            }

            RenderCharacters();

            if (!string.IsNullOrWhiteSpace(_sceneState.Bgm))
            {
                PlayBgm(_sceneState.Bgm);
            }
            else
            {
                BgmPlayer.Stop();
            }
        }

        private void SetSaveLoadStatus(string title, string status, bool isSaveMode)
        {
            SaveLoadTitle.Text = title;
            SaveLoadStatus.Text = status;
            SaveLoadPanel.Visibility = Visibility.Visible;
            MenuPanel.Visibility = Visibility.Collapsed;
            SaveSlotList.Children.Clear();

            for (int slot = 1; slot <= SaveService.MaxSlots; slot++)
            {
                SaveData? data = _saveService.LoadAsync(slot).GetAwaiter().GetResult();

                Border slotBorder = new()
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(12)
                };

                StackPanel info = new();
                info.Children.Add(new TextBlock
                {
                    Text = $"[{slot}] {(data != null ? data.SaveTime.ToString("yyyy-MM-dd HH:mm") : "空存档")}",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                });

                info.Children.Add(new TextBlock
                {
                    Text = data != null ? (string.IsNullOrWhiteSpace(data.Background) ? "未知场景" : data.Background) : "空存档",
                    Foreground = Brushes.LightGray,
                    FontSize = 14,
                    Margin = new Thickness(0, 6, 0, 0)
                });

                info.Children.Add(new TextBlock
                {
                    Text = data != null
                        ? (string.IsNullOrWhiteSpace(data.CurrentCharacterName) ? "角色：无" : $"角色：{data.CurrentCharacterName}")
                        : "角色：无",
                    Foreground = Brushes.GhostWhite,
                    FontSize = 13,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                string detail = data != null
                    ? (!string.IsNullOrWhiteSpace(data.CurrentText) ? data.CurrentText : "当前剧情信息：无")
                    : "当前剧情信息：无";

                info.Children.Add(new TextBlock
                {
                    Text = detail,
                    Foreground = Brushes.GhostWhite,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                Button slotButton = new()
                {
                    Content = info,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                int targetSlot = slot;
                slotButton.Click += async (_, _) =>
                {
                    if (isSaveMode)
                    {
                        await SaveGameAsync(targetSlot);
                        await RefreshSaveSlotListAsync(true);
                    }
                    else
                    {
                        await LoadGameAsync(targetSlot);
                    }
                };

                slotBorder.Child = slotButton;
                SaveSlotList.Children.Add(slotBorder);
            }
        }

        private async Task RefreshSaveSlotListAsync(bool isSaveMode)
        {
            SaveLoadTitle.Text = isSaveMode ? "保存游戏" : "读取游戏";
            SaveLoadStatus.Text = isSaveMode
                ? "选择一个存档槽位进行保存。"
                : "选择一个存档槽位进行读取。";

            SaveSlotList.Children.Clear();

            for (int slot = 1; slot <= SaveService.MaxSlots; slot++)
            {
                SaveData? data = await _saveService.LoadAsync(slot);

                Border slotBorder = new()
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(12)
                };

                StackPanel info = new();
                info.Children.Add(new TextBlock
                {
                    Text = $"[{slot}] {(data != null ? data.SaveTime.ToString("yyyy-MM-dd HH:mm") : "空存档")}",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                });

                info.Children.Add(new TextBlock
                {
                    Text = data != null ? (string.IsNullOrWhiteSpace(data.Background) ? "未知场景" : data.Background) : "空存档",
                    Foreground = Brushes.LightGray,
                    FontSize = 14,
                    Margin = new Thickness(0, 6, 0, 0)
                });

                info.Children.Add(new TextBlock
                {
                    Text = data != null
                        ? (string.IsNullOrWhiteSpace(data.CurrentCharacterName) ? "角色：无" : $"角色：{data.CurrentCharacterName}")
                        : "角色：无",
                    Foreground = Brushes.GhostWhite,
                    FontSize = 13,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                info.Children.Add(new TextBlock
                {
                    Text = data != null
                        ? (!string.IsNullOrWhiteSpace(data.CurrentText) ? data.CurrentText : "当前剧情信息：无")
                        : "当前剧情信息：无",
                    Foreground = Brushes.GhostWhite,
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                Button slotButton = new()
                {
                    Content = info,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                int targetSlot = slot;
                slotButton.Click += async (_, _) =>
                {
                    if (isSaveMode)
                    {
                        await SaveGameAsync(targetSlot);
                        await RefreshSaveSlotListAsync(true);
                    }
                    else
                    {
                        await LoadGameAsync(targetSlot);
                    }
                };

                slotBorder.Child = slotButton;
                SaveSlotList.Children.Add(slotBorder);
            }
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMenuOverlay();
        }

        private void ShowMenuOverlay()
        {
            MenuOverlay.Visibility = Visibility.Visible;
            MenuPanel.Visibility = Visibility.Visible;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
        }

        private void CloseMenuOverlay()
        {
            MenuOverlay.Visibility = Visibility.Collapsed;
            MenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
        }

        private async void SaveMenuButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLoadPanel.Visibility = Visibility.Visible;
            MenuPanel.Visibility = Visibility.Collapsed;
            await RefreshSaveSlotListAsync(true);
        }

        private async void LoadMenuButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLoadPanel.Visibility = Visibility.Visible;
            MenuPanel.Visibility = Visibility.Collapsed;
            await RefreshSaveSlotListAsync(false);
        }

        private void SettingsMenuButton_Click(object sender, RoutedEventArgs e)
        {
            SaveLoadTitle.Text = "设置";
            SaveLoadStatus.Text = "设置功能尚未开放，后续可在这里扩展音量和界面参数。";
            SaveLoadPanel.Visibility = Visibility.Visible;
            MenuPanel.Visibility = Visibility.Collapsed;
            SaveSlotList.Children.Clear();

            StackPanel settingsPanel = new();
            settingsPanel.Children.Add(new TextBlock
            {
                Text = "当前版本：AkexiVN",
                Foreground = Brushes.White,
                FontSize = 18,
                Margin = new Thickness(0, 0, 0, 8)
            });
            settingsPanel.Children.Add(new TextBlock
            {
                Text = "菜单系统已接入，保存/读取界面已可用。",
                Foreground = Brushes.LightGray,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            SaveSlotList.Children.Add(settingsPanel);
        }

        private void ReturnTitleButton_Click(object sender, RoutedEventArgs e)
        {
            CloseMenuOverlay();

            _sceneState.Background = string.Empty;
            _sceneState.Bgm = string.Empty;
            _sceneState.Characters.Clear();

            ShowStory("start");
        }

        private void CloseMenuButton_Click(object sender, RoutedEventArgs e)
        {
            CloseMenuOverlay();
        }

        private void BackToMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMenuOverlay();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            await SaveGameAsync(1);
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadGameAsync(1);
        }
    }
}