﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    public class DefaultStatus : BaseStatusUpdate
    {
        public DefaultStatus(string text)
            : base(string.Empty, null, string.Empty, string.Empty)
        {
            Text = text; 
        }
    }
}