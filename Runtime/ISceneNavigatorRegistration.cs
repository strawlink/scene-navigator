namespace SceneNavigator
{
	using JetBrains.Annotations;
	using UnityEngine;

	public interface ISceneNavigatorRegistration
	{
		void Register([NotNull] Object obj, [NotNull] string tag = "(default)");
		void Register([NotNull] Object obj, [NotNull] params string[] tags);
		void Deregister([NotNull] Object obj);

	}
}