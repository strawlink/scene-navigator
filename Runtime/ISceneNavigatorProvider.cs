namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;

	public interface ISceneNavigatorProvider
	{
		IReadOnlyDictionary<string,int> tagData { get; }

		IEnumerable<SceneObjectMetaData> GetObjects();

		event Action onCollectionChanged;
	}
}