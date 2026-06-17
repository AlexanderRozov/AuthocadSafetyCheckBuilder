using Demo.Models;
using System;
using System.Collections.Generic;

namespace Demo.Services
{
    public static class PtObjectRepository
    {
        private static readonly List<PtObject> Objects = new List<PtObject>();
        private static readonly Dictionary<string, int> NumberCounters = new Dictionary<string, int>();

        public static IReadOnlyList<PtObject> All => Objects;

        public static string GetSuggestedNumber(string code)
        {
            if (!NumberCounters.ContainsKey(code))
                return "001";

            return NumberCounters[code].ToString("D3");
        }

        public static string GetNextNumber(string code)
        {
            if (!NumberCounters.ContainsKey(code))
                NumberCounters[code] = 1;

            var number = NumberCounters[code];
            NumberCounters[code] = number + 1;
            return number.ToString("D3");
        }

        public static void ReserveNumber(string code, string number)
        {
            if (!int.TryParse(number, out var parsed))
                return;

            if (!NumberCounters.ContainsKey(code) || NumberCounters[code] <= parsed)
                NumberCounters[code] = parsed + 1;
        }

        public static void Add(PtObject ptObject)
        {
            Objects.Add(ptObject);
            ReserveNumber(ptObject.Code, ptObject.Number);
        }

        public static int DeviceCount => Objects.Count;
    }
}
