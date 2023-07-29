using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace El_Gogh.Database
{
	class Img2ImgPreset
	{
		public string name { get; set; }
		public ulong creator { get; set; }
		public Settings settings { get; set; }
		public Img2ImgRequest request { get; set; }
	}
}
