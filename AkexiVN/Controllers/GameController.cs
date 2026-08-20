using AkexiVN.Models;
using AkexiVN.Services;
using System;
using System.Collections.Generic;

namespace AkexiVN.Controllers
{
    public enum GameFlowState
    {
        MainMenu,
        Playing,
        ChapterEnd
    }

    public class GameController
    {
        public StoryService StoryService { get; } = new();
        public ChapterManager ChapterManager { get; } = new();
        public SceneState SceneState { get; } = new();
        public StoryNode? CurrentNode { get; private set; }
        public GameFlowState FlowState { get; private set; } = GameFlowState.MainMenu;

        public void ResetScene()
        {
            SceneState.Background = string.Empty;
            SceneState.Bgm = string.Empty;
            SceneState.Characters.Clear();
        }

        public string GetStartChapterId()
        {
            string chapterId = StoryService.GetGameConfig().StartChapter;
            return !string.IsNullOrWhiteSpace(chapterId) && ChapterManager.HasChapter(chapterId)
                ? chapterId
                : "chapter00";
        }

        public StoryNode LoadChapter(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId))
            {
                chapterId = StoryService.GetCurrentChapterId();
            }

            if (string.IsNullOrWhiteSpace(chapterId) || !ChapterManager.HasChapter(chapterId))
            {
                throw new InvalidOperationException($"章节 ID 不存在：{chapterId}");
            }

            StoryChapter chapter = ChapterManager.LoadChapter(chapterId)
                ?? throw new InvalidOperationException($"无法加载章节：{chapterId}");

            StoryService.SetCurrentChapter(chapterId);
            FlowState = GameFlowState.Playing;
            string startNodeId = string.IsNullOrWhiteSpace(chapter.Start) ? "start" : chapter.Start;
            return ShowNode(startNodeId);
        }

        public StoryNode ShowNode(string id)
        {
            CurrentNode = StoryService.GetNode(id);
            FlowState = GameFlowState.Playing;
            return CurrentNode;
        }

        public void MarkChapterEnd()
        {
            FlowState = GameFlowState.ChapterEnd;
        }

        public SaveData CreateSaveData(string currentText)
        {
            if (CurrentNode == null)
            {
                throw new InvalidOperationException("当前没有可保存的剧情节点。");
            }

            return new SaveData
            {
                CurrentChapterId = StoryService.GetCurrentChapterId(),
                CurrentNodeId = CurrentNode.Id,
                GameState = FlowState.ToString(),
                Background = SceneState.Background,
                Bgm = SceneState.Bgm,
                CurrentCharacterName = CurrentNode.Character,
                CurrentText = string.IsNullOrWhiteSpace(currentText) ? CurrentNode.Text : currentText,
                Characters = new Dictionary<string, SceneCharacter>(SceneState.Characters),
                SaveTime = DateTime.Now
            };
        }

        public StoryNode RestoreSaveData(SaveData data)
        {
            string chapterId = !string.IsNullOrWhiteSpace(data.CurrentChapterId)
                ? data.CurrentChapterId
                : StoryService.GetCurrentChapterId();

            if (!string.IsNullOrWhiteSpace(chapterId))
            {
                StoryService.SetCurrentChapter(chapterId);
            }

            try
            {
                CurrentNode = StoryService.GetNode(data.CurrentNodeId);
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(chapterId))
                {
                    throw new InvalidOperationException("这个存档对应的剧情节点已不存在。");
                }

                CurrentNode = StoryService.GetStartNode(chapterId);
            }

            SceneState.Background = data.Background;
            SceneState.Bgm = data.Bgm;
            SceneState.Characters = new Dictionary<string, SceneCharacter>(
                data.Characters ?? new Dictionary<string, SceneCharacter>());
            FlowState = string.Equals(data.GameState, GameFlowState.ChapterEnd.ToString(), StringComparison.OrdinalIgnoreCase)
                ? GameFlowState.ChapterEnd
                : GameFlowState.Playing;
            return CurrentNode;
        }
    }
}
