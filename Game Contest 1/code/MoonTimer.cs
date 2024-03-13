using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class MoonTimerComponent : Component
{
	public static MoonTimerComponent Instance { get; set; }

	[Sync] public bool IsCounting { get; set; } = false;
	[Sync] public float TargetTime { get; set; } = 0;

	private RealTimeSince TimeSinceStarted { get; set; }

	/// <summary>
	/// Is the timer visible on this client?
	/// </summary>
	public bool Visible { get; private set; } = false;

	public void Show() { Visible = true; }
	public void Hide() { Visible = false; }

	public void ToggleVisibility() { if( Visible ) { Hide(); } else { Show(); } }

	public float SecondsSinceStart
	{
		get
		{
			return IsCounting ? TimeSinceStarted : 0;
		}
		private set
		{
			TimeSinceStarted = value;
		}
	}

	public float SecondsRemaining => (TargetTime - SecondsSinceStart);

	public string FormattedTimeRemaining {
		get
		{
			int minutes = ((int)SecondsRemaining / 60);
			float seconds = SecondsRemaining % 60;
			return $"0{ minutes } : { seconds.ToString("00") }";
		}
	}

	private Action OnFinish { get; set; }



	public void StartTimer(float targetTime, Action onFinish )
	{

		this.OnFinish = onFinish;
		Log.Info( "started timer for " + TargetTime + " seconds." );
		StartTimer( targetTime );
	}

	[Broadcast]
	public void StartTimer( float targetTime )
	{

		Log.Info( "start timer" );
		IsCounting = true;
		SecondsSinceStart = 0;
		this.TargetTime = targetTime;

		if ( IsProxy ) { return; }

	}

	[Broadcast]
	public void StopTimer()
	{

		IsCounting = false;

		if ( IsProxy ) { return; }

		this.OnFinish = null;

	}

	protected override void OnAwake()
	{
		Instance = this;

		base.OnStart();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if( !IsCounting ) { return; }

		if( SecondsSinceStart >= TargetTime )
		{
			IsCounting = false;
			OnFinish?.Invoke();
			OnFinish = null;
		}

	}


}
