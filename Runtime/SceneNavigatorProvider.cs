namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;

	internal class SceneNavigatorProvider : ISceneNavigatorProvider, ISceneNavigatorRegistration
	{
		private readonly Dictionary<string, HashSet<Object>> _collection = new Dictionary<string, HashSet<Object>>();

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

		private readonly List<string> _pendingDeletedTags = new List<string>();

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

		public ICollection<string> GetAllTags()
		{
			return _collection.Keys;
		}

		private readonly HashSet<Object> _objectsBuffer = new HashSet<Object>();

		public IEnumerable<Object> GetObjects(IList<string> tags, SceneNavigatorFilterMode tagFilterMode)
		{
			_objectsBuffer.Clear();
			if (tags.Count == 0)
			{
				return _objectsBuffer;
			}

			switch (tagFilterMode)
			{
				case SceneNavigatorFilterMode.MatchAnyTag:
					foreach (var tag in tags)
					{
						foreach (var o in _collection[tag])
						{
							_objectsBuffer.Add(o);
						}
					}
					break;
				case SceneNavigatorFilterMode.MatchAllTags:
					foreach (var o in _collection[tags[0]])
					{
						_objectsBuffer.Add(o);
					}
					for (int i = 1; i < tags.Count; i++)
					{
						_objectsBuffer.IntersectWith(_collection[tags[i]]);
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(tagFilterMode), tagFilterMode, null);
			}

			return _objectsBuffer;
		}

		public event Action onCollectionChanged;
	}
}