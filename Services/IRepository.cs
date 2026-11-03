namespace PersonalFinance.Services;

public interface IRepository<T> where T : class
{
    IReadOnlyList<T> GetAll();
    T? GetById(string id);
    void Add(T entity);
    void Update(T entity);
    bool Delete(string id);
    void Clear();
}

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    private readonly List<T> _items = new();
    private readonly Func<T, string> _idSelector;

    public InMemoryRepository(Func<T, string> idSelector)
    {
        _idSelector = idSelector;
    }

    public IReadOnlyList<T> GetAll() => _items.AsReadOnly();

    public T? GetById(string id) => _items.FirstOrDefault(x => _idSelector(x) == id);

    public void Add(T entity)
    {
        _items.Add(entity);
    }

    public void Update(T entity)
    {
        var id = _idSelector(entity);
        var index = _items.FindIndex(x => _idSelector(x) == id);
        if (index >= 0)
        {
            _items[index] = entity;
        }
    }

    public bool Delete(string id)
    {
        var item = GetById(id);
        return item != null && _items.Remove(item);
    }

    public void Clear() => _items.Clear();

    public void Load(IEnumerable<T> items)
    {
        _items.Clear();
        _items.AddRange(items);
    }
}
