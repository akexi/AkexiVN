using AkexiVN.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace AkexiVN.Services
{
    public class SaveService
    {
        public const int MaxSlots = 5;

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
            if (slot < 1 || slot > MaxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

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
            if (slot < 1 || slot > MaxSlots)
            {
                return null;
            }

            string path = GetSavePath(slot);

            if (!File.Exists(path))
            {
                return null;
            }

            string json =
                await File.ReadAllTextAsync(path);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SaveData>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        public bool Exists(int slot)
        {
            if (slot < 1 || slot > MaxSlots)
            {
                return false;
            }

            return File.Exists(
                GetSavePath(slot));
        }

        public string GetSavePath(int slot)
        {
            if (slot < 1 || slot > MaxSlots)
            {
                throw new ArgumentOutOfRangeException(nameof(slot));
            }

            return Path.Combine(
                _saveDirectory,
                $"save_{slot}.json");
        }
    }
}
