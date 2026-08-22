using AkexiVN.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AkexiVN.Services
{
    public class ChapterProgressService
    {
        private readonly HashSet<string> _unlockedChapters = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _completedChapters = new(StringComparer.OrdinalIgnoreCase);

        public void Initialize(ChapterManager chapterManager, string startChapterId)
        {
            _unlockedChapters.Clear();
            _completedChapters.Clear();

            foreach (Chapter chapter in chapterManager.GetChapters())
            {
                if (chapter.Unlock)
                {
                    _unlockedChapters.Add(chapter.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(startChapterId) && chapterManager.HasChapter(startChapterId))
            {
                _unlockedChapters.Add(startChapterId);
            }

            if (_unlockedChapters.Count == 0)
            {
                string? firstChapterId = chapterManager.GetChapters().FirstOrDefault()?.Id;
                if (!string.IsNullOrWhiteSpace(firstChapterId))
                {
                    _unlockedChapters.Add(firstChapterId);
                }
            }
        }

        public void Restore(ChapterManager chapterManager, IEnumerable<string>? unlockedChapters,
            IEnumerable<string>? completedChapters, string currentChapterId)
        {
            HashSet<string> initialUnlocked = new(_unlockedChapters, StringComparer.OrdinalIgnoreCase);
            HashSet<string> initialCompleted = new(StringComparer.OrdinalIgnoreCase);
            _unlockedChapters.Clear();
            _completedChapters.Clear();

            AddExistingChapters(chapterManager, _unlockedChapters, unlockedChapters);
            AddExistingChapters(chapterManager, _completedChapters, completedChapters);

            if (_unlockedChapters.Count == 0)
            {
                _unlockedChapters.UnionWith(initialUnlocked);
            }

            _completedChapters.RemoveWhere(chapterId => !_unlockedChapters.Contains(chapterId));
            if (!string.IsNullOrWhiteSpace(currentChapterId) && chapterManager.HasChapter(currentChapterId))
            {
                _unlockedChapters.Add(currentChapterId);
            }

            if (_unlockedChapters.Count == 0)
            {
                _unlockedChapters.UnionWith(initialCompleted);
            }
        }

        public bool IsUnlocked(string chapterId) => _unlockedChapters.Contains(chapterId);

        public bool IsCompleted(string chapterId) => _completedChapters.Contains(chapterId);

        public void CompleteChapter(ChapterManager chapterManager, string chapterId)
        {
            if (!chapterManager.HasChapter(chapterId))
            {
                return;
            }

            _unlockedChapters.Add(chapterId);
            _completedChapters.Add(chapterId);
            string? nextChapterId = chapterManager.GetNextChapterId(chapterId);
            if (!string.IsNullOrWhiteSpace(nextChapterId))
            {
                _unlockedChapters.Add(nextChapterId);
            }
        }

        public List<string> GetUnlockedChapters() => _unlockedChapters.ToList();

        public List<string> GetCompletedChapters() => _completedChapters.ToList();

        private static void AddExistingChapters(ChapterManager chapterManager, HashSet<string> target,
            IEnumerable<string>? chapterIds)
        {
            if (chapterIds == null)
            {
                return;
            }

            foreach (string chapterId in chapterIds)
            {
                if (chapterManager.HasChapter(chapterId))
                {
                    target.Add(chapterId);
                }
            }
        }
    }
}
