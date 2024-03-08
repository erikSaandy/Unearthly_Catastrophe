using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public interface IWeighted
{
	public bool Disabled { get; set; }
	[Range( 0, 100 )] public int Weight { get; set; }

}
