namespace SceneNavigator
{
	using JetBrains.Annotations;

	[PublicAPI]
	public static class SceneNavigatorSettings
	{
		public static ISceneNavigatorProvider     provider     { get; set; }
		public static ISceneNavigatorRegistration registration { get; set; }

		static SceneNavigatorSettings()
		{
			var obj = new SceneNavigatorProvider();
			provider = obj;
			registration = obj;
		}
	}
}