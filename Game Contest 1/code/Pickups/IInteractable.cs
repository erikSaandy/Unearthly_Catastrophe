
public interface IInteractable
{

	public float InteractionTime { get; }

	public string ToolTip { get; set; }

	public string GetToolTip( Player player );

	public abstract bool IsInteractableBy( Player player );
	public abstract void OnInteract( Guid playerId );

	public static string GetInteractionKey() { return $"[{Input.GetButtonOrigin( "use" ).ToUpper()}]"; }

}
