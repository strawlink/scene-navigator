namespace SceneNavigator
{
	using System;
	using System.Collections.Generic;
	using Object = UnityEngine.Object;

	public interface ISceneNavigatorProvider
	{
		ICollection<string> GetAllTags();

		IEnumerable<Object> GetObjects(IList<string> tags, SceneNavigatorFilterMode tagFilterMode);

		event Action onCollectionChanged;
	}
}