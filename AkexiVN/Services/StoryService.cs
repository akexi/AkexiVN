using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AkexiVN.Models;

namespace AkexiVN.Services
{
    public class StoryService
    {
        private readonly Dictionary<string, StoryChapter> _chapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, StoryNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
        private string _currentChapterId = string.Empty;

        public async Task LoadAsync()
        {
            _chapters.Clear();
            _nodes.Clear();
            _currentChapterId = string.Empty;

            string chaptersDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "Chapters");

            if (Directory.Exists(chaptersDirectory))
            {
                string[] chapterFiles = Directory.GetFiles(
                    chaptersDirectory,
                    "*.json",
                    SearchOption.TopDirectoryOnly);

                foreach (string chapterFile in chapterFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
                {
                    StoryChapter? chapter = await LoadChapterFromFileAsync(chapterFile);
                    if (chapter == null)
                    {
                        continue;
                    }

                    RegisterChapter(chapter);
                    if (string.IsNullOrWhiteSpace(_currentChapterId))
                    {
                        _currentChapterId = chapter.Chapter;
                    }
                }
            }

            if (_chapters.Count == 0)
            {
                await LoadLegacyStoryAsync();
            }

            if (string.IsNullOrWhiteSpace(_currentChapterId) && _chapters.Count > 0)
            {
                _currentChapterId = _chapters.Keys.First();
            }
        }

        public void SetCurrentChapter(string chapterId)
        {
            if (!string.IsNullOrWhiteSpace(chapterId) && _chapters.ContainsKey(chapterId))
            {
                _currentChapterId = chapterId;
            }
        }

        public string GetCurrentChapterId()
        {
            return _currentChapterId;
        }

        public StoryChapter GetChapter(string chapterId)
        {
            if (_chapters.TryGetValue(chapterId, out StoryChapter? chapter))
            {
                return chapter;
            }

            throw new Exception($"找不到章节：{chapterId}");
        }

        public StoryNode GetStartNode(string chapterId)
        {
            StoryChapter chapter = GetChapter(chapterId);
            string startId = string.IsNullOrWhiteSpace(chapter.Start)
                ? chapter.Nodes.FirstOrDefault()?.Id ?? string.Empty
                : chapter.Start;

            if (string.IsNullOrWhiteSpace(startId))
            {
                throw new Exception($"章节 {chapterId} 没有起始节点。");
            }

            return GetNodeFromChapter(chapterId, startId);
        }

        public StoryNode GetNode(string id)
        {
            string nodeId = id?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new Exception("剧情节点 ID 不能为空。");
            }

            if (string.Equals(nodeId, "start", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(_currentChapterId))
                {
                    return GetStartNode(_chapters.Keys.First());
                }

                return GetStartNode(_currentChapterId);
            }

            if (!string.IsNullOrWhiteSpace(_currentChapterId))
            {
                StoryNode? nodeInCurrentChapter = TryGetNodeByChapter(_currentChapterId, nodeId);
                if (nodeInCurrentChapter != null)
                {
                    _currentChapterId = nodeInCurrentChapter.ChapterId;
                    return nodeInCurrentChapter;
                }
            }

            if (_nodes.TryGetValue(nodeId, out StoryNode? node))
            {
                if (!string.IsNullOrWhiteSpace(node.ChapterId))
                {
                    _currentChapterId = node.ChapterId;
                }

                return node;
            }

            foreach (var chapter in _chapters.Values)
            {
                StoryNode? chapterNode = chapter.Nodes.FirstOrDefault(n =>
                    string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));

                if (chapterNode != null)
                {
                    _currentChapterId = chapter.Chapter;
                    return chapterNode;
                }
            }

            throw new Exception($"找不到剧情节点：{id}");
        }

        public StoryNode GetNodeFromChapter(string chapterId, string nodeId)
        {
            StoryNode? node = TryGetNodeByChapter(chapterId, nodeId);
            if (node != null)
            {
                _currentChapterId = chapterId;
                return node;
            }

            throw new Exception($"在章节 {chapterId} 中找不到节点：{nodeId}");
        }

        private StoryNode? TryGetNodeByChapter(string chapterId, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(chapterId) || string.IsNullOrWhiteSpace(nodeId))
            {
                return null;
            }

            string lookupKey = $"{chapterId}:{nodeId}";
            if (_nodes.TryGetValue(lookupKey, out StoryNode? node))
            {
                return node;
            }

            if (_chapters.TryGetValue(chapterId, out StoryChapter? chapter))
            {
                return chapter.Nodes.FirstOrDefault(n =>
                    string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private void RegisterChapter(StoryChapter chapter)
        {
            if (string.IsNullOrWhiteSpace(chapter.Chapter))
            {
                return;
            }

            _chapters[chapter.Chapter] = chapter;

            foreach (StoryNode node in chapter.Nodes)
            {
                node.ChapterId = chapter.Chapter;
                string key = $"{chapter.Chapter}:{node.Id}";
                _nodes[key] = node;

                if (!_nodes.ContainsKey(node.Id))
                {
                    _nodes[node.Id] = node;
                }
            }
        }

        private async Task<StoryChapter?> LoadChapterFromFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            StoryChapter? chapter = JsonSerializer.Deserialize<StoryChapter>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (chapter == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(chapter.Chapter))
            {
                chapter.Chapter = Path.GetFileNameWithoutExtension(path);
            }

            if (string.IsNullOrWhiteSpace(chapter.Start) && chapter.Nodes.Count > 0)
            {
                chapter.Start = chapter.Nodes.First().Id;
            }

            return chapter;
        }

        private async Task LoadLegacyStoryAsync()
        {
            string legacyPath = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "story.json");

            if (!File.Exists(legacyPath))
            {
                throw new FileNotFoundException("找不到剧情文件：", legacyPath);
            }

            string json = await File.ReadAllTextAsync(legacyPath);
            StoryData? legacyData = JsonSerializer.Deserialize<StoryData>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (legacyData == null || legacyData.Nodes.Count == 0)
            {
                throw new Exception("剧情文件解析失败。");
            }

            string defaultChapterId = "chapter01";
            string startNodeId = legacyData.Nodes.FirstOrDefault(node => string.Equals(node.Id, "start", StringComparison.OrdinalIgnoreCase))?.Id
                ?? legacyData.Nodes.First().Id;

            StoryChapter legacyChapter = new()
            {
                Chapter = defaultChapterId,
                Title = "Legacy Story",
                Start = startNodeId,
                Nodes = legacyData.Nodes
            };

            foreach (StoryNode node in legacyChapter.Nodes)
            {
                node.ChapterId = legacyChapter.Chapter;
            }

            RegisterChapter(legacyChapter);
            _currentChapterId = legacyChapter.Chapter;
        }
    }
}
