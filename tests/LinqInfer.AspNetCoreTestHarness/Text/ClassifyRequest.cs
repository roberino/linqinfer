﻿namespace LinqInfer.AspNetCoreTestHarness.Text
{
    public class ClassifyRequest : FeatureExtractRequest
    {
        public string ClassifierName { get; set; }

        public string Text { get; set; }
    }
}