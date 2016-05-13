﻿using System;
using System.Collections.Generic;

namespace LinqInfer.Data.Sampling
{
    [Serializable]
    public class DataSampleMetadata
    {
        public DataSampleMetadata()
        {
            Fields = new List<FieldDescriptor>();
        }

        public virtual ICollection<FieldDescriptor> Fields { get; set; }
    }
}