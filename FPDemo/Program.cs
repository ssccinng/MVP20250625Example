// See https://aka.ms/new-console-template for more information
using LanguageExt;
using System.Collections.Immutable;
using System.Globalization;
using static LanguageExt.Prelude;

Console.WriteLine("Hello, World!");
public record Age(int Value)
{
    public static bool operator >(Age left, Age right) => left.Value > right.Value;
    public static bool operator <(Age left, Age right) => left.Value < right.Value;
    public static Either<string, Age> Create(int value) =>
        value < 0 ? "Age cannot be negative" : new Age(value);
};
public record Book(string Id, string Title, string Author, PublishState PublishState, Age AgeLimit);
public interface PublishState;
public record Published : PublishState;
public record Unpublished : PublishState;
public record Withdrawn : PublishState;

public record Inventory(ImmutableDictionary<string, int> Stock)
{
    public static Inventory Empty => new(ImmutableDictionary<string, int>.Empty);
};

public record User(string Id, string Name, Age Age, Lst<Book> BorrowedBooks, int MaxBorrowedBooks = 5)
{
    // 使用Either<string, User>来传递Age创建时的错误信息
    // 这里可以提升以后 再用应用式解决多参数创建的问题
    public static Either<string, User> Create(string id, string name, int ageValue) =>
        Age.Create(ageValue).Map(age => new User(id, name, age, []));
};

public record Library(
    ImmutableDictionary<string, Book> Books,
    ImmutableDictionary<string, User> Users,
    Inventory Inventory)
{
    public static Library Empty => new(ImmutableDictionary<string, Book>.Empty, ImmutableDictionary<string, User>.Empty, Inventory.Empty);
}

public static class LibraryExtensions
{
    public static Library AddBook(this Library library, Book book, int quantity)
    {
        var newBooks = library.Books.SetItem(book.Id, book);
        var newInventory = library.Inventory.AddStock(book.Id, quantity);
        return library with { Books = newBooks, Inventory = newInventory };
    }

    public static bool CheckUserExist(this Library library, User user) => library.Users.ContainsKey(user.Id);

    public static Either<string, Library> AddUser(this Library library, User user) =>
       CheckUserExist(library, user)
            ? Left($"User '{user.Id}' already exists")
            : Right(library with { Users = library.Users.SetItem(user.Id, user) });

    public static Either<string, User> GetUser(this Library library, string userId) => library.Users.TryGetValue(userId, out var user)
        ? Right(user)
        : Left($"User '{userId}' not found");

    public static Either<string, Book> GetBook(this Library library, string bookId)
    {
        return library.Books.TryGetValue(bookId, out var book)
            ? book
            : $"Book '{bookId}' not found";
    }

    public static Either<string, Library> CheckStock(this Library library, string bookId)
    {
        var stock = library.Inventory.GetStock(bookId);
        return stock > 0
            ? library
            : $"Not enough stock for book '{bookId}'";
    }

    public static Either<string, Library> BorrowBook(this Library library, string userId, string bookId) => from book in library.GetBook(bookId).Bind(UserExtensions.IsPublished) // 并且
                from user in library.GetUser(userId).Bind(s => s.CanBorrowBook(book))
                from _ in library.CheckStock(bookId)

                let newBorrowedBooks = user.BorrowedBooks.Add(book)
                let newUser = user with { BorrowedBooks = newBorrowedBooks }
                let newUsers = library.Users.SetItem(userId, newUser)
                let newInventory = library.Inventory.AddStock(bookId, -1)

                select library with { Users = newUsers, Inventory = newInventory };

    public static Either<string, Library> ReturnBook(this Library library, string userId, string bookId) => from user in library.GetUser(userId)
                from book in user.BorrowedBooks.Find(b => b.Id == bookId).ToEither($"User '{userId}' has not borrowed book '{bookId}'")
                let newBorrowedBooks = user.BorrowedBooks.Remove(book)
                let newUser = user with { BorrowedBooks = newBorrowedBooks }
                let newUsers = library.Users.SetItem(userId, newUser)
                let newInventory = library.Inventory.AddStock(bookId, 1)
                select library with { Users = newUsers, Inventory = newInventory };
}





public static class InventoryExtensions
{
    public static Inventory AddStock(this Inventory inventory, string bookId, int quantity)
    {
        var newStock = inventory.Stock.SetItem(bookId, inventory.Stock.GetValueOrDefault(bookId) + quantity);
        return new Inventory(newStock);
    }
    public static int GetStock(this Inventory inventory, string bookId) =>
        inventory.Stock.GetValueOrDefault(bookId, 0);
}

public static class UserExtensions
{
    public static Either<string, User> CanBorrowBook(this User user, Book book)
    {
        if (user.BorrowedBooks.Count >= user.MaxBorrowedBooks)
        {
            return $"User '{user.Name}' has reached the maximum number of borrowed books";
        }
        if (book.AgeLimit > user.Age)
        {
            return $"User '{user.Name}' is not old enough to borrow '{book.Title}'";
        }
        return user;
    }
    public static Either<string, Book> IsPublished(this Book book) => book.PublishState is Published
        ? book
        : $"Book '{book.Title}' is not published yet";
}

