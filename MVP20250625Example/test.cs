// filepath: d:\wcsrepo\MVP20250625Example\MVP20250625Example\test.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalLibrary
{
    // 结果类型 - 用于函数式错误处理
    public abstract class Result<T>
    {
        public abstract bool IsSuccess { get; }
        public abstract T Value { get; }
        public abstract string Error { get; }

        public static Result<T> Success(T value) => new SuccessResult<T>(value);
        public static Result<T> Failure(string error) => new FailureResult<T>(error);

        public Result<U> Map<U>(Func<T, U> func)
        {
            return IsSuccess ? Result<U>.Success(func(Value)) : Result<U>.Failure(Error);
        }

        public Result<U> Bind<U>(Func<T, Result<U>> func)
        {
            return IsSuccess ? func(Value) : Result<U>.Failure(Error);
        }
    }

    public class SuccessResult<T> : Result<T>
    {
        public override bool IsSuccess => true;
        public override T Value { get; }
        public override string Error => throw new InvalidOperationException("Success result has no error");

        public SuccessResult(T value) => Value = value;
    }

    public class FailureResult<T> : Result<T>
    {
        public override bool IsSuccess => false;
        public override T Value => throw new InvalidOperationException("Failure result has no value");
        public override string Error { get; }

        public FailureResult(string error) => Error = error;
    }

    // 不可变数据结构
    public record Book(string Id, string Title, string Author, bool IsAvailable = true, 
                      string? BorrowedBy = null, DateTime? BorrowDate = null)
    {
        public override string ToString() =>
            $"[{Id}] {Title} - {Author} ({(IsAvailable ? "可借" : $"已借出给 {BorrowedBy}")})";
    }

    public record User(string Id, string Name, IReadOnlyList<string> BorrowedBooks, int MaxBorrowLimit = 5)
    {
        public User(string id, string name, int maxBorrowLimit = 5) 
            : this(id, name, new List<string>(), maxBorrowLimit) { }

        public bool CanBorrowMore => BorrowedBooks.Count < MaxBorrowLimit;

        public override string ToString() =>
            $"用户: {Name} (ID: {Id}) - 已借 {BorrowedBooks.Count}/{MaxBorrowLimit} 本";
    }

    public record LibraryState(IReadOnlyDictionary<string, Book> Books, 
                              IReadOnlyDictionary<string, User> Users)
    {
        public static LibraryState Empty => new(
            new Dictionary<string, Book>(),
            new Dictionary<string, User>()
        );
    }

    // 纯函数库
    public static class LibraryFunctions
    {
        // 添加图书
        public static Result<LibraryState> AddBook(LibraryState state, Book book)
        {
            if (string.IsNullOrEmpty(book.Id) || string.IsNullOrEmpty(book.Title) || string.IsNullOrEmpty(book.Author))
                return Result<LibraryState>.Failure("图书信息不能为空");

            if (state.Books.ContainsKey(book.Id))
                return Result<LibraryState>.Failure($"图书ID '{book.Id}' 已存在");

            var newBooks = new Dictionary<string, Book>(state.Books) { [book.Id] = book };
            return Result<LibraryState>.Success(state with { Books = newBooks });
        }

        // 添加用户
        public static Result<LibraryState> AddUser(LibraryState state, User user)
        {
            if (string.IsNullOrEmpty(user.Id) || string.IsNullOrEmpty(user.Name))
                return Result<LibraryState>.Failure("用户信息不能为空");

            if (state.Users.ContainsKey(user.Id))
                return Result<LibraryState>.Failure($"用户ID '{user.Id}' 已存在");

            var newUsers = new Dictionary<string, User>(state.Users) { [user.Id] = user };
            return Result<LibraryState>.Success(state with { Users = newUsers });
        }

        // 借书操作
        public static Result<LibraryState> BorrowBook(LibraryState state, string userId, string bookId)
        {
            return ValidateUser(state, userId)
                .Bind(user => ValidateBook(state, bookId)
                    .Bind(book => ValidateCanBorrow(user, book)
                        .Bind(_ => ExecuteBorrow(state, user, book))));
        }

        // 还书操作
        public static Result<LibraryState> ReturnBook(LibraryState state, string userId, string bookId)
        {
            return ValidateUser(state, userId)
                .Bind(user => ValidateBook(state, bookId)
                    .Bind(book => ValidateCanReturn(user, book)
                        .Bind(_ => ExecuteReturn(state, user, book))));
        }

        // 验证函数
        private static Result<User> ValidateUser(LibraryState state, string userId)
        {
            return state.Users.TryGetValue(userId, out var user)
                ? Result<User>.Success(user)
                : Result<User>.Failure($"用户ID '{userId}' 不存在");
        }

        private static Result<Book> ValidateBook(LibraryState state, string bookId)
        {
            return state.Books.TryGetValue(bookId, out var book)
                ? Result<Book>.Success(book)
                : Result<Book>.Failure($"图书ID '{bookId}' 不存在");
        }

        private static Result<Unit> ValidateCanBorrow(User user, Book book)
        {
            if (!book.IsAvailable)
                return Result<Unit>.Failure($"图书 '{book.Title}' 已被借出");

            if (!user.CanBorrowMore)
                return Result<Unit>.Failure($"用户 '{user.Name}' 已达到借书上限");

            return Result<Unit>.Success(Unit.Value);
        }

        private static Result<Unit> ValidateCanReturn(User user, Book book)
        {
            if (book.IsAvailable)
                return Result<Unit>.Failure($"图书 '{book.Title}' 未被借出，无法归还");

            if (book.BorrowedBy != user.Id)
                return Result<Unit>.Failure($"图书 '{book.Title}' 不是由用户 '{user.Name}' 借出的");

            return Result<Unit>.Success(Unit.Value);
        }

        // 执行操作函数
        private static Result<LibraryState> ExecuteBorrow(LibraryState state, User user, Book book)
        {
            var borrowedBook = book with 
            { 
                IsAvailable = false, 
                BorrowedBy = user.Id, 
                BorrowDate = DateTime.Now 
            };

            var updatedUser = user with 
            { 
                BorrowedBooks = user.BorrowedBooks.Append(book.Id).ToList() 
            };

            var newBooks = new Dictionary<string, Book>(state.Books) { [book.Id] = borrowedBook };
            var newUsers = new Dictionary<string, User>(state.Users) { [user.Id] = updatedUser };

            return Result<LibraryState>.Success(state with { Books = newBooks, Users = newUsers });
        }

        private static Result<LibraryState> ExecuteReturn(LibraryState state, User user, Book book)
        {
            var returnedBook = book with 
            { 
                IsAvailable = true, 
                BorrowedBy = null, 
                BorrowDate = null 
            };

            var updatedUser = user with 
            { 
                BorrowedBooks = user.BorrowedBooks.Where(id => id != book.Id).ToList() 
            };

            var newBooks = new Dictionary<string, Book>(state.Books) { [book.Id] = returnedBook };
            var newUsers = new Dictionary<string, User>(state.Users) { [user.Id] = updatedUser };

            return Result<LibraryState>.Success(state with { Books = newBooks, Users = newUsers });
        }

        // 查询函数
        public static IEnumerable<Book> GetAllBooks(LibraryState state) => state.Books.Values;

        public static IEnumerable<Book> GetAvailableBooks(LibraryState state) =>
            state.Books.Values.Where(book => book.IsAvailable);

        public static IEnumerable<User> GetAllUsers(LibraryState state) => state.Users.Values;

        public static Option<Book> FindBook(LibraryState state, string bookId) =>
            state.Books.TryGetValue(bookId, out var book) ? Option<Book>.Some(book) : Option<Book>.None;

        public static Option<User> FindUser(LibraryState state, string userId) =>
            state.Users.TryGetValue(userId, out var user) ? Option<User>.Some(user) : Option<User>.None;
    }

    // Unit类型 - 表示无返回值
    public struct Unit
    {
        public static Unit Value => new();
    }

    // Option类型 - 处理可能为空的值
    public abstract class Option<T>
    {
        public abstract bool HasValue { get; }
        public abstract T Value { get; }

        public static Option<T> Some(T value) => new SomeOption<T>(value);
        public static Option<T> None => new NoneOption<T>();

        public U Match<U>(Func<T, U> onSome, Func<U> onNone) =>
            HasValue ? onSome(Value) : onNone();
    }

    public class SomeOption<T> : Option<T>
    {
        public override bool HasValue => true;
        public override T Value { get; }
        public SomeOption(T value) => Value = value;
    }

    public class NoneOption<T> : Option<T>
    {
        public override bool HasValue => false;
        public override T Value => throw new InvalidOperationException("None option has no value");
    }

    // 显示辅助函数
    public static class DisplayFunctions
    {
        public static void DisplayBooks(LibraryState state)
        {
            Console.WriteLine("\n=== 图书馆藏书 (FP版本) ===");
            var books = LibraryFunctions.GetAllBooks(state).ToList();
            
            if (!books.Any())
            {
                Console.WriteLine("暂无图书");
                return;
            }

            books.ForEach(book => Console.WriteLine(book));
        }

        public static void DisplayUsers(LibraryState state)
        {
            Console.WriteLine("\n=== 注册用户 (FP版本) ===");
            var users = LibraryFunctions.GetAllUsers(state).ToList();
            
            if (!users.Any())
            {
                Console.WriteLine("暂无用户");
                return;
            }

            users.ForEach(user =>
            {
                Console.WriteLine(user);
                if (user.BorrowedBooks.Any())
                {
                    var bookTitles = user.BorrowedBooks
                        .Select(id => LibraryFunctions.FindBook(state, id))
                        .Where(opt => opt.HasValue)
                        .Select(opt => opt.Value.Title);
                    Console.WriteLine($"  借阅的图书: {string.Join(", ", bookTitles)}");
                }
            });
        }

        public static void DisplayAvailableBooks(LibraryState state)
        {
            Console.WriteLine("\n=== 可借图书 (FP版本) ===");
            var availableBooks = LibraryFunctions.GetAvailableBooks(state).ToList();
            
            if (!availableBooks.Any())
            {
                Console.WriteLine("暂无可借图书");
                return;
            }

            availableBooks.ForEach(book => Console.WriteLine(book));
        }
    }

    // 主程序类
    public class FunctionalLibraryDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== 图书馆管理系统 (函数式编程版本) ===\n");

            // 初始化空状态
            var state = LibraryState.Empty;

            // 使用函数式方式构建初始数据
            state = AddBooksToLibrary(state);
            state = AddUsersToLibrary(state);

            // 显示初始状态
            DisplayFunctions.DisplayBooks(state);
            DisplayFunctions.DisplayUsers(state);

            Console.WriteLine("\n=== 开始借书操作 (FP版本) ===");

            // 执行借书操作
            state = ExecuteBorrowOperations(state);

            // 显示状态
            DisplayFunctions.DisplayBooks(state);
            DisplayFunctions.DisplayAvailableBooks(state);
            DisplayFunctions.DisplayUsers(state);

            Console.WriteLine("\n=== 开始还书操作 (FP版本) ===");

            // 执行还书操作
            state = ExecuteReturnOperations(state);

            // 显示最终状态
            DisplayFunctions.DisplayBooks(state);
            DisplayFunctions.DisplayUsers(state);

            Console.WriteLine("\n=== 测试异常情况 (FP版本) ===");
            TestExceptionalCases(state);

            Console.WriteLine("\n函数式版本演示结束！");
        }

        private static LibraryState AddBooksToLibrary(LibraryState state)
        {
            var books = new[]
            {
                new Book("B001", "C# 编程指南", "微软"),
                new Book("B002", "设计模式", "GoF"),
                new Book("B003", "算法导论", "Cormen"),
                new Book("B004", "代码整洁之道", "Robert Martin")
            };

            return books.Aggregate(state, (currentState, book) =>
                LibraryFunctions.AddBook(currentState, book).Match(
                    success => { Console.WriteLine($"✓ 添加图书: {book}"); return success; },
                    () => { Console.WriteLine($"❌ 添加图书失败: {book}"); return currentState; }
                ));
        }

        private static LibraryState AddUsersToLibrary(LibraryState state)
        {
            var users = new[]
            {
                new User("U001", "张三", 3),
                new User("U002", "李四", 2)
            };

            return users.Aggregate(state, (currentState, user) =>
                LibraryFunctions.AddUser(currentState, user).Match(
                    success => { Console.WriteLine($"✓ 添加用户: {user}"); return success; },
                    () => { Console.WriteLine($"❌ 添加用户失败: {user}"); return currentState; }
                ));
        }

        private static LibraryState ExecuteBorrowOperations(LibraryState state)
        {
            var borrowOperations = new[]
            {
                ("U001", "B001"),
                ("U001", "B002"),
                ("U002", "B003")
            };

            return borrowOperations.Aggregate(state, (currentState, operation) =>
                LibraryFunctions.BorrowBook(currentState, operation.Item1, operation.Item2).Match(
                    success => 
                    {
                        var user = LibraryFunctions.FindUser(success, operation.Item1).Value;
                        var book = LibraryFunctions.FindBook(success, operation.Item2).Value;
                        Console.WriteLine($"✓ 借书成功: {user.Name} 借阅了 《{book.Title}》");
                        return success;
                    },
                    () => { Console.WriteLine($"❌ 借书失败"); return currentState; }
                ));
        }

        private static LibraryState ExecuteReturnOperations(LibraryState state)
        {
            return LibraryFunctions.ReturnBook(state, "U001", "B001").Match(
                success =>
                {
                    var user = LibraryFunctions.FindUser(success, "U001").Value;
                    var book = LibraryFunctions.FindBook(success, "B001").Value;
                    Console.WriteLine($"✓ 还书成功: {user.Name} 归还了 《{book.Title}》");
                    return success;
                },
                () => { Console.WriteLine("❌ 还书失败"); return state; }
            );
        }

        private static void TestExceptionalCases(LibraryState state)
        {
            var testCases = new[]
            {
                ("U001", "B999", "借不存在的书"),
                ("U999", "B004", "不存在的用户借书"),
                ("U002", "B002", "借已被借出的书"),
                ("U002", "B004", "归还未借的书")
            };

            foreach (var (userId, bookId, description) in testCases)
            {
                var result = LibraryFunctions.BorrowBook(state, userId, bookId);
                if (!result.IsSuccess)
                {
                    Console.WriteLine($"❌ {description}: {result.Error}");
                }
            }
        }
    }
    // 扩展方法
    public static class Extensions
    {
        public static U Match<T, U>(this Result<T> result, Func<T, U> onSuccess, Func<U> onFailure) =>
            result.IsSuccess ? onSuccess(result.Value) : onFailure();

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}