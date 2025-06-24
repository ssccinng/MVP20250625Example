// See https://aka.ms/new-console-template for more information



public class Book
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int AgeLimit { get; set; } = 0;
    public bool IsPublished { get; set; }
}


// 以下这个类，代表图书库存有多少
public class Inventory
{
    private Dictionary<string, int> stock = new();
    public void AddStock(string bookId, int quantity)
    {
        if (stock.ContainsKey(bookId))
        {
            stock[bookId] += quantity;
        }
        else
        {
            stock[bookId] = quantity;
        }
    }
    public int GetStock(string bookId)
    {
        return stock.TryGetValue(bookId, out var quantity) ? quantity : 0;
    }
}

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public List<Book> BorrowedBooks { get; set; } = new List<Book>();

    public int MaxBorrowedBooks { get; set; } = 5;

}

public class BorrowedBook : Book
{
    public DateTime BorrowedDate { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue => DateTime.Now > DueDate;
    public User Borrower { get; set; } = new User();
}
public class Library
{
    private readonly Dictionary<string, Book> books = new();
    private readonly Dictionary<string, User> users = new();
    private readonly Inventory inventory = new();

    public void AddBook(Book book, int quantity)
    {
        books[book.Id] = book;
        inventory.AddStock(book.Id, quantity);
    }

    public void RegisterUser(User user)
    {
        users[user.Id] = user;
    }

    public bool BorrowBook(string userId, string bookId)
    {
        if (!users.TryGetValue(userId, out var user))
            throw new ArgumentException($"用户ID不存在: {userId}");
        if (!books.TryGetValue(bookId, out var book))
            throw new ArgumentException($"图书ID不存在: {bookId}");
        if (inventory.GetStock(bookId) <= 0)
            throw new InvalidOperationException($"图书库存不足: {bookId}");
        if (user.BorrowedBooks.Any(b => b.Id == bookId))
            throw new InvalidOperationException($"用户已借阅该图书: {bookId}");
        if (user.BorrowedBooks.Count >= user.MaxBorrowedBooks)
            throw new InvalidOperationException($"用户已达到最大借阅数量: {user.MaxBorrowedBooks}");
        if (book.AgeLimit < user.Age && book.AgeLimit > 0)
            throw new InvalidOperationException($"用户年龄不符合借阅要求: {book.AgeLimit}岁及以上可借阅");

        inventory.AddStock(bookId, -1);
        user.BorrowedBooks.Add(book);
        return true;
    }

    public bool ReturnBook(string userId, string bookId)
    {
        if (!users.TryGetValue(userId, out var user))
            throw new ArgumentException($"用户ID不存在: {userId}");
        var borrowedBook = user.BorrowedBooks.FirstOrDefault(b => b.Id == bookId);
        if (borrowedBook == null)
            throw new InvalidOperationException($"用户未借阅该图书: {bookId}");

        user.BorrowedBooks.Remove(borrowedBook);
        inventory.AddStock(bookId, 1);
        return true;
    }

    public string? GetBookBorrower(string bookId)
    {
        foreach (var user in users.Values)
        {
            if (user.BorrowedBooks.Any(b => b.Id == bookId))
            {
                return user.Name;
            }
        }
        return null;
    }
}
