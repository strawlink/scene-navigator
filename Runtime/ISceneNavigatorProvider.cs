namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;

	public interface ISceneNavigatorProvider
	{
		IReadOnlyDictionary<string,int> tagData { get; }

		IEnumerable<SceneObjectMetaData> GetObjects(HashSet<string> tags, SceneNavigatorFilterMode tagFilterMode);

		event Action onCollectionChanged;
	}
}