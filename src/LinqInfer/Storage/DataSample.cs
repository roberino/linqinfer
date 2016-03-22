using System;
using System.Collections.Generic;

namespace LinqInfer.Storage
{
    [Serializable]
    public class DataSample : DataSampleHeader
    {
        public string Description { get; set; }

        public virtual SampleSummary Recalculate()
        {
            if (SampleData != null)
            {
                if (Summary == null) Summary = new SampleSummary();

                Summary.Recalculate(SampleData);
            }

            return Summary;
        }

        public virtual ICollection<DataItem> SampleData { get; set; }
    }
}
