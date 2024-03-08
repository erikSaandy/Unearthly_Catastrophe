public static class ICollectionExtension
{
	public static T GetRandom<T>( this ICollection<T> pool ) where T : IWeighted
	{
		if ( pool == null || pool.Count == 0 ) { Log.Error( $"weighted pool can not be empty." ); }

		// Only one item in list? select it.
		if ( pool.Count == 1 ) { return pool.ElementAt( 0 ); }

		int totalWeight = pool.Sum( x => x.Weight );

		int rnd = LethalGameManager.Random.Next( totalWeight );

		return pool.First( x => (rnd -= x.Weight) < 0 );

	}
}
