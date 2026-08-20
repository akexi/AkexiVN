using AkexiVN.Models;
using AkexiVN.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;

namespace AkexiVN.Controllers
{
    public class SaveController
    {
        private readonly SaveService _saveService;
        private readonly GameController _gameController;
        private readonly StackPanel _slotList;
        private readonly TextBlock _title;
        private readonly TextBlock _status;
        private readonly Func<string> _currentText;
        private readonly Func<int, Task> _loadGame;
        private readonly Func<Task>? _onSaveCompleted;

        public SaveController(SaveService saveService, GameController gameController, StackPanel slotList,
            TextBlock title, TextBlock status, Func<string> currentText, Func<int, Task> loadGame, Func<Task>? onSaveCompleted = null)
        {
            _saveService = saveService;
            _gameController = gameController;
            _slotList = slotList;
            _title = title;
            _status = status;
            _currentText = currentText;
            _loadGame = loadGame;
            _onSaveCompleted = onSaveCompleted;
        }

        public async Task<int> GetLatestSaveSlotAsync()
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

        public async Task SaveAsync(int slot)
        {
            await _saveService.SaveAsync(slot, _gameController.CreateSaveData(_currentText()));
        }

        public async Task RefreshSlotListAsync(bool isSaveMode)
        {
            _title.Text = isSaveMode ? "保存游戏" : "读取游戏";
            _status.Text = isSaveMode ? "选择一个存档槽位进行保存。" : "选择一个存档槽位进行读取。";
            _slotList.Children.Clear();

            for (int slot = 1; slot <= SaveService.MaxSlots; slot++)
            {
                SaveData? data = await _saveService.LoadAsync(slot);
                int targetSlot = slot;
                Border border = new()
                {
                    Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(0, 0, 0, 10),
                    Padding = new Thickness(12)
                };
                StackPanel info = new();
                info.Children.Add(new TextBlock { Text = $"存档 {slot} - {(data != null ? data.SaveTime.ToString("yyyy-MM-dd HH:mm") : "暂无存档")}", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold });
                info.Children.Add(new TextBlock
                {
                    Text = data == null ? "暂无存档" : (string.IsNullOrWhiteSpace(data.CurrentText) ? "剧情：无" : data.CurrentText),
                    Foreground = data == null ? Brushes.Gray : Brushes.GhostWhite,
                    FontSize = data == null ? 14 : 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                if (data != null)
                {
                    info.Children.Insert(1, new TextBlock { Text = string.IsNullOrWhiteSpace(data.Background) ? "场景：未知" : $"场景：{data.Background}", Foreground = Brushes.LightGray, FontSize = 14, Margin = new Thickness(0, 4, 0, 0) });
                }

                Button button = new()
                {
                    Content = info,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0),
                    Cursor = Cursors.Hand,
                    IsEnabled = isSaveMode || data != null
                };
                button.Click += async (_, _) =>
                {
                    if (isSaveMode)
                    {
                        await SaveAsync(targetSlot);
                        await RefreshSlotListAsync(true);
                        if (_onSaveCompleted != null) await _onSaveCompleted();
                    }
                    else if (data != null)
                    {
                        await _loadGame(targetSlot);
                    }
                };
                border.Child = button;
                _slotList.Children.Add(border);
            }
        }
    }
}
