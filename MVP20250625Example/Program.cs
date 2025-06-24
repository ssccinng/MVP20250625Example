using System;
using System.Collections.Generic;
using System.Linq;

// 自定义异常类
public class BookNotFoundException : Exception
{
    public BookNotFoundException(string message) : base(message) { }
}

public class BookAlreadyBorrowedException : Exception
{
    public BookAlreadyBorrowedException(string message) : base(message) { }
}

public class BookNotBorrowedException : Exception
{
    public BookNotBorrowedException(string message) : base(message) { }
}

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message) : base(message) { }
}

// 图书类
public class Book
{
    public string Id { get; private set; }
    public string Title { get; private set; }
    public string Author { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? BorrowedBy { get; private set; }
    public DateTime? BorrowDate { get; private set; }

    public Book(string id, string title, string author)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Author = author ?? throw new ArgumentNullException(nameof(author));
        IsAvailable = true;
    }

    public void Borrow(string userId)
    {
        if (!IsAvailable)
        {
            throw new BookAlreadyBorrowedException($"图书 '{Title}' 已被借出");
        }

        IsAvailable = false;
        BorrowedBy = userId;
        BorrowDate = DateTime.Now;
    }

    public void Return()
    {
        if (IsAvailable)
        {
            throw new BookNotBorrowedException($"图书 '{Title}' 未被借出，无法归还");
        }

        IsAvailable = true;
        BorrowedBy = null;
        BorrowDate = null;
    }

    public override string ToString()
    {
        return $"[{Id}] {Title} - {Author} ({(IsAvailable ? "可借" : $"已借出给 {BorrowedBy}")})";
    }
}

// 用户类
public class User
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public List<string> BorrowedBooks { get; private set; }
    public int MaxBorrowLimit { get; private set; }

    public User(string id, string name, int maxBorrowLimit = 5)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        MaxBorrowLimit = maxBorrowLimit;
        BorrowedBooks = new List<string>();
    }

    public bool CanBorrowMore()
    {
        return BorrowedBooks.Count < MaxBorrowLimit;
    }

    public void AddBorrowedBook(string bookId)
    {
        if (!CanBorrowMore())
        {
            throw new InvalidOperationException($"用户 '{Name}' 已达到借书上限 ({MaxBorrowLimit} 本)");
        }
        BorrowedBooks.Add(bookId);
    }

    public void RemoveBorrowedBook(string bookId)
    {
        if (!BorrowedBooks.Remove(bookId))
        {
            throw new InvalidOperationException($"用户 '{Name}' 没有借阅图书 '{bookId}'");
        }
    }

    public override string ToString()
    {
        return $"用户: {Name} (ID: {Id}) - 已借 {BorrowedBooks.Count}/{MaxBorrowLimit} 本";
    }
}

// 图书馆类
public class Library
{
    private Dictionary<string, Book> books;
    private Dictionary<string, User> users;

    public Library()
    {
        books = new Dictionary<string, Book>();
        users = new Dictionary<string, User>();
    }

    public void AddBook(Book book)
    {
        if (book == null)
            throw new ArgumentNullException(nameof(book));

        if (books.ContainsKey(book.Id))
            throw new ArgumentException($"图书ID '{book.Id}' 已存在");

        books[book.Id] = book;
        Console.WriteLine($"✓ 添加图书: {book}");
    }

    public void AddUser(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (users.ContainsKey(user.Id))
            throw new ArgumentException($"用户ID '{user.Id}' 已存在");

        users[user.Id] = user;
        Console.WriteLine($"✓ 添加用户: {user}");
    }

    public void BorrowBook(string userId, string bookId)
    {
        // 验证用户存在
        if (!users.TryGetValue(userId, out User? user))
        {
            throw new UserNotFoundException($"用户ID '{userId}' 不存在");
        }

        // 验证图书存在
        if (!books.TryGetValue(bookId, out Book? book))
        {
            throw new BookNotFoundException($"图书ID '{bookId}' 不存在");
        }

        // 检查用户借书限额
        if (!user.CanBorrowMore())
        {
            throw new InvalidOperationException($"用户 '{user.Name}' 已达到借书上限");
        }

        // 执行借书操作
        book.Borrow(userId);
        user.AddBorrowedBook(bookId);

        Console.WriteLine($"✓ 借书成功: {user.Name} 借阅了 《{book.Title}》");
    }

