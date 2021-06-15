using System;
using System.Text;
using UnityEngine;

namespace CrazyPanda.UnityCore.BuildUtils
{
	/// <summary>
	/// Информация о билде. Записывается в файл во время билда
	/// </summary>
	[ Serializable ]
	public class BuildInfo : ScriptableObject
	{
		public const string BuildInfoPath = "BuildInfo";

		/// <summary>
		/// Id приложения для доступа в runtime (та же что прописана в самом приложении как bundle identifier)
		/// </summary>
		public string BundleIdentifier;

		/// <summary>
		/// Версия приложения для доступа в runtime (та же что прописана в самом приложении как bundle version)
		/// </summary>
		public string BundleVersion;

		/// <summary>
		/// Версия исходных файлов
		/// </summary>
		public string SourceCodeVersion;

		/// <summary>
		/// Имя проекта в системе сборки
		/// </summary>
		public string BuildJob;

		/// <summary>
		/// Номер билда в системе сборки
		/// </summary>
		public int BuildNumber;

		/// <summary>
		/// Пользовательские дефайны использованные при сборке.
		/// </summary>
		public string[ ] BuildDefines;

		/// <summary>
		/// Внутреннее имя майлстоуна
		/// </summary>
		public string MilestoneCodename;

		[ SerializeField ] private long _buildTimestamp;

		/// <summary>
		/// Являетсья ли версия - девелопмент билдом
		/// </summary>
		public bool IsDevelopmentBuild { get { return Debug.isDebugBuild; } }

		/// <summary>
		/// Информация о дате и времени когда был создан билд (локальное время)
		/// </summary>
		public DateTime BuildTimestamp { get { return DateTime.FromFileTime( _buildTimestamp ); } set { _buildTimestamp = value.ToFileTime(); } }

		/// <summary>
		/// Загружает из файла
		/// </summary>
		public static BuildInfo Load()
		{
			var buildInfo = Resources.Load< BuildInfo >( BuildInfoPath );
			if( buildInfo != null )
			{
				return buildInfo;
			}

			buildInfo = CreateInstance< BuildInfo >();
			buildInfo.BundleIdentifier = "Unknown";
			buildInfo.BundleVersion = "Unknown";
			buildInfo.SourceCodeVersion = "Unknown";
			buildInfo.BuildJob = "Manual build";
			buildInfo.BuildNumber = -1;
			buildInfo.BuildDefines = new[ ]
			{
				"UNKNOWN",
				"UNKNOWN",
				"UNKNOWN"
			};
			buildInfo.MilestoneCodename = "Unknown";
			return buildInfo;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb.Append( "BundleIdentifier: " );
			sb.AppendLine( BundleIdentifier );
			sb.Append( "BundleVersion: " );
			sb.AppendLine( BundleVersion );
			sb.Append( "SourceCodeVersion: " );
			sb.AppendLine( SourceCodeVersion );
			sb.Append( "BuildJob: " );
			sb.AppendLine( BuildJob );
			sb.Append( "IsDevelopmentBuild: " );
			sb.AppendLine( IsDevelopmentBuild.ToString() );
			sb.Append( "BuildNumber: " );
			sb.AppendLine( BuildNumber.ToString() );
			sb.Append( "BuildDefines: " );
			sb.AppendLine( string.Join( ", ", BuildDefines ) );
			sb.Append( "MilestoneCodename: " );
			sb.Append( MilestoneCodename );
			return sb.ToString();
		}
	}
}