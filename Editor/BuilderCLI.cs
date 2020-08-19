using System;
using CrazyPanda.UnityCore.BuildUtils;

// Живёт без неймспейса для того, чтобы можно было писать -executeMethod CLI.Build

public static class CLI
{
	/// <summary>
	/// Запуск сборки из командной строки: Unity -quit -batchmode -executeMethod CLI.Build -buildDir="some-dir" -buildVersion=some-version
	/// </summary>
	public static void Build()
	{
        new Builder().BuildGame( Environment.GetCommandLineArgs() );
	}
}