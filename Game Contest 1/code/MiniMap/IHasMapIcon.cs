using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IHasMapIcon
{
	public GameTransform Transform { get; }

	public Color IconColor { get; }

	public float IconRotation { get; }

	public void RenderMapIcon();

}
