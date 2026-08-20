using AkexiVN.Models;
using AkexiVN.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AkexiVN.Controllers
{
    public class SceneController
    {
        private const double DesignWidth = 1280;
        private const double CharacterBaseWidth = 500;
        private const double CharacterBaseHeight = 820;
        private const double CharacterViewportHeight = 480;

        private readonly StoryService _storyService;
        private readonly SceneState _sceneState;
        private readonly Image _backgroundImage;
        private readonly Canvas _characterLayer;
        private readonly MediaElement _bgmPlayer;
        private readonly MediaElement _sePlayer;

        public SceneController(StoryService storyService, SceneState sceneState, Image backgroundImage,
            Canvas characterLayer, MediaElement bgmPlayer, MediaElement sePlayer)
        {
            _storyService = storyService;
            _sceneState = sceneState;
            _backgroundImage = backgroundImage;
            _characterLayer = characterLayer;
            _bgmPlayer = bgmPlayer;
            _sePlayer = sePlayer;
            _bgmPlayer.MediaEnded += BgmPlayer_MediaEnded;
        }

        public void StopAudio()
        {
            _bgmPlayer.Stop();
            _sePlayer.Stop();
        }

        public void UpdateScene(StoryNode node)
        {
            if (!string.IsNullOrWhiteSpace(node.Background))
            {
                _sceneState.Background = node.Background;
                SetBackground(node.Background);
            }

            UpdateBgm(node.Bgm);
            if (!string.IsNullOrWhiteSpace(node.Se))
            {
                PlaySoundEffect(node.Se);
            }

            foreach (SceneCharacter character in node.Characters)
            {
                if (string.Equals(character.Effect, "hide", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (string key in _sceneState.Characters
                        .Where(pair => pair.Value.Id == character.Id)
                        .Select(pair => pair.Key).ToList())
                    {
                        _sceneState.Characters.Remove(key);
                    }
                }
                else
                {
                    foreach (string key in _sceneState.Characters
                        .Where(pair => pair.Value.Id == character.Id && pair.Key != character.Position)
                        .Select(pair => pair.Key).ToList())
                    {
                        _sceneState.Characters.Remove(key);
                    }

                    _sceneState.Characters[character.Position] = character;
                }
            }

            RenderCharacters();
        }

        public void RestoreVisuals()
        {
            if (!string.IsNullOrWhiteSpace(_sceneState.Background))
            {
                SetBackground(_sceneState.Background);
            }

            RenderCharacters();
            if (!string.IsNullOrWhiteSpace(_sceneState.Bgm))
            {
                PlayBgm(_sceneState.Bgm);
            }
            else
            {
                _bgmPlayer.Stop();
            }
        }

        private void SetBackground(string fileName)
        {
            string path = $"pack://application:,,,/Assets/Backgrounds/{fileName}";
            try
            {
                _backgroundImage.Source = new BitmapImage(new Uri(path));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Background image not found: {path} ({ex.Message})");
            }
        }

        private void RenderCharacters()
        {
            _characterLayer.Children.Clear();
            foreach (SceneCharacter character in _sceneState.Characters.Values)
            {
                _characterLayer.Children.Add(CreateCharacterImage(character));
            }
        }

        private Image CreateCharacterImage(SceneCharacter character)
        {
            ApplyCharacterDefaults(character);
            string id = !string.IsNullOrWhiteSpace(character.Id) ? character.Id : character.Name;
            BitmapImage? bitmap = null;
            if (!string.IsNullOrWhiteSpace(character.Expression))
            {
                string expression = character.Expression.Replace("\\", "/");
                string candidate = expression.Contains('/') || string.IsNullOrWhiteSpace(id)
                    ? expression
                    : $"{id}/{expression}";
                TryLoadBitmapFromPack($"pack://application:,,,/Assets/Characters/{candidate}", out bitmap);
            }

            Image image = new()
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 1)
            };
            ApplyCharacterLayout(image, character);
            if (string.Equals(character.Effect, "fade", StringComparison.OrdinalIgnoreCase))
            {
                DoubleAnimation animation = new() { From = 0, To = 1, Duration = TimeSpan.FromMilliseconds(300) };
                image.BeginAnimation(UIElement.OpacityProperty, animation);
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

            if (character.Scale <= 0) character.Scale = profile.DefaultScale > 0 ? profile.DefaultScale : 1;
            if (character.OffsetX == 0 && profile.DefaultOffsetX != 0) character.OffsetX = profile.DefaultOffsetX;
            if (character.OffsetY == 0 && profile.DefaultOffsetY != 0) character.OffsetY = profile.DefaultOffsetY;
        }

        private static void ApplyCharacterLayout(Image image, SceneCharacter character)
        {
            double scale = character.Scale <= 0 ? 1 : character.Scale;
            double width = CharacterBaseWidth * scale;
            double height = CharacterBaseHeight * scale;
            double viewportHeight = CharacterViewportHeight * scale;
            image.Width = width;
            image.Height = height;
            image.Clip = new RectangleGeometry(new Rect(0, 0, width, viewportHeight));
            Canvas.SetLeft(image, GetCharacterAnchorX(character.Position) - width / 2 + character.OffsetX);
            Canvas.SetBottom(image, character.OffsetY);
        }

        private static double GetCharacterAnchorX(string position) => position?.Trim().ToLowerInvariant() switch
        {
            "left" => DesignWidth * 0.25,
            "right" => DesignWidth * 0.75,
            _ => DesignWidth * 0.5
        };

        private static bool TryLoadBitmapFromPack(string packUri, out BitmapImage? bitmap)
        {
            try
            {
                bitmap = new BitmapImage(new Uri(packUri, UriKind.Absolute));
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Character image not found: {packUri} ({ex.Message})");
                bitmap = null;
                return false;
            }
        }

        private void UpdateBgm(string bgmFileName)
        {
            if (string.IsNullOrWhiteSpace(bgmFileName) || string.Equals(_sceneState.Bgm, bgmFileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _sceneState.Bgm = bgmFileName;
            PlayBgm(bgmFileName);
        }

        private void PlayBgm(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "BGM", fileName);
            if (!File.Exists(path)) return;
            _bgmPlayer.Stop();
            _bgmPlayer.Source = new Uri(path);
            _bgmPlayer.Position = TimeSpan.Zero;
            _bgmPlayer.Play();
        }

        private void BgmPlayer_MediaEnded(object? sender, RoutedEventArgs e)
        {
            _bgmPlayer.Position = TimeSpan.Zero;
            _bgmPlayer.Play();
        }

        private void PlaySoundEffect(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio", "SE", fileName);
            if (!File.Exists(path)) return;
            _sePlayer.Stop();
            _sePlayer.Source = new Uri(path);
            _sePlayer.Position = TimeSpan.Zero;
            _sePlayer.Play();
        }
    }
}
