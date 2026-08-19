using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using AkexiVN.Models;
using System.IO;

namespace AkexiVN.Services
{
    public class StoryService
    {
        private readonly Dictionary<string, StoryNode> _nodes = new();

        public async Task LoadAsync()
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                "story.json");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "找不到剧情文件：",
                    path);
            }

            string json = await File.ReadAllTextAsync(path);

            StoryData? data = JsonSerializer.Deserialize<StoryData>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data == null)
            {
                throw new Exception("剧情文件解析失败。");
            }

            _nodes.Clear();

            foreach (StoryNode node in data.Nodes)
            {
                _nodes[node.Id] = node;
            }
        }

        public StoryNode GetNode(string id)
        {
            if (!_nodes.TryGetValue(id, out StoryNode? node))
            {
                throw new Exception($"找不到剧情节点：{id}");
            }

            return node;
        }
    }
}
