﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonLibrary.Languages
{
    public class LocalizedStrings
    {

        private static readonly AppResources _localizedResources = new AppResources();

        public AppResources LocalizedResources 
        { 
            get 
            { 
                return _localizedResources; 
            }
        }

    }
}
