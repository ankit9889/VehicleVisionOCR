using System;

public static class FuzzyMatcher
{
    public static int ComputeLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            if (string.IsNullOrEmpty(target)) return 0;
            return target.Length;
        }

        if (string.IsNullOrEmpty(target)) return source.Length;

        int n = source.Length;
        int m = target.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 1; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                int min1 = d[i - 1, j] + 1;
                int min2 = d[i, j - 1] + 1;
                int min3 = d[i - 1, j - 1] + cost;
                d[i, j] = Math.Min(Math.Min(min1, min2), min3);
            }
        }

        return d[n, m];
    }

    public static bool IsFuzzyMatch(string rawText, string dbColor, int thresholdDistance = 3)
    {
        if (rawText.Contains(dbColor)) return true;

        var rawWords = rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var colorWords = dbColor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (colorWords.Length == 0 || rawWords.Length < colorWords.Length) return false;

        for (int i = 0; i <= rawWords.Length - colorWords.Length; i++)
        {
            string windowString = string.Join(" ", rawWords, i, colorWords.Length);
            int distance = ComputeLevenshteinDistance(windowString, dbColor);
            
            int allowedDistance = Math.Max(1, Math.Min(thresholdDistance, dbColor.Length / 5));
            if (distance <= allowedDistance)
            {
                return true;
            }
        }
        
        if (rawText.Length < dbColor.Length + 10)
        {
            int distance = ComputeLevenshteinDistance(rawText, dbColor);
            int allowedDistance = Math.Max(1, Math.Min(thresholdDistance, dbColor.Length / 4));
            if (distance <= allowedDistance) return true;
        }

        return false;
    }
}

class Program
{
    static void Main()
    {
        var tests = new (string raw, string db)[] {
            ("NHBOS PEARI. SNOW WHITE", "PEARL SNOW WHITE"),
            ("NHBOS I_UNAR SILVER METALLIC", "LUNAR SILVER METALLIC"),
            ("PB396 STEEL GREV METALLIC", "STEEL GREY METALLIC"),
            ("NHBOS DEEP CRIMSON MICA", "DEEP CRIMSON MICA")
        };

        foreach (var t in tests)
        {
            bool match = FuzzyMatcher.IsFuzzyMatch(t.raw, t.db);
            Console.WriteLine($"Match '{t.db}' in '{t.raw}': {match}");
        }
    }
}
