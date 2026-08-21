using Godot;
using UniversalRPG.App.Library;
using UniversalRPG.GameDetectorNs;

namespace UniversalRPG.App.Launcher;

public partial class RuntimeLauncher : RefCounted
{
	public enum SupportState
	{
		Unavailable,
		Experimental,
		Available,
	}

	public class SupportInfo
	{
		public SupportState State { get; init; }
		public string Label { get; init; } = "";
		public string Reason { get; init; } = "";
	}

	public SupportInfo GetSupport(GameDetector.EngineType pEngine)
	{
		return pEngine switch
		{
			GameDetector.EngineType.RpgMaker2000 or GameDetector.EngineType.RpgMaker2003 or GameDetector.EngineType.RpgMaker2000_2003
				=> new SupportInfo
				{
					State = SupportState.Unavailable,
					Label = Tr("RUNTIME_LCF_LABEL"),
					Reason = Tr("RUNTIME_LCF_REASON"),
				},
			GameDetector.EngineType.RpgMakerXp or GameDetector.EngineType.RpgMakerVx or GameDetector.EngineType.RpgMakerVxAce
				=> new SupportInfo
				{
					State = SupportState.Unavailable,
					Label = Tr("RUNTIME_PLANNED_LABEL"),
					Reason = Tr("RUNTIME_RGSS_REASON"),
				},
			GameDetector.EngineType.RpgMakerMv or GameDetector.EngineType.RpgMakerMz
				=> new SupportInfo
				{
					State = SupportState.Unavailable,
					Label = Tr("RUNTIME_PLANNED_LABEL"),
					Reason = Tr("RUNTIME_JS_REASON"),
				},
			_ => new SupportInfo
			{
				State = SupportState.Unavailable,
				Label = Tr("RUNTIME_UNSUPPORTED_LABEL"),
				Reason = Tr("RUNTIME_UNSUPPORTED_REASON"),
			},
		};
	}

	public LaunchResult Launch(GameLibrary.GameEntry pGame)
	{
		var support = GetSupport(pGame.Detection.Engine);
		if (support.State != SupportState.Available)
		{
			return new LaunchResult { Success = false, Message = support.Reason };
		}
		return new LaunchResult { Success = false, Message = Tr("RUNTIME_NOT_REGISTERED") };
	}

	public class LaunchResult
	{
		public bool Success { get; init; }
		public string Message { get; init; } = "";
	}
}
