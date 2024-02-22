namespace Kyna.Analysis;

public struct Split
{
    public string Source;
    public string Code;
    public DateOnly SplitDate;
    public double Before;
    public double After;
    public readonly double Factor => Before == 0 ? 1 : (After / Before);
}

public struct Dividend
{

}
