namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;

	public interface ISceneNavigatorProvider
	{
		string providerName { get; }

		IReadOnlyDictionary<string,int> tagData { get; }

		IEnumerable<SceneObjectMetaData> GetObjects();

		event Action onCollectionChanged;
	}
}