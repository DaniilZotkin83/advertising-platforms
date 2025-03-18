// Общий класс TrieNode для обычного Trie
public class TrieNode
{
    public Dictionary<char, TrieNode> Children = new();
    public List<string> Locations = new();
    public HashSet<string> Platforms { get; } = new();
}

public class Trie
{
    private readonly TrieNode _root = new();

    public void Insert(string location, string adSpot)
    {
        var node = _root;
        foreach (char ch in location.ToLower())
        {
            if (!node.Children.ContainsKey(ch))
                node.Children[ch] = new TrieNode();
            node = node.Children[ch];
        }
        node.Locations.Add(adSpot);
    }

    public List<string> Search(string prefix)
    {
        var node = _root;
        foreach (char ch in prefix.ToLower())
        {
            if (!node.Children.ContainsKey(ch))
                return new List<string>();
            node = node.Children[ch];
        }
        return CollectAll(node);
    }

    private List<string> CollectAll(TrieNode node)
    {
        List<string> results = new(node.Locations);
        foreach (var child in node.Children.Values)
        {
            results.AddRange(CollectAll(child));
        }
        return results;
    }
}

// Отдельная реализация узла для AdPlatformTrie
public class AdPlatformTrieNode
{
    public Dictionary<string, AdPlatformTrieNode> Children = new();
    public HashSet<string> Platforms { get; } = new();
}

class AdPlatformTrie
{
    private readonly AdPlatformTrieNode root = new();

    public void AddPlatform(string platform, string location)
    {
        var parts = location.Trim('/')
            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.ToLower())
            .ToArray();

        var node = root;
        foreach (var part in parts)
        {
            if (!node.Children.ContainsKey(part))
                node.Children[part] = new AdPlatformTrieNode();
            node = node.Children[part];
        }
        node.Platforms.Add(platform);
    }

    public List<string> Search(string location)
    {
        var parts = location.Trim('/')
            .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.ToLower())
            .ToArray();

        var node = root;
        var result = new HashSet<string>();

        foreach (var part in parts)
        {
            if (node.Platforms.Count > 0)
                result.UnionWith(node.Platforms);

            if (!node.Children.TryGetValue(part, out var childNode))
                break;

            node = childNode;
        }

        // Добавляем платформы последнего достигнутого узла
        result.UnionWith(node.Platforms);
        return result.ToList();
    }
}