    public void ReturnBook(string userId, string bookId)
    {
        // 验证用户存在
        if (!users.TryGetValue(userId, out User? user))
        {
            throw new UserNotFoundException($"用户ID '{userId}' 不存在");
        }

        // 验证图书存在
        if (!books.TryGetValue(bookId, out Book? book))
        {
            throw new BookNotFoundException($"图书ID '{bookId}' 不存在");
        }

        // 验证这本书确实是这个用户借的
        if (book.BorrowedBy != userId)
        {
            throw new InvalidOperationException($"图书 '{book.Title}' 不是由用户 '{user.Name}' 借出的");
        }

        // 执行还书操作
        book.Return();
        user.RemoveBorrowedBook(bookId);

        Console.WriteLine($"✓ 还书成功: {user.Name} 归还了 《{book.Title}》");
    }

    public void DisplayAllBooks()
    {
        Console.WriteLine("\n=== 图书馆藏书 ===");
        if (books.Count == 0)
        {
            Console.WriteLine("暂无图书");
            return;
        }

        foreach (var book in books.Values)
        {
            Console.WriteLine(book);
        }
    }

    public void DisplayAllUsers()
    {
        Console.WriteLine("\n=== 注册用户 ===");
        if (users.Count == 0)
        {
            Console.WriteLine("暂无用户");
            return;
        }

        foreach (var user in users.Values)
        {
            Console.WriteLine(user);
            if (user.BorrowedBooks.Count > 0)
            {
                Console.WriteLine($"  借阅的图书: {string.Join(", ", user.BorrowedBooks.Select(id => books[id].Title))}");
            }
        }
    }

    public void DisplayAvailableBooks()
    {
        Console.WriteLine("\n=== 可借图书 ===");
        var availableBooks = books.Values.Where(b => b.IsAvailable).ToList();
        
        if (availableBooks.Count == 0)
        {
            Console.WriteLine("暂无可借图书");
            return;
        }

        foreach (var book in availableBooks)
        {
            Console.WriteLine(book);
        }
    }
}

// 主程序
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 图书馆管理系统 (OOP版本) ===\n");

        var library = new Library();

        try
        {
            // 添加图书
            library.AddBook(new Book("B001", "C# 编程指南", "微软"));
            library.AddBook(new Book("B002", "设计模式", "GoF"));
            library.AddBook(new Book("B003", "算法导论", "Cormen"));
            library.AddBook(new Book("B004", "代码整洁之道", "Robert Martin"));

            // 添加用户
            library.AddUser(new User("U001", "张三", 3));
            library.AddUser(new User("U002", "李四", 2));

            // 显示初始状态
            library.DisplayAllBooks();
            library.DisplayAllUsers();

            Console.WriteLine("\n=== 开始借书操作 ===");

            // 正常借书
            library.BorrowBook("U001", "B001");
            library.BorrowBook("U001", "B002");
            library.BorrowBook("U002", "B003");

            // 显示当前状态
            library.DisplayAllBooks();
            library.DisplayAvailableBooks();
            library.DisplayAllUsers();

            Console.WriteLine("\n=== 开始还书操作 ===");

            // 正常还书
            library.ReturnBook("U001", "B001");

            // 显示还书后状态
            library.DisplayAllBooks();
            library.DisplayAllUsers();

            Console.WriteLine("\n=== 测试异常情况 ===");

            // 测试各种异常情况
            TestExceptions(library);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 程序异常: {ex.Message}");
        }

        Console.WriteLine("\n程序结束，按任意键退出...");
        Console.ReadKey();
    }

    static void TestExceptions(Library library)
    {
        // 测试1: 借不存在的书
        try
        {
            library.BorrowBook("U001", "B999");
        }
        catch (BookNotFoundException ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }

        // 测试2: 不存在的用户借书
        try
        {
            library.BorrowBook("U999", "B004");
        }
        catch (UserNotFoundException ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }

        // 测试3: 借已被借出的书
        try
        {
            library.BorrowBook("U002", "B002"); // B002已被U001借出
        }
        catch (BookAlreadyBorrowedException ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }

        // 测试4: 超出借书限额
        try
        {
            library.BorrowBook("U001", "B004"); // U001已借2本，限额3本
            library.BorrowBook("U001", "B004"); // 再借一本，应该失败因为B004已被借出
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }

        // 测试5: 归还未借的书
        try
        {
            library.ReturnBook("U002", "B004"); // U002没有借B004
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }

        // 测试6: 归还已归还的书
        try
        {
            library.ReturnBook("U001", "B001"); // B001已经归还过了
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 捕获异常: {ex.Message}");
        }
    }
}
