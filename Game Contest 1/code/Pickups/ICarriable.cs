using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ICarriable
{
	[Property] public Texture Icon { get; set; }

	public abstract void OnPickup( Player player );

}
