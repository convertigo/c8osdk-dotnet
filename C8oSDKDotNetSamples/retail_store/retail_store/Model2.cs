﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace retail_store
{
    interface Model2
    {
        void PopulateData(JObject json, Boolean isproduct);
        List<ProdStock> ProductStock
        {
            get;
            set;
        }
    }
}