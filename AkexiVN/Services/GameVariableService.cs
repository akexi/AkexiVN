using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace AkexiVN.Services
{
    public class GameVariableService
    {
        private readonly Dictionary<string, object> _variables = new(StringComparer.OrdinalIgnoreCase);

        public void Set(string name, object value)
        {
            ValidateName(name);
            _variables[name] = NormalizeValue(value);
        }

        public object? Get(string name) => _variables.TryGetValue(name, out object? value) ? value : null;

        public T? Get<T>(string name)
        {
            object? value = Get(name);
            if (value == null) return default;
            if (value is T typedValue) return typedValue;
            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }

        public bool Contains(string name) => _variables.ContainsKey(name);

        public void Add(string name, object amount) => ChangeNumber(name, amount, 1);

        public void Remove(string name, object amount) => ChangeNumber(name, amount, -1);

        public void Clear() => _variables.Clear();

        public Dictionary<string, object> GetAll() => _variables.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

        public void Restore(IReadOnlyDictionary<string, object>? variables)
        {
            Clear();
            if (variables == null) return;

            foreach (KeyValuePair<string, object> variable in variables)
            {
                Set(variable.Key, variable.Value);
            }
        }

        public void ApplyEffects(IReadOnlyDictionary<string, object>? effects)
        {
            if (effects == null) return;

            foreach (KeyValuePair<string, object> effect in effects)
            {
                object value = NormalizeValue(effect.Value);
                if (IsNumber(value)) Add(effect.Key, value);
                else Set(effect.Key, value);
            }
        }

        private void ChangeNumber(string name, object amount, int direction)
        {
            ValidateName(name);
            object normalizedAmount = NormalizeValue(amount);
            if (!IsNumber(normalizedAmount))
            {
                throw new ArgumentException("变量增减值必须是数字。", nameof(amount));
            }

            object? current = Get(name);
            double result = (current == null ? 0 : ToDouble(current)) + direction * ToDouble(normalizedAmount);
            bool keepInteger = (current == null || IsInteger(current)) && IsInteger(normalizedAmount)
                && result >= int.MinValue && result <= int.MaxValue;
            Set(name, keepInteger ? (object)(int)result : result);
        }

        private static object NormalizeValue(object value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number when element.TryGetInt32(out int integer) => integer,
                    JsonValueKind.Number when element.TryGetDouble(out double number) => number,
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => element.GetString() ?? string.Empty,
                    _ => throw new ArgumentException("游戏变量只支持 int、double、bool 和 string。", nameof(value))
                };
            }

            if (value is int or double or bool or string) return value;
            throw new ArgumentException("游戏变量只支持 int、double、bool 和 string。", nameof(value));
        }

        private static bool IsNumber(object value) => value is int or double or float or decimal or long;

        private static bool IsInteger(object value) => value is int or long;

        private static double ToDouble(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("变量名不能为空。", nameof(name));
        }
    }
}