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

	public RealTimeSince TimeSinceStarted { get; private set; }

	public float TimeLeft => TargetTime - TimeSinceStarted;

	public const float WARNING_TIME = 60;
	private bool HasSentWarning { get; set; } = false;

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
			return TimeSinceStarted;
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
			return $"{ minutes.ToString("00") } : { seconds.ToString("00") }";
		}
	}

	private Action OnFinish { get; set; }
	private Action OnWarning { get; set; }


	public void StartTimer(float targetTime, Action onFinish, Action onWarning = null )
	{
		HasSentWarning = true;
		this.OnFinish = onFinish;
		this.OnWarning = onWarning;
		StartTimer( targetTime );
		Log.Info( "started timer for " + targetTime + " seconds." );
	}

	[Broadcast]
	private void StartTimer( float targetTime )
	{

		Log.Info( "start timer" );
		IsCounting = true;
		SecondsSinceStart = 0;
		HasSentWarning = false;
		this.TargetTime = targetTime;

		if ( IsProxy ) { return; }

	}

	[Broadcast]
	public void StopTimer()
	{

		IsCounting = false;
		OnWarning = null;

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

		if ( !HasSentWarning && TimeLeft < WARNING_TIME )
		{
			HasSentWarning = true;
			OnWarning?.Invoke();
		}

		if (IsProxy) { return; }

	}


}
