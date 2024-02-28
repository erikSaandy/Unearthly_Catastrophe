
public interface IInteractable
{
	public float InteractionTime { get; set; }

	public string ToolTip { get; set; }

	public string GetToolTip( Player player );

	public abstract void OnInteract( Player player );

	public static string GetInteractionKey() { return $"[{Input.GetButtonOrigin( "use" ).ToUpper()}]"; }

}
