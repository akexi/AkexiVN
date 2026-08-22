using AkexiVN.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AkexiVN.Services
{
    public class ChapterManager
    {
        private readonly Dictionary<string, Chapter> _chapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StoryChapter> _chapterData = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _chaptersConfigPath;

        public ChapterManager()
        {
            _chaptersConfigPath = Path.Combine(AppContext.BaseDirectory, "Data", "Chapters", "chapters.json");
        }

        public async Task LoadAsync()
        {
            _chapters.Clear();
            _chapterData.Clear();

            if (!File.Exists(_chaptersConfigPath))
            {
                throw new FileNotFoundException("未找到章节配置文件：Data/Chapters/chapters.json");
            }

            string json = await File.ReadAllTextAsync(_chaptersConfigPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidOperationException("章节配置文件为空。");
            }

            ChapterCollection? collection;
            try
            {
                collection = JsonSerializer.Deserialize<ChapterCollection>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"章节配置文件格式错误：{ex.Message}", ex);
            }

            if (collection == null || collection.Chapters == null)
            {
                throw new InvalidOperationException("章节配置文件解析失败。");
            }

            foreach (Chapter chapter in collection.Chapters)
            {
                if (string.IsNullOrWhiteSpace(chapter.Id))
                {
                    continue;
                }

                _chapters[chapter.Id] = chapter;
            }

            foreach (Chapter chapter in _chapters.Values)
            {
                if (string.IsNullOrWhiteSpace(chapter.File))
                {
                    continue;
                }

                string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Chapters", chapter.File);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    string chapterJson = await File.ReadAllTextAsync(filePath);
                    StoryChapter? data = JsonSerializer.Deserialize<StoryChapter>(chapterJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (data != null)
                    {
                        _chapterData[chapter.Id] = data;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"无法加载章节文件：{chapter.File} ({ex.Message})", ex);
                }
            }
        }

        public bool HasChapter(string chapterId)
        {
            return !string.IsNullOrWhiteSpace(chapterId) && _chapters.ContainsKey(chapterId);
        }

        public IReadOnlyList<Chapter> GetChapters()
        {
            return _chapters.Values.ToList();
        }

        public bool IsUnlocked(string chapterId)
        {
            if (!HasChapter(chapterId))
            {
                return false;
            }

            return _chapters[chapterId].Unlock;
        }

        public Chapter? GetChapter(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return null;
            }

            return _chapters.TryGetValue(chapterId, out Chapter? chapter) ? chapter : null;
        }

        public StoryChapter? GetStoryChapter(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                return null;
            }

            return _chapterData.TryGetValue(chapterId, out StoryChapter? chapter) ? chapter : null;
        }

        public string? GetNextChapterId(string chapterId)
        {
            Chapter? chapter = GetChapter(chapterId);
            if (chapter == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(chapter.NextChapter))
            {
                return null;
            }

            return HasChapter(chapter.NextChapter) ? chapter.NextChapter : null;
        }

        public string GetChapterDisplayName(string chapterId)
        {
            Chapter? chapter = GetChapter(chapterId);
            if (chapter != null && !string.IsNullOrWhiteSpace(chapter.Title))
            {
                return chapter.Title;
            }

            StoryChapter? storyChapter = GetStoryChapter(chapterId);
            if (storyChapter != null && !string.IsNullOrWhiteSpace(storyChapter.Title))
            {
                return storyChapter.Title;
            }

            return chapterId;
        }

        public string GetChapterTitleById(string chapterId)
        {
            return GetChapterDisplayName(chapterId);
        }

        public Chapter? GetCurrentChapter(string currentChapterId)
        {
            if (string.IsNullOrWhiteSpace(currentChapterId))
            {
                return null;
            }

            return GetChapter(currentChapterId);
        }

        public StoryChapter? LoadChapter(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                throw new ArgumentException("章节 ID 不能为空。");
            }

            if (!HasChapter(chapterId))
            {
                throw new InvalidOperationException($"章节 ID 不存在：{chapterId}");
            }

            Chapter? chapterMeta = GetChapter(chapterId);
            if (chapterMeta == null || string.IsNullOrWhiteSpace(chapterMeta.File))
            {
                throw new InvalidOperationException($"章节 {chapterId} 缺少文件配置。");
            }

            string filePath = Path.Combine(AppContext.BaseDirectory, "Data", "Chapters", chapterMeta.File);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"无法加载章节：{chapterId}");
            }

            try
            {
                string json = File.ReadAllText(filePath);
                StoryChapter? chapter = JsonSerializer.Deserialize<StoryChapter>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (chapter == null)
                {
                    throw new InvalidOperationException($"章节文件内容为空或无法解析：{chapterMeta.File}");
                }

                if (string.IsNullOrWhiteSpace(chapter.Chapter))
                {
                    chapter.Chapter = chapterId;
                }

                if (string.IsNullOrWhiteSpace(chapter.Start) && chapter.Nodes.Count > 0)
                {
                    chapter.Start = chapter.Nodes.First().Id;
                }

                _chapterData[chapterId] = chapter;
                return chapter;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException || ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"章节 JSON 格式错误：{chapterMeta.File} ({ex.Message})", ex);
            }
        }

        public string GetStartNodeId(string chapterId)
        {
            StoryChapter? chapter = GetStoryChapter(chapterId);
            if (chapter == null)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(chapter.Start)
                ? (chapter.Nodes.FirstOrDefault()?.Id ?? string.Empty)
                : chapter.Start;
        }
    }
}
