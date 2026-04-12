using Microsoft.EntityFrameworkCore.Query;
using System.Collections;
using System.Linq.Expressions;

public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    public TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
        => new TestAsyncEnumerable<TEntity>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object? Execute(Expression expression)
        => _inner.Execute(expression);

    public TResult Execute<TResult>(Expression expression)
        => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult);

        if (!resultType.IsGenericType)
        {
            return default!;
        }

        var innerType = resultType.GetGenericArguments()[0];

        var executeMethod = typeof(IQueryProvider)
            .GetMethods()
            .First(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod);

        var executionResult = executeMethod
            .MakeGenericMethod(innerType)
            .Invoke(_inner, new object[] { expression });

        var fromResultMethod = typeof(Task)
            .GetMethods()
            .First(m => m.Name == nameof(Task.FromResult) && m.IsGenericMethod);

        var task = fromResultMethod
            .MakeGenericMethod(innerType)
            .Invoke(null, new[] { executionResult });

        return (TResult)task!;
    }
}

public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner)
    {
        _inner = inner;
    }

    public T Current => _inner.Current;

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync()
        => new ValueTask<bool>(_inner.MoveNext());
}