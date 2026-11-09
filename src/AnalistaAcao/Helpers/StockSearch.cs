using System.Globalization;
using System.Text;
using AnalistaAcao.Models;

namespace AnalistaAcao.Helpers;

/// <summary>
/// Filtro e ordenação das sugestões de busca. Função pura (sem rede nem UI).
/// Aceita ticker ou nome da empresa, sem diferenciar maiúsculas nem acentos.
/// </summary>
public static class StockSearch
{
    private const int NoMatch = int.MaxValue;
    private const int MinLengthForContains = 3;

    private static readonly char[] WordSeparators = [' ', '.', ',', '-', '/', '(', ')'];
    private static readonly HashSet<string> StopWords = ["DO", "DA", "DE", "DOS", "DAS"];

    /// <summary>
    /// Retorna até <paramref name="max"/> ativos. Ordem: ticker exato, ticker que começa
    /// com o texto, palavra do nome que começa com o texto, texto contido em qualquer
    /// parte. Em caso de empate, a ação mais negociada vem primeiro.
    /// </summary>
    public static IReadOnlyList<StockSummary> Filter(IEnumerable<StockSummary> catalog, string query, int max)
    {
        var allTokens = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (allTokens.Length == 0)
            return [];

        var compactQuery = string.Concat(allTokens);

        // "Banco do Brasil" precisa achar "BCO BRASIL": ignora "do", "da"...
        var significant = allTokens.Where(t => !StopWords.Contains(t)).ToArray();
        var tokens = significant.Length > 0 ? significant : allTokens;

        return catalog
            .Select(stock => (Stock: stock, Rank: Rank(stock, compactQuery, tokens)))
            .Where(x => x.Rank != NoMatch)
            .OrderBy(x => x.Rank)
            .ThenByDescending(x => x.Stock.Volume)
            .ThenBy(x => x.Stock.Symbol, StringComparer.Ordinal)
            .Take(max)
            .Select(x => x.Stock)
            .ToList();
    }

    private static int Rank(StockSummary stock, string compactQuery, string[] tokens)
    {
        var symbol = Normalize(stock.Symbol);
        if (symbol == compactQuery) return 0;
        if (symbol.StartsWith(compactQuery, StringComparison.Ordinal)) return 1;

        var name = Normalize(stock.Name);
        var words = name.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);

        // A B3 abrevia "Banco" como "BCO" nos nomes registrados.
        if (words.Contains("BCO"))
            words = [.. words, "BANCO"];

        if (tokens.All(t => words.Any(w => w.StartsWith(t, StringComparison.Ordinal))))
            return 2;

        // "Contém" só a partir de 3 letras: com menos, quase tudo combinaria.
        if (tokens.All(t => t.Length >= MinLengthForContains &&
                            (name.Contains(t, StringComparison.Ordinal) ||
                             symbol.Contains(t, StringComparison.Ordinal))))
            return 3;

        return NoMatch;
    }

    /// <summary>Maiúsculas e sem acentos: "Petróleo" vira "PETROLEO".</summary>
    private static string Normalize(string text)
    {
        var decomposed = text.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }
}
