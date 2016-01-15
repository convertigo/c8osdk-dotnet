using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace retail_store
{
    public interface IDisplay
    {


        /// <summary>
        /// Gets the screen height in pixels
        /// </summary>
        Double Height { get; }

        /// <summary>
        /// Gets the screen width in pixels
        /// </summary>
        Double Width { get; }
    }
}
