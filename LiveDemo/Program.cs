// See https://aka.ms/new-console-template for more information

int[] a = Enumerable
    .Range(1, 10)
    .Select(x => x * 3) // Map
    //.SelectMany // fold 
    .Reverse()
    .ToArray();

Nullable<int> a1 = null; // 这行代码是为了演示 null 的使用，实际代码中不需要
var f = (int a) => a + 1;
AA aa = new AA(1, "scixing");

Console.WriteLine(aa);
var bb = aa with { Name = "scixing2" };

Console.WriteLine(bb);

return;

//aa

List<int> mod2 = [];

for (int i = 0; i < a.Length; i++)
{
    if (a[i] % 3 == 0)
    {
        mod2.Add(a[i]);
    }
}



var mod3FP = 
    a
    .Where(IsMod(3))
    .ToList()
    ;

Console.WriteLine(string.Join(", ", mod2));
Console.WriteLine(string.Join(", ", mod3FP));
var a11 = (1, 3);
//Array.Sort(a);

Console.WriteLine(string.Join(", ", a));
Console.WriteLine(string.Join(", ", a.Order()));


Func<int, bool> IsMod(int mod) => x => x % mod == 0;

public record class AA(int Id, string Name)
{
    public int Id { get; init; } = Id;
    public string Name { get; init; } = Name;
}

