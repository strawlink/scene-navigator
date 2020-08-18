namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;

	public interface ISceneNavigatorProvider
	{
		IEnumerable<string> GetAllTags();

		IEnumerable<Object> GetObjects(string tag);

		event Action onCollectionChanged;
	}
}