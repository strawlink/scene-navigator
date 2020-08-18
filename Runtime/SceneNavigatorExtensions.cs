namespace SceneNavigator
{
	using System.Diagnostics;
	using JetBrains.Annotations;
	using UnityEngine;

	public static class SceneNavigatorExtensions
	{
		private const string ConditionalDefine = "UNITY_EDITOR";

		[Conditional(ConditionalDefine)]
		public static void AddToNavigator(this Object obj, [NotNull] string tag = "(default)")
			=> SceneNavigatorSettings.registration.Register(obj, tag);

		[Conditional(ConditionalDefine)]
		public static void AddToNavigator(this Object obj, [NotNull] params string[] tags)
			=> SceneNavigatorSettings.registration.Register(obj, tags);

		[Conditional(ConditionalDefine)]
		public static void RemoveFromNavigator(this Object obj)
			=> SceneNavigatorSettings.registration.Deregister(obj);
	}
}