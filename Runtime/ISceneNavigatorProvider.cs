namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;

	public interface ISceneNavigatorProvider
	{
		IReadOnlyDictionary<string,int> tagData { get; }

		IEnumerable<SceneObjectMetaData> GetObjects(IEnumerable<string> tags, SceneNavigatorFilterMode tagFilterMode);

		event Action onCollectionChanged;
	}
}