﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace MobileFormsDemo
{
    interface Model
    {
        void PopulateData(JObject json);
    }
}