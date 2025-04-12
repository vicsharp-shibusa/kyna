namespace Kyna.DataProviders.Polygon.Models;

public struct FlatFileLine
{
    public FlatFileLine(string text)
    {
        var split = text.Split(',');
        if (split.Length != 8)
            throw new ArgumentException($"Expecting 8 items in {nameof(FlatFileLine)} text.");

        Code = split[0].Trim();
        if (!long.TryParse(split[1], out Volume))
            ThrowParsingException(text, nameof(Volume));
        if (!decimal.TryParse(split[2], out Open))
            ThrowParsingException(text, nameof(Open));
        if (!decimal.TryParse(split[3], out Close))
            ThrowParsingException(text, nameof(Close));
        if (!decimal.TryParse(split[4], out High))
            ThrowParsingException(text, nameof(High));
        if (!decimal.TryParse(split[5], out Low))
            ThrowParsingException(text, nameof(Low));
        if (!long.TryParse(split[6], out WindowStart))
            ThrowParsingException(text, nameof(WindowStart));
        if (!int.TryParse(split[7], out Transactions))
            ThrowParsingException(text, nameof(Transactions));
    }

    private ArgumentException ThrowParsingException(string text, string property)
    {
        throw new ArgumentException($"Could not parse {nameof(property)} in {nameof(FlatFileLine)}; line: {text}");
    }

    public string Code;
    public long Volume;
    public decimal Open;
    public decimal Close;
    public decimal High;
    public decimal Low;
    public long WindowStart;
    public int Transactions;

    public readonly DateOnly Date =>
        DateOnly.FromDateTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Local).AddTicks(WindowStart / 100));
}
