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
using System.Diagnostics;

namespace AkexiVN
{
    public partial class MainWindow : Window
    {
        private enum OverlaySource
        {
            MainMenu,
            InGameMenu
        }

        private readonly StoryService _storyService = new();
        private readonly SceneState _sceneState = new();
        private readonly SaveService _saveService = new();

        private StoryNode? _currentNode;
        private string _currentText = string.Empty;
        private int _textIndex;
        private readonly DispatcherTimer _typingTimer;
        private bool _isTyping;
        private const int TypingInterval = 50;

        private OverlaySource _currentOverlaySource = OverlaySource.MainMenu;

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
                await UpdateContinueButtonStateAsync();
                ShowMainMenu();
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

        private void ShowMainMenu()
        {
            MainMenuPanel.Visibility = Visibility.Visible;
            GamePanel.Visibility = Visibility.Collapsed;
            OverlayContainer.Visibility = Visibility.Collapsed;
        }

        private async Task UpdateContinueButtonStateAsync()
        {
            int latestSlot = await GetLatestSaveSlotAsync();
            ContinueGameButton.IsEnabled = latestSlot > 0;
        }

        private async Task<int> GetLatestSaveSlotAsync()
        {
            int latestSlot = -1;
            DateTime latestTime = DateTime.MinValue;

            for (int slot = 1; slot <= SaveService.MaxSlots; slot++)
            {
                SaveData? data = await _saveService.LoadAsync(slot);
                if (data != null && data.SaveTime > latestTime)
                {
                    latestTime = data.SaveTime;
                    latestSlot = slot;
                }
            }

            return latestSlot;
        }

        private void StartNewGame()
        {
            _sceneState.Background = string.Empty;
            _sceneState.Bgm = string.Empty;
            _sceneState.Characters.Clear();

            BgmPlayer.Stop();
            SePlayer.Stop();

            MainMenuPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            OverlayContainer.Visibility = Visibility.Collapsed;

            ShowStory("start");
        }

        private async Task ContinueGameAsync()
        {
            int latestSlot = await GetLatestSaveSlotAsync();
            if (latestSlot <= 0)
            {
                MessageBox.Show("暂无存档，请先开始游戏并存档。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MainMenuPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            OverlayContainer.Visibility = Visibility.Collapsed;

            await LoadGameAsync(latestSlot);
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

        private Image CreateCharacterImage(SceneCharacter character)
        {
            ApplyCharacterDefaults(character);

            var candidates = new List<string>();

            string id = !string.IsNullOrWhiteSpace(character.Id) ? character.Id : character.Name;

            // Treat Expression as the filename (or relative path). If it contains a folder separator,
            // use it as-is; otherwise use Assets/Characters/{Id}/{Expression}.
            if (!string.IsNullOrWhiteSpace(character.Expression))
            {
                string expr = character.Expression.Replace("\\", "/");
                if (expr.Contains('/'))
                {
                    candidates.Add(expr);
                }
                else if (!string.IsNullOrWhiteSpace(id))
                {
                    candidates.Add($"{id}/{expr}");
                }
            }

            BitmapImage? bitmap = null;

            foreach (var candidate in candidates)
            {
                string path = $"pack://application:,,,/Assets/Characters/{candidate}";
                if (TryLoadBitmapFromPack(path, out bitmap))
                {
                    Debug.WriteLine($"Loaded character image: {path}");
                    break;
                }
                else
                {
                    Debug.WriteLine($"Character image not found: {path}");
                }
            }

            if (bitmap == null)
            {
                Debug.WriteLine($"Failed to load any character image for Id='{character.Id}', Name='{character.Name}', Expression='{character.Expression}'");
            }

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

        private void ApplyCharacterDefaults(SceneCharacter character)
        {
            if (string.IsNullOrWhiteSpace(character.Id))
            {
                return;
            }

            CharacterProfile? profile = _storyService.GetCharacterProfile(character.Id);
            if (profile == null)
            {
                return;
            }

            if (character.Scale <= 0)
            {
                character.Scale = profile.DefaultScale > 0 ? profile.DefaultScale : 1;
            }

            if (character.OffsetX == 0 && profile.DefaultOffsetX != 0)
            {
                character.OffsetX = profile.DefaultOffsetX;
            }

            if (character.OffsetY == 0 && profile.DefaultOffsetY != 0)
            {
                character.OffsetY = profile.DefaultOffsetY;
            }
        }

        private static bool TryLoadBitmapFromPack(string packUri, out BitmapImage? bitmap)
        {
            try
            {
                bitmap = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryLoadBitmapFromPack failed for '{packUri}': {ex.Message}");
                bitmap = null;
                return false;
            }
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
                MessageBox.Show("这个存档不存在。", "读取失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _currentNode = _storyService.GetNode(data.CurrentNodeId);
            }
            catch
            {
                MessageBox.Show("这个存档对应的剧情节点已不存在。", "读取失败", MessageBoxButton.OK, MessageBoxImage.Error);
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

            MainMenuPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            OverlayContainer.Visibility = Visibility.Collapsed;
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
                    Text = $"存档 {slot} - {(data != null ? data.SaveTime.ToString("yyyy-MM-dd HH:mm") : "暂无存档")}",
                    Foreground = Brushes.White,
                    FontSize = 18,
                    FontWeight = FontWeights.Bold
                });

                if (data != null)
                {
                    info.Children.Add(new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(data.Background) ? "场景：未知" : $"场景：{data.Background}",
                        Foreground = Brushes.LightGray,
                        FontSize = 14,
                        Margin = new Thickness(0, 4, 0, 0)
                    });

                    info.Children.Add(new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(data.CurrentText) ? "剧情：无" : data.CurrentText,
                        Foreground = Brushes.GhostWhite,
                        FontSize = 13,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }
                else
                {
                    info.Children.Add(new TextBlock
                    {
                        Text = "暂无存档",
                        Foreground = Brushes.Gray,
                        FontSize = 14,
                        Margin = new Thickness(0, 4, 0, 0)
                    });
                }

                Button slotButton = new()
                {
                    Content = info,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    IsEnabled = isSaveMode || data != null
                };

                int targetSlot = slot;
                slotButton.Click += async (_, _) =>
                {
                    if (isSaveMode)
                    {
                        await SaveGameAsync(targetSlot);
                        await RefreshSaveSlotListAsync(true);
                        await UpdateContinueButtonStateAsync();
                    }
                    else
                    {
                        if (data == null)
                        {
                            return;
                        }
                        await LoadGameAsync(targetSlot);
                    }
                };

                slotBorder.Child = slotButton;
                SaveSlotList.Children.Add(slotBorder);
            }
        }

        // --- Main Menu Event Handlers ---

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        private async void ContinueGameButton_Click(object sender, RoutedEventArgs e)
        {
            await ContinueGameAsync();
        }

        private async void LoadGameMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.MainMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;

            await RefreshSaveSlotListAsync(isSaveMode: false);
        }

        private void SettingsMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.MainMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // --- In-Game Menu Event Handlers ---

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.InGameMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Visible;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
        }

