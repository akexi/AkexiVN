using AkexiVN.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AkexiVN.Services
{
    public class SaveService
    {
        private readonly string _saveDirectory;

        public SaveService()
        {
            _saveDirectory = Path.Combine(
                AppContext.BaseDirectory,
                "Save");

            Directory.CreateDirectory(_saveDirectory);
        }

        public async Task SaveAsync(
            int slot,
            SaveData data)
        {
            string path = GetSavePath(slot);

            string json = JsonSerializer.Serialize(
                data,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await File.WriteAllTextAsync(
                path,
                json);
        }

        public async Task<SaveData?> LoadAsync(
            int slot)
        {
            string path = GetSavePath(slot);

            if (!File.Exists(path))
            {
                return null;
            }

            string json =
                await File.ReadAllTextAsync(path);

            return JsonSerializer.Deserialize<SaveData>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public bool Exists(int slot)
        {
            return File.Exists(
                GetSavePath(slot));
        }

        private string GetSavePath(int slot)
        {
            return Path.Combine(
                _saveDirectory,
                $"save_{slot}.json");
        }
    }
}
