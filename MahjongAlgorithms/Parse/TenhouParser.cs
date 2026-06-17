namespace MahjongAlgorithms.Parse;

using MahjongAlgorithms.Tiles;

/// <summary>天凤式手牌字符串解析器。格式: "123m456p789s11122z"。</summary>
public static class TenhouParser
{
    public static TileSet? Parse(string hand) { var r = new TileSet(); var m = new List<CalledMeld>(); return ParseInternal(hand, r, m) ? r : null; }
    public static ParsedHand? ParseWithMelds(string hand) { var t = new TileSet(); var m = new List<CalledMeld>(); return ParseInternal(hand, t, m) ? new ParsedHand(t, m) : null; }

    private static bool ParseInternal(string input, TileSet result, List<CalledMeld> calledMelds)
    {
        if (string.IsNullOrEmpty(input)) return false;
        int pos = 0;
        while (pos < input.Length)
        {
            char c = input[pos];
            // 副露标记: [1111m] 暗杠, (1111m) 明杠, (111m) 碰, (123m) 吃
            if (c is '[' or '(')
            {
                bool isClosed = c == '[';
                char close = isClosed ? ']' : ')';
                int closePos = input.IndexOf(close, pos + 1); if (closePos < 0) return false;
                string content = input[(pos + 1)..closePos]; pos = closePos + 1;
                var mt = new TileSet(); if (!ParseTiles(content, mt)) return false;
                int tc = mt.TotalCount;
                MeldType type; bool isKan = false;
                if (tc == 4) { type = MeldType.Kan; isKan = true; }
                else if (tc == 3) { bool allSame = true; byte? fid = null; foreach (var (tile, _) in mt.Tiles) { if (fid == null) fid = tile.Id; else if (tile.Id != fid) { allSame = false; break; } } type = allSame ? MeldType.Pon : MeldType.Chi; }
                else return false;
                calledMelds.Add(new CalledMeld(type, isKan, isClosed, mt)); continue;
            }
            // 字牌字母记法
            string? letter = TryReadLetter(input, ref pos);
            if (letter != null) { var t = Tile.TryParse(letter); if (t == null) return false; result.Add(t.Value); continue; }
            // 数字+花色: 123m, 456p 等
            if (char.IsDigit(c))
            {
                var digits = new List<int>();
                while (pos < input.Length && char.IsDigit(input[pos])) { digits.Add(input[pos] - '0'); pos++; }
                if (pos >= input.Length) return false;
                char suit = char.ToLowerInvariant(input[pos]); pos++;
                if (suit is not ('m' or 'p' or 's' or 'z')) return false;
                foreach (int d in digits)
                {
                    if (d == 0) { var aka = suit switch { 'm' => Tile.M5, 'p' => Tile.P5, 's' => Tile.S5, _ => (Tile?)null }; if (aka == null) return false; result.Add(aka.Value); }
                    else { var t = Tile.TryParse($"{d}{suit}"); if (t == null) return false; result.Add(t.Value); }
                }
                continue;
            }
            return false;
        }
        return true;
    }

    private static bool ParseTiles(string content, TileSet result)
    {
        int pos = 0;
        while (pos < content.Length)
        {
            if (char.IsDigit(content[pos]))
            {
                var digits = new List<int>();
                while (pos < content.Length && char.IsDigit(content[pos])) { digits.Add(content[pos] - '0'); pos++; }
                if (pos >= content.Length) return false;
                char suit = char.ToLowerInvariant(content[pos]); pos++;
                foreach (int d in digits)
                {
                    if (d == 0) { var aka = suit switch { 'm' => Tile.M5, 'p' => Tile.P5, 's' => Tile.S5, _ => (Tile?)null }; if (aka == null) return false; result.Add(aka.Value); }
                    else { var t = Tile.TryParse($"{d}{suit}"); if (t == null) return false; result.Add(t.Value); }
                }
                continue;
            }
            string? letter = TryReadLetter(content, ref pos);
            if (letter != null) { var t = Tile.TryParse(letter); if (t == null) return false; result.Add(t.Value); }
            else return false;
        }
        return true;
    }

    private static string? TryReadLetter(string input, ref int pos)
    {
        if (pos >= input.Length) return null;
        switch (char.ToLowerInvariant(input[pos]))
        {
            case 'e': pos++; return "e";
            case 's': pos++; return "s";
            case 'w': pos++; if (pos < input.Length && char.ToLowerInvariant(input[pos]) == 'h') { pos++; return "wh"; } return "w";
            case 'n': pos++; return "n";
            case 'g': pos++; return "g";
            case 'r': pos++; return "r";
            default: return null;
        }
    }
}

public enum MeldType { Chi, Pon, Kan }
public record CalledMeld(MeldType Type, bool IsKan, bool IsClosed, TileSet Tiles);
public record ParsedHand(TileSet Tiles, List<CalledMeld> CalledMelds);
