namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using JetBrains.Annotations;
	using Object = UnityEngine.Object;

	[PublicAPI]
	public class SceneNavigatorProvider : ISceneNavigatorProvider, ISceneNavigatorRegistration
	{
		private static Dictionary<string, HashSet<Object>> _collection = new Dictionary<string, HashSet<Object>>();

		public void Register(Object obj, string tag = "(default)")
		{
			if (!_collection.TryGetValue(tag, out var coll))
			{
				_collection[tag] = new HashSet<Object> {obj};
			}
			else
			{
				coll.Add(obj);
			}

			onCollectionChanged?.Invoke();
		}

		public void Register(Object obj, params string[] tags)
		{
			foreach (var tag in tags)
			{
				Register(obj, tag);
			}
		}

		private static List<string> _pendingDeletedTags = new List<string>();

		public void Deregister(Object obj)
		{
			foreach (var pair in _collection)
			{
				if (pair.Value.Remove(obj) && pair.Value.Count == 0)
				{
					_pendingDeletedTags.Add(pair.Key);
				}
			}

			foreach (var tag in _pendingDeletedTags)
			{
				_collection.Remove(tag);
			}

			_pendingDeletedTags.Clear();

			onCollectionChanged?.Invoke();
		}

		public IEnumerable<string> GetAllTags()
		{
			return _collection.Keys;
		}

		public IEnumerable<Object> GetObjects(string tag)
		{
			return _collection[tag];
		}

		public event Action onCollectionChanged;
	}
}