namespace ImageToAscii.Helper;

public class Cache<K, V>
{
	private readonly Dictionary<K, CacheItem<V>> _cache = new();

	public void Store(K key, V value, TimeSpan expiresAfter)
	{
		_cache[key] = new CacheItem<V>(value, expiresAfter);
	}

	public V Get(K key)
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