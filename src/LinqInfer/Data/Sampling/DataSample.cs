using LinqInfer.Learning.Features;
using System.Collections.Generic;

namespace LinqInfer.Data.Sampling
{
    public class DataSample : DataSampleHeader
    {
        public string Description { get; set; }

        public virtual IFloatingPointFeatureExtractor<DataItem> CreateFeatureExtractor(params int[] selectedFeatures)
        {
            return new DataSampleFeatureMap(this, (selectedFeatures == null || selectedFeatures.Length == 0) ? null : selectedFeatures);
        }

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
