/*
 * Author: Andy Tran, Tingting Zhou
 * CS 3500 Project: PS8 - GUI in client side
 * 11/28/2022
 */
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    /// <summary>
    /// Class represents a command object
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Command
    {
        [JsonProperty("moving")]
        public String? moving;

        /// <summary>
        /// Constructor
        /// </summary>
        public Command()
        {

        }
    }
}
