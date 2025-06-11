namespace ImageToAscii.HelperClasses;

public class Cache<K, V>
{
	private readonly Dictionary<K, CacheItem<V>> _cache = new();

	public V this[K key]
	{
		get { return Get(key); }
	}

	public V this[K key, TimeSpan span]
	{
		set { Store(key, value, span); }
	}

	private void Store(K key, V value, TimeSpan expiresAfter)
	{
		_cache[key] = new CacheItem<V>(value, expiresAfter);
	}

	private V Get(K key)
	{
		if (!_cache.ContainsKey(key))
			return default(V);

		var cached = _cache[key];
		if (DateTimeOffset.Now - cached.Created >= cached.ExpiresAfter)
		{
			_cache.Remove(key);
			return default(V);
		}

		return cached.Value;
	}
}

public class CacheItem<T>(T value, TimeSpan expiresAfter)
{
	public T Value { get; } = value;
	internal DateTimeOffset Created { get; } = DateTimeOffset.Now;
	internal TimeSpan ExpiresAfter { get; } = expiresAfter;
}