namespace purry;
public class TempJO : JO
{
    private readonly HashSet<string> check = new();
    private bool keepAll = false;

    public void Keep(string key = null)
    {
        if (key == null)
        {
            keepAll = true;
            return;
        }
        if (Has(key)) check.Add(key);
    }

    internal void Commit()
    {
        if (keepAll)
        {
            keepAll = false;
            return;
        }
        foreach (string key in Keys)
        {
            if (!check.Contains(key)) Remove(key);
        }
        check.Clear();
    }

    public object Peek(string key){
        Keep(key);
        return base.GetValue(key);
    }
}