        private async void InGameSaveButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;
            await RefreshSaveSlotListAsync(isSaveMode: true);
        }

        private async void InGameLoadButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;
            await RefreshSaveSlotListAsync(isSaveMode: false);
        }

        private void InGameSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private async void ReturnTitleButton_Click(object sender, RoutedEventArgs e)
        {
            BgmPlayer.Stop();
            SePlayer.Stop();

            _sceneState.Background = string.Empty;
            _sceneState.Bgm = string.Empty;
            _sceneState.Characters.Clear();

            await UpdateContinueButtonStateAsync();
            ShowMainMenu();
        }

        private void CloseMenuButton_Click(object sender, RoutedEventArgs e)
        {
            OverlayContainer.Visibility = Visibility.Collapsed;
        }

        // --- Back Buttons from Overlays ---

        private void BackFromSaveLoad_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOverlaySource == OverlaySource.MainMenu)
            {
                OverlayContainer.Visibility = Visibility.Collapsed;
                SaveLoadPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SaveLoadPanel.Visibility = Visibility.Collapsed;
                InGameMenuPanel.Visibility = Visibility.Visible;
            }
        }

        private void BackFromSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOverlaySource == OverlaySource.MainMenu)
            {
                OverlayContainer.Visibility = Visibility.Collapsed;
                SettingsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
                InGameMenuPanel.Visibility = Visibility.Visible;
            }
        }

        // --- Volume Settings Controls ---

        private void BgmVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BgmPlayer != null)
            {
                BgmPlayer.Volume = BgmVolumeSlider.Value;
            }
            if (BgmVolumeText != null)
            {
                BgmVolumeText.Text = $"{(int)(BgmVolumeSlider.Value * 100)}%";
            }
        }

        private void SeVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SePlayer != null)
            {
                SePlayer.Volume = SeVolumeSlider.Value;
            }
            if (SeVolumeText != null)
            {
                SeVolumeText.Text = $"{(int)(SeVolumeSlider.Value * 100)}%";
            }
        }
    }
}
