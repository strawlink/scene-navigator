namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;

	internal class SceneNavigatorProvider : ISceneNavigatorProvider, ISceneNavigatorRegistration
	{
		private readonly Dictionary<Object, SceneObjectMetaData> _metaData = new Dictionary<Object, SceneObjectMetaData>();
		private readonly Dictionary<string,int> _allTags = new Dictionary<string, int>();

		public void Register(Object obj, string tag = "(default)")
		{
			if (_metaData.TryGetValue(obj, out var existingData))
			{
				if (existingData.tags.Add(tag))
				{
					IncrementTagCounter(tag);
				}
				return;
			}

			_metaData[obj] = new SceneObjectMetaData(obj, tag);

			IncrementTagCounter(tag);

			onCollectionChanged?.Invoke();
		}

		private void IncrementTagCounter(string tag)
		{
			if (_allTags.TryGetValue(tag, out var count))
			{
				_allTags[tag] = count + 1;
			}
			else
			{
				_allTags[tag] = 1;
			}
		}

		public void Register(Object obj, params string[] tags)
		{
			if (_metaData.TryGetValue(obj, out var existingData))
			{
				foreach (var tag in tags)
				{
					if (existingData.tags.Add(tag))
					{
						IncrementTagCounter(tag);
					}
				}
				return;
			}

			_metaData[obj] = new SceneObjectMetaData(obj, tags);
			foreach (var tag in tags)
			{
				IncrementTagCounter(tag);
			}

			onCollectionChanged?.Invoke();
		}


		public void Deregister(Object obj)
		{
			if (!_metaData.TryGetValue(obj, out var data))
			{
				return;
			}

			foreach (var tag in data.tags)
			{
				var val = _allTags[tag]--;
				if (val == 0)
				{
					_allTags.Remove(tag);
				}
			}

			_metaData.Remove(obj);

			onCollectionChanged?.Invoke();
		}

		public IReadOnlyDictionary<string, int> tagData => _allTags;

		private readonly List<Object> _pendingDeletion = new List<Object>();
		public IEnumerable<SceneObjectMetaData> GetObjects()
		{
			foreach (var pair in _metaData)
			{
				if(pair.Value.targetObject == null)
				{
					_pendingDeletion.Add(pair.Key);
					continue;
				}
				yield return pair.Value;
			}

			foreach (var obj in _pendingDeletion)
			{
				Deregister(obj);
			}
			_pendingDeletion.Clear();
		}

		public event Action onCollectionChanged;
	}
}