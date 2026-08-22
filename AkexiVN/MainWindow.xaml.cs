using AkexiVN.Controllers;
using AkexiVN.Models;
using AkexiVN.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AkexiVN
{
    public partial class MainWindow : Window
    {
        private enum OverlaySource { MainMenu, InGameMenu }

        private readonly GameController _gameController = new();
        private readonly SaveService _saveService = new();
        private readonly SceneController _sceneController;
        private readonly DialogueController _dialogueController;
        private readonly SaveController _saveController;
        private OverlaySource _currentOverlaySource = OverlaySource.MainMenu;

        public MainWindow()
        {
            InitializeComponent();
            _sceneController = new SceneController(_gameController.StoryService, _gameController.SceneState, BackgroundImage, CharacterLayer, BgmPlayer, SePlayer);
            _dialogueController = new DialogueController(CharacterNameText, DialogueText, ChoicePanel, NextButton, Dispatcher);
            _dialogueController.NodeRequested += ShowStory;
            _dialogueController.ChapterEndRequested += ProcessChapterEnd;
            _saveController = new SaveController(_saveService, _gameController, SaveSlotList, SaveLoadTitle, SaveLoadStatus, () => _dialogueController.CurrentText, LoadGameAsync, UpdateContinueButtonStateAsync);
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await _gameController.ChapterManager.LoadAsync();
                await _gameController.StoryService.LoadAsync();
                _gameController.InitializeChapterProgress();
                string currentChapterId = _gameController.StoryService.GetCurrentChapterId();
                if (!string.IsNullOrWhiteSpace(currentChapterId)) _gameController.StoryService.SetCurrentChapter(currentChapterId);
                await UpdateContinueButtonStateAsync();
                ShowMainMenu();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "游戏启动失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowMainMenu()
        {
            MainMenuPanel.Visibility = Visibility.Visible;
            GamePanel.Visibility = Visibility.Collapsed;
            OverlayContainer.Visibility = Visibility.Collapsed;
            HideChapterEnd();
            _sceneController.PlayBgm("main_menu.mp3");
        }

        private async Task UpdateContinueButtonStateAsync() => ContinueGameButton.IsEnabled = await _saveController.GetLatestSaveSlotAsync() > 0;

        private void StartNewGame()
        {
            _gameController.ResetScene();
            _sceneController.StopAudio();
            LoadStoryChapter(_gameController.GetStartChapterId());
        }

        private void LoadStoryChapter(string chapterId)
        {
            try
            {
                StoryNode node = _gameController.LoadChapter(chapterId);
                HideChapterEnd();
                MainMenuPanel.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;
                OverlayContainer.Visibility = Visibility.Collapsed;
                ShowStory(node.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"章节加载失败：{ex.Message}", "章节错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ContinueGameAsync()
        {
            int latestSlot = await _saveController.GetLatestSaveSlotAsync();
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

        private void ShowStory(string id)
        {
            try
            {
                StoryNode node = _gameController.ShowNode(id);
                HideChapterEnd();
                _sceneController.UpdateScene(node);
                _dialogueController.ShowNode(node);
                ChoicePanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"剧情加载失败：{ex.Message}", "剧情错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) => _dialogueController.Next();

        private void ProcessChapterEnd()
        {
            string chapterId = _gameController.StoryService.GetCurrentChapterId();
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                MessageBox.Show("本作目前内容已结束", "AkexiVN", MessageBoxButton.OK, MessageBoxImage.Information);
                ReturnToMainMenu();
                return;
            }
            _gameController.MarkChapterEnd();
            _gameController.CompleteCurrentChapter();
            ShowChapterEnd(chapterId);
        }

        private void ShowChapterEnd(string chapterId)
        {
            HideChapterEnd();
            ChapterEndPanel.Visibility = Visibility.Visible;
            DialogueBox.Visibility = Visibility.Collapsed;
            ChoicePanel.Visibility = Visibility.Collapsed;
            NextButton.Visibility = Visibility.Collapsed;
            CharacterLayer.Visibility = Visibility.Collapsed;
            int chapterNumber = GetChapterNumber(chapterId);
            ChapterEndChapterNumberText.Text = chapterNumber > 0 ? $"第{chapterNumber}章" : chapterId;
            ChapterEndTitleText.Text = _gameController.ChapterManager.GetChapterTitleById(chapterId);
            string? nextChapterId = _gameController.ChapterManager.GetNextChapterId(chapterId);
            ChapterEndContinueButton.Content = !string.IsNullOrWhiteSpace(nextChapterId)
                ? $"进入{_gameController.ChapterManager.GetChapterTitleById(nextChapterId)}"
                : "返回主菜单";
        }

        private void HideChapterEnd()
        {
            ChapterEndPanel.Visibility = Visibility.Collapsed;
            DialogueBox.Visibility = Visibility.Visible;
            CharacterLayer.Visibility = Visibility.Visible;
        }

        private static int GetChapterNumber(string chapterId)
        {
            string normalized = chapterId.Replace("chapter", string.Empty, StringComparison.OrdinalIgnoreCase);
            return int.TryParse(normalized, out int value) ? value : 0;
        }

        private void ChapterEndContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string chapterId = _gameController.StoryService.GetCurrentChapterId();
            string? nextChapterId = _gameController.ChapterManager.GetNextChapterId(chapterId);
            if (!string.IsNullOrWhiteSpace(nextChapterId)) LoadStoryChapter(nextChapterId);
            else ReturnToMainMenu();
        }

        private void ChapterEndReturnButton_Click(object sender, RoutedEventArgs e) => ReturnToMainMenu();

        private void ReturnToMainMenu()
        {
            _sceneController.StopAudio();
            _gameController.ResetScene();
            HideChapterEnd();
            ShowMainMenu();
        }

        private async Task LoadGameAsync(int slot)
        {
            try
            {
                SaveData? data = await _saveService.LoadAsync(slot);
                if (data == null)
                {
                    MessageBox.Show("这个存档不存在。", "读取失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                StoryNode node = _gameController.RestoreSaveData(data);
                _sceneController.RestoreVisuals();
                _dialogueController.RestoreText(node, data.CurrentText, data.CurrentCharacterName);
                NextButton.Visibility = Visibility.Collapsed;
                ChoicePanel.Visibility = Visibility.Collapsed;
                DialogueBox.Visibility = Visibility.Visible;
                CharacterLayer.Visibility = Visibility.Visible;
                if (_gameController.FlowState == GameFlowState.ChapterEnd) ShowChapterEnd(_gameController.StoryService.GetCurrentChapterId());
                else if (node.Choices.Count > 0) _dialogueController.ShowChoices();
                else NextButton.Visibility = Visibility.Visible;
                MainMenuPanel.Visibility = Visibility.Collapsed;
                GamePanel.Visibility = Visibility.Visible;
                OverlayContainer.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取失败：{ex.Message}", "读取失败", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();
        private async void ContinueGameButton_Click(object sender, RoutedEventArgs e) => await ContinueGameAsync();

        private async void LoadGameMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.MainMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;
            await _saveController.RefreshSlotListAsync(false);
        }

        private void SettingsMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.MainMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private void ChapterMainMenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.MainMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Visible;
            RefreshChapterList();
        }

        private void RefreshChapterList()
        {
            ChapterListPanel.Children.Clear();
            IReadOnlyList<Chapter> chapters = _gameController.ChapterManager.GetChapters();
            if (chapters.Count == 0)
            {
                ChapterListPanel.Children.Add(new TextBlock
                {
                    Text = "暂无可用章节。",
                    Foreground = Brushes.Gray,
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 30, 0, 0)
                });
                return;
            }

            foreach (Chapter chapter in chapters)
            {
                bool unlocked = _gameController.ChapterProgress.IsUnlocked(chapter.Id);
                bool completed = _gameController.ChapterProgress.IsCompleted(chapter.Id);
                int chapterNumber = GetChapterNumber(chapter.Id);
                string numberText = chapterNumber > 0 ? $"第{chapterNumber}章" : chapter.Id;
                StackPanel info = new() { Margin = new Thickness(16, 10, 16, 10) };
                info.Children.Add(new TextBlock
                {
                    Text = numberText,
                    Foreground = unlocked ? Brushes.White : Brushes.Gray,
                    FontSize = 20,
                    FontWeight = FontWeights.Bold
                });
                info.Children.Add(new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(chapter.Title) ? chapter.Id : chapter.Title,
                    Foreground = unlocked ? new SolidColorBrush(Color.FromRgb(233, 197, 141)) : Brushes.Gray,
                    FontSize = 16,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                info.Children.Add(new TextBlock
                {
                    Text = unlocked ? (completed ? "✓ 已完成" : "未完成") : "锁定  未解锁",
                    Foreground = unlocked ? new SolidColorBrush(Color.FromRgb(168, 233, 197)) : Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(0, 6, 0, 0)
                });

                Border border = new()
                {
                    Background = unlocked ? new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = unlocked ? new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(35, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Child = new Button
                    {
                        Content = info,
                        Background = Brushes.Transparent,
                        BorderThickness = new Thickness(0),
                        HorizontalContentAlignment = HorizontalAlignment.Stretch,
                        Padding = new Thickness(0),
                        Cursor = unlocked ? Cursors.Hand : Cursors.Arrow,
                        IsEnabled = unlocked
                    }
                };

                if (border.Child is Button button)
                {
                    string chapterId = chapter.Id;
                    button.Click += (_, _) => LoadSelectedChapter(chapterId);
                }
                ChapterListPanel.Children.Add(border);
            }
        }

        private void LoadSelectedChapter(string chapterId)
        {
            if (!_gameController.ChapterProgress.IsUnlocked(chapterId))
            {
                return;
            }

            OverlayContainer.Visibility = Visibility.Collapsed;
            _gameController.ResetScene();
            _sceneController.StopAudio();
            LoadStoryChapter(chapterId);
        }

        private void ExitGameButton_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOverlaySource = OverlaySource.InGameMenu;
            OverlayContainer.Visibility = Visibility.Visible;
            InGameMenuPanel.Visibility = Visibility.Visible;
            SaveLoadPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Collapsed;
        }

        private async void InGameSaveButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;
            await _saveController.RefreshSlotListAsync(true);
        }

        private async void InGameLoadButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SaveLoadPanel.Visibility = Visibility.Visible;
            await _saveController.RefreshSlotListAsync(false);
        }

        private void InGameSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            InGameMenuPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
        }

        private async void ReturnTitleButton_Click(object sender, RoutedEventArgs e)
        {
            ReturnToMainMenu();
            await UpdateContinueButtonStateAsync();
        }

        private void CloseMenuButton_Click(object sender, RoutedEventArgs e)
        {
            OverlayContainer.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Collapsed;
        }

        private void BackFromChapterSelect_Click(object sender, RoutedEventArgs e)
        {
            OverlayContainer.Visibility = Visibility.Collapsed;
            ChapterSelectPanel.Visibility = Visibility.Collapsed;
        }

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

        private void BgmVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (BgmPlayer != null) BgmPlayer.Volume = BgmVolumeSlider.Value;
            if (BgmVolumeText != null) BgmVolumeText.Text = $"{(int)(BgmVolumeSlider.Value * 100)}%";
        }

        private void SeVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SePlayer != null) SePlayer.Volume = SeVolumeSlider.Value;
            if (SeVolumeText != null) SeVolumeText.Text = $"{(int)(SeVolumeSlider.Value * 100)}%";
        }
    }
}
