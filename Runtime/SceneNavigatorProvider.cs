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
			_metaData[obj] = new SceneObjectMetaData(obj, tag);

			if (_allTags.TryGetValue(tag, out var count))
			{
				_allTags[tag] = count + 1;
			}
			else
			{
				_allTags[tag] = 1;
			}

			onCollectionChanged?.Invoke();
		}

		public void Register(Object obj, params string[] tags)
		{
			_metaData[obj] = new SceneObjectMetaData(obj, tags);
			foreach (var tag in tags)
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

			onCollectionChanged?.Invoke();
		}


		public void Deregister(Object obj)
		{
			if(_metaData.TryGetValue(obj, out var data))
			{
				foreach (var tag in data.tags)
				{
					var val = _allTags[tag]--;
					if (val == 0)
					{
						_allTags.Remove(tag);
					}
				}
				_metaData.Remove(obj);
			}

			onCollectionChanged?.Invoke();
		}

		public IReadOnlyDictionary<string, int> tagData => _allTags;

		public IEnumerable<SceneObjectMetaData> GetObjects(HashSet<string> tags, SceneNavigatorFilterMode tagFilterMode)
		{
			switch (tagFilterMode)
			{
				case SceneNavigatorFilterMode.MatchAnyTag:
					foreach (var val in _metaData.Values)
					{
						if (val.tags.Overlaps(tags) && val.targetObject != null)
						{
							yield return val;
						}
					}
					break;
				case SceneNavigatorFilterMode.MatchAllTags:

					foreach (var val in _metaData.Values)
					{
						if (val.tags.IsSupersetOf(tags) && val.targetObject != null)
						{
							yield return val;
						}
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(tagFilterMode), tagFilterMode, null);
			}
		}

		public event Action onCollectionChanged;
	}